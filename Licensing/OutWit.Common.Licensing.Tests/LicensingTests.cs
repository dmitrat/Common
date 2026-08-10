using System;
using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.Licensing.Binding;
using OutWit.Common.Licensing.Storage;

namespace OutWit.Common.Licensing.Tests
{
    /// <summary>
    /// The two ways a product acquires a licensing runtime: by hand, and through
    /// a container.
    /// </summary>
    [TestFixture]
    public sealed class LicensingTests
    {
        private static readonly DateTime NOW = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

        private LicenseTestContext m_context = null!;

        [SetUp]
        public void SetUp()
        {
            m_context = new LicenseTestContext();
        }

        #region Factory Tests

        [Test]
        public void CreateReturnsASettledStateTest()
        {
            // A product that discovers its licence state on first use discovers
            // it at an arbitrary moment, and a startup banner that renders
            // before the state settles is a bug nobody reproduces reliably.
            var service = Licensing.Create(options => Configure(options));

            Assert.Multiple(() =>
            {
                Assert.That(service.State.Mode, Is.EqualTo(LicenseMode.Licensed));
                Assert.That(service.Fingerprint, Is.Not.Empty);
                Assert.That(service.Snapshot.CanRun, Is.True);
            });
        }

        [Test]
        public void CreateWithoutAProductRefusesTest()
        {
            Assert.Throws<InvalidOperationException>(() => Licensing.Create(options => options.WithGrace(TimeSpan.Zero)));
        }

        #endregion

        #region Container Tests

        [Test]
        public void EagerRegistrationResolvesASettledServiceTest()
        {
            var services = new ServiceCollection();
            services.AddLicensing(options => Configure(options));

            using var provider = services.BuildServiceProvider();

            Assert.That(provider.GetRequiredService<ILicenseService>().State.Mode, Is.EqualTo(LicenseMode.Licensed));
        }

        [Test]
        public void LazyRegistrationSeesTheContainerTest()
        {
            // The point of the overload: a store built from a directory
            // provider, a key ring contributed by a module and a tenant slug
            // read from configuration are all things the eager path cannot
            // reach, because at registration time there is no container yet.
            var services = new ServiceCollection();
            services.AddSingleton<ILicenseStore>(new LicenseStoreMemory(Issue()));
            services.AddLicensing((provider, options) => Configure(options)
                .WithStore(provider.GetRequiredService<ILicenseStore>()));

            using var provider = services.BuildServiceProvider();

            Assert.That(provider.GetRequiredService<ILicenseService>().State.Mode, Is.EqualTo(LicenseMode.Licensed));
        }

        [Test]
        public void ContributionsFromSeveralModulesLandOnOneServiceTest()
        {
            // Two modules that both want licensing would otherwise produce two
            // services, two timers and two stores writing the same sidecar.
            var services = new ServiceCollection();

            services.ConfigureLicensing((_, options) => options
                .ForProduct(LicenseTestContext.PRODUCT, new Version(1, 5, 0))
                .WithClock(() => NOW));

            services.ConfigureLicensing((_, options) => options
                .WithKeyRing(m_context.Ring())
                .WithBinding(new LicenseBindingProviderNone()));

            services.ConfigureLicensing((_, options) => options
                .WithStore(new LicenseStoreMemory(Issue()))
                .Declares(vocabulary => vocabulary.Feature("sso")));

            services.AddLicensingCore();

            using var provider = services.BuildServiceProvider();

            var first = provider.GetRequiredService<ILicenseService>();
            var second = provider.GetRequiredService<LicenseService>();

            Assert.Multiple(() =>
            {
                Assert.That(first, Is.SameAs(second), "The interface and the implementation must be one instance.");
                Assert.That(first.State.Mode, Is.EqualTo(LicenseMode.Licensed));
                Assert.That(provider.GetRequiredService<LicensingOptions>().Product,
                    Is.EqualTo(LicenseTestContext.PRODUCT));
            });
        }

        [Test]
        public void RegisteringTwiceBuildsOneServiceTest()
        {
            var services = new ServiceCollection();
            services.AddLicensing((_, options) => Configure(options));
            services.AddLicensing((_, options) => Configure(options));

            using var provider = services.BuildServiceProvider();

            Assert.That(provider.GetRequiredService<ILicenseService>(),
                Is.SameAs(provider.GetRequiredService<LicenseService>()));
        }

        [Test]
        public void ContainerDisposesTheServiceTest()
        {
            // The service now owns a timer. A container that never disposes what
            // it hands out leaks one per host.
            var services = new ServiceCollection();
            services.AddLicensing(options => Configure(options).WithPeriodicReload(TimeSpan.FromMinutes(5)));

            LicenseService service;

            using (var provider = services.BuildServiceProvider())
                service = provider.GetRequiredService<LicenseService>();

            Assert.DoesNotThrow(() => service.Dispose(), "A second dispose must be harmless.");
        }

        #endregion

        #region Tools

        private string Issue()
        {
            return m_context.Issue(LicenseTestContext.Payload(notBefore: NOW.AddYears(-1), expires: NOW.AddYears(1)));
        }

        private LicensingOptions Configure(LicensingOptions options)
        {
            return options
                .ForProduct(LicenseTestContext.PRODUCT, new Version(1, 5, 0))
                .WithKeyRing(m_context.Ring())
                .WithStore(new LicenseStoreMemory(Issue()))
                .WithBinding(new LicenseBindingProviderNone())
                .WithClock(() => NOW);
        }

        #endregion
    }
}
