using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.DependencyInjection.Tests.Mock;

namespace OutWit.Common.DependencyInjection.Tests
{
    /// <summary>
    /// Coverage for the <c>Add*WithInject</c> registration helpers — eager property
    /// resolution at instance creation time, plus the mixin wired up for any
    /// subsequent lazy getter access.
    /// </summary>
    [TestFixture]
    public class ServiceCollectionExtensionsTests
    {
        #region Singleton Tests

        [Test]
        public void AddSingletonWithInjectResolvesPropertiesTest()
        {
            var sp = new ServiceCollection()
                .AddSingleton<IRequiredService, RequiredServiceImpl>()
                .AddSingletonWithInject<ServiceWithInject>()
                .BuildServiceProvider();

            var instance = sp.GetRequiredService<ServiceWithInject>();

            Assert.That(instance.Required, Is.Not.Null);
            Assert.That(instance.Required.Name, Is.EqualTo("Required"));
            Assert.That(instance.Optional, Is.Null);
        }

        [Test]
        public void AddSingletonWithInjectReturnsSameInstanceTest()
        {
            var sp = new ServiceCollection()
                .AddSingleton<IRequiredService, RequiredServiceImpl>()
                .AddSingletonWithInject<ServiceWithInject>()
                .BuildServiceProvider();

            var a = sp.GetRequiredService<ServiceWithInject>();
            var b = sp.GetRequiredService<ServiceWithInject>();

            Assert.That(a, Is.SameAs(b));
        }

        [Test]
        public void AddSingletonWithInjectWiresMixinForLazyAccessTest()
        {
            var sp = new ServiceCollection()
                .AddSingleton<IRequiredService, RequiredServiceImpl>()
                .AddSingletonWithInject<ServiceWithInject>()
                .BuildServiceProvider();

            var instance = sp.GetRequiredService<ServiceWithInject>();
            var injectable = (IInjectable)instance;

            // Exact instance is an MS DI implementation detail (root vs engine scope);
            // what matters is that the mixin received a service provider so any later
            // lazy [Inject] access works.
            Assert.That(injectable.ServiceProvider, Is.Not.Null);
        }

        #endregion

        #region Scoped Tests

        [Test]
        public void AddScopedWithInjectResolvesPropertiesTest()
        {
            var sp = new ServiceCollection()
                .AddSingleton<IRequiredService, RequiredServiceImpl>()
                .AddScopedWithInject<ServiceWithInject>()
                .BuildServiceProvider();

            using var scope = sp.CreateScope();
            var instance = scope.ServiceProvider.GetRequiredService<ServiceWithInject>();

            Assert.That(instance.Required, Is.Not.Null);
        }

        #endregion

        #region Transient Tests

        [Test]
        public void AddTransientWithInjectCreatesDifferentInstancesTest()
        {
            var sp = new ServiceCollection()
                .AddSingleton<IRequiredService, RequiredServiceImpl>()
                .AddTransientWithInject<ServiceWithInject>()
                .BuildServiceProvider();

            var a = sp.GetRequiredService<ServiceWithInject>();
            var b = sp.GetRequiredService<ServiceWithInject>();

            Assert.That(a, Is.Not.SameAs(b));
            Assert.That(a.Required, Is.Not.Null);
            Assert.That(b.Required, Is.Not.Null);
        }

        #endregion

        #region Interface Registration Tests

        [Test]
        public void AddSingletonWithInjectInterfaceRegistrationTest()
        {
            var sp = new ServiceCollection()
                .AddSingleton<IRequiredService, RequiredServiceImpl>()
                .AddSingletonWithInject<IRequiredService, RequiredServiceImpl>()
                .BuildServiceProvider();

            // Both registrations are visible — GetServices returns both.
            var all = sp.GetServices<IRequiredService>().ToList();
            Assert.That(all.Count, Is.EqualTo(2));
        }

        #endregion
    }
}
