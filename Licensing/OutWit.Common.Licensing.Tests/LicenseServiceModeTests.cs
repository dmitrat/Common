using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OutWit.Common.Licensing.Binding;
using OutWit.Common.Licensing.Storage;
using OutWit.Common.Licensing.Validation;

namespace OutWit.Common.Licensing.Tests
{
    /// <summary>
    /// Mode, grace and the clock through the whole service, on real signed
    /// licences and a real store — the arrangement a product actually has.
    /// </summary>
    [TestFixture]
    public sealed class LicenseServiceModeTests
    {
        private static readonly DateTime NOW = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

        private LicenseTestContext m_context = null!;
        private LicenseStoreMemory m_store = null!;
        private DateTime m_clock;

        [SetUp]
        public void SetUp()
        {
            m_context = new LicenseTestContext();
            m_store = new LicenseStoreMemory();
            m_clock = NOW;
        }

        #region Grace Tests

        [Test]
        public async Task ExpiredInsideGraceStillWorksTest()
        {
            var service = await CreateAsync(grace: TimeSpan.FromDays(14));
            await service.InstallAsync(Issue(expires: NOW.AddDays(1), features: new[] { "sso" }));

            m_clock = NOW.AddDays(5);
            await service.ReloadAsync();

            Assert.Multiple(() =>
            {
                Assert.That(service.State.Mode, Is.EqualTo(LicenseMode.Grace));
                Assert.That(service.State.Status, Is.EqualTo(LicenseStatus.Expired),
                    "Grace never hides the underlying verdict — the reason has to stay specific.");
                Assert.That(service.State.CanRun, Is.True);
                Assert.That(service.HasFeature("sso"), Is.True,
                    "A grace window that refused work would be a later expiry with a worse message.");
            });
        }

        [Test]
        public async Task GraceRunsOutTest()
        {
            var service = await CreateAsync(grace: TimeSpan.FromDays(14));
            await service.InstallAsync(Issue(expires: NOW.AddDays(1)));

            m_clock = NOW.AddDays(20);
            await service.ReloadAsync();

            Assert.Multiple(() =>
            {
                Assert.That(service.State.Mode, Is.EqualTo(LicenseMode.Restricted));
                Assert.That(service.State.CanRun, Is.False);
                Assert.That(service.HasFeature("sso"), Is.False);
            });
        }

        [Test]
        public async Task WithoutGraceExpiryIsImmediateTest()
        {
            // The default. Every build that has not opted in behaves exactly as
            // it did before this stage existed.
            var service = await CreateAsync();
            await service.InstallAsync(Issue(expires: NOW.AddDays(1)));

            m_clock = NOW.AddDays(2);
            await service.ReloadAsync();

            Assert.Multiple(() =>
            {
                Assert.That(service.State.Mode, Is.EqualTo(LicenseMode.Restricted));
                Assert.That(service.State.CanRun, Is.False);
            });
        }

        [Test]
        public async Task StagedRenewalSupersedesTheGracedLicenceTest()
        {
            // Grace is a safety net, not a state to live in: the moment a valid
            // successor is installed, the product is Licensed again.
            var service = await CreateAsync(grace: TimeSpan.FromDays(14));
            await service.InstallAsync(Issue(expires: NOW.AddDays(1)));

            m_clock = NOW.AddDays(5);
            await service.ReloadAsync();
            Assert.That(service.State.Mode, Is.EqualTo(LicenseMode.Grace));

            await service.InstallAsync(Issue(notBefore: NOW.AddDays(1), expires: NOW.AddYears(1)));

            Assert.Multiple(() =>
            {
                Assert.That(service.State.Mode, Is.EqualTo(LicenseMode.Licensed));
                Assert.That(service.State.CanRun, Is.True);
            });
        }

        #endregion

        #region Demo Tests

        [Test]
        public async Task DemoDaysRemainingFollowsTheInjectedClockTest()
        {
            // The bug the harness's clock travel exposed: the sentence was built
            // from the wall clock while the state was built from the injected
            // one, so the two disagreed and neither was obviously wrong.
            var service = await CreateAsync(demo: TimeSpan.FromDays(30));

            m_clock = NOW.AddDays(20);
            await service.ReloadAsync();

            Assert.Multiple(() =>
            {
                Assert.That(service.State.Mode, Is.EqualTo(LicenseMode.Demo));
                Assert.That(service.State.DaysRemaining, Is.EqualTo(10));
                Assert.That(service.State.Describe(), Does.Contain("10 day"));
            });
        }

        [Test]
        public async Task DemoOverIsRestrictedNotGracedTest()
        {
            var service = await CreateAsync(demo: TimeSpan.FromDays(30), grace: TimeSpan.FromDays(14));

            m_clock = NOW.AddDays(31);
            await service.ReloadAsync();

            Assert.That(service.State.Mode, Is.EqualTo(LicenseMode.Restricted));
        }

        #endregion

        #region Clock Tests

        [Test]
        public async Task SuspectClockKeepsTheLicenceReadableTest()
        {
            // Previously the state came back with no payload at all, so a panel
            // could only say "clock tampered" to a customer whose CMOS battery
            // had died.
            var service = await CreateAsync();
            await service.InstallAsync(Issue(expires: NOW.AddYears(1)));

            m_clock = NOW.AddDays(-10);
            await service.ReloadAsync();

            Assert.Multiple(() =>
            {
                Assert.That(service.State.Mode, Is.EqualTo(LicenseMode.Restricted));
                Assert.That(service.State.IsClockSuspect, Is.True);
                Assert.That(service.State.Status, Is.EqualTo(LicenseStatus.ClockTampered));
                Assert.That(service.State.Payload, Is.Not.Null);
                Assert.That(service.State.Payload!.Customer!.Name, Is.EqualTo("ACME GmbH"));
                Assert.That(service.State.Describe(), Does.Contain("ACME GmbH"));
            });
        }

        [Test]
        public async Task CorrectingTheClockHealsWithoutAReissueTest()
        {
            var service = await CreateAsync();
            await service.InstallAsync(Issue(expires: NOW.AddYears(1)));

            m_clock = NOW.AddDays(-10);
            await service.ReloadAsync();
            Assert.That(service.State.Mode, Is.EqualTo(LicenseMode.Restricted));

            m_clock = NOW;
            await service.ReloadAsync();

            Assert.Multiple(() =>
            {
                Assert.That(service.State.Mode, Is.EqualTo(LicenseMode.Licensed));
                Assert.That(service.State.IsClockSuspect, Is.False);
            });
        }

        [Test]
        public async Task SuspectClockDoesNotAnchorTheDemoTest()
        {
            // Seeding the first-run stamp from a wound-back clock would anchor
            // the demo term to a date that never happened.
            var service = await CreateAsync(demo: TimeSpan.FromDays(30));

            m_clock = NOW.AddDays(10);
            await service.ReloadAsync();

            m_clock = NOW.AddDays(-100);
            await service.ReloadAsync();

            m_clock = NOW.AddDays(11);
            await service.ReloadAsync();

            Assert.That(service.State.DemoExpiresUtc, Is.EqualTo(NOW.AddDays(30)));
        }

        #endregion

        #region Tools

        private string Issue(DateTime? notBefore = null, DateTime? expires = null, IReadOnlyList<string>? features = null)
        {
            return m_context.Issue(LicenseTestContext.Payload(
                notBefore: notBefore ?? NOW.AddYears(-1),
                expires: expires,
                features: features));
        }

        private async Task<LicenseService> CreateAsync(TimeSpan? demo = null, TimeSpan? grace = null)
        {
            var options = new LicensingOptions()
                .ForProduct(LicenseTestContext.PRODUCT, new Version(1, 5, 0))
                .WithKeyRing(m_context.Ring())
                .WithStore(m_store)
                .WithBinding(new LicenseBindingProviderNone())
                .WithClock(() => m_clock);

            if (demo != null)
                options.WithDemo(demo.Value);

            if (grace != null)
                options.WithGrace(grace.Value);

            var service = new LicenseService(options);
            await service.ReloadAsync();

            return service;
        }

        #endregion
    }
}
