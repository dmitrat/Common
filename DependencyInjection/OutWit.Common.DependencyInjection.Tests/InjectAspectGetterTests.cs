using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.DependencyInjection.Tests.Mock;

namespace OutWit.Common.DependencyInjection.Tests
{
    /// <summary>
    /// Coverage for the AspectInjector-woven lazy getter path
    /// (<see cref="InjectAspect"/>).
    /// </summary>
    [TestFixture]
    public class InjectAspectGetterTests
    {
        #region Mixin Tests

        [Test]
        public void MixinAddsIInjectableTest()
        {
            var instance = new ServiceWithConstructorSP(null!);

            Assert.That(instance, Is.InstanceOf<IInjectable>());
        }

        #endregion

        #region InitInject Tests

        [Test]
        public void InitInjectSetsServiceProviderTest()
        {
            var sp = new ServiceCollection()
                .AddSingleton<IRequiredService, RequiredServiceImpl>()
                .BuildServiceProvider();

            var instance = new ServiceWithInitInject(sp);
            var injectable = (IInjectable)instance;

            Assert.That(injectable.ServiceProvider, Is.SameAs(sp));
        }

        [Test]
        public void InitInjectThrowsWhenInstanceIsNullTest()
        {
            var sp = new ServiceCollection().BuildServiceProvider();

            Assert.Throws<ArgumentNullException>(() =>
                InjectableExtensions.InitInject(null!, sp));
        }

        [Test]
        public void InitInjectThrowsWhenServiceProviderIsNullTest()
        {
            var instance = new ServiceWithInject();

            Assert.Throws<ArgumentNullException>(() =>
                instance.InitInject(null!));
        }

        [Test]
        public void InitInjectThrowsWhenInstanceIsNotInjectableTest()
        {
            // A POCO with no [Inject] properties doesn't get the IInjectable mixin.
            var sp = new ServiceCollection().BuildServiceProvider();
            var poco = new object();

            var ex = Assert.Throws<InvalidOperationException>(() =>
                poco.InitInject(sp));

            Assert.That(ex!.Message, Does.Contain("does not implement IInjectable"));
        }

        #endregion

        #region Field Auto-Discovery Tests

        [Test]
        public void FieldAutoDiscoveryFindsServiceProviderTest()
        {
            var sp = new ServiceCollection()
                .AddSingleton<IRequiredService, RequiredServiceImpl>()
                .BuildServiceProvider();

            // ServiceWithConstructorSP stores SP in a field — aspect finds it.
            var instance = new ServiceWithConstructorSP(sp);

            Assert.That(instance.Required, Is.Not.Null);
            Assert.That(instance.Required.Name, Is.EqualTo("Required"));
        }

        #endregion

        #region Lazy Getter Tests

        [Test]
        public void LazyGetterResolvesOnFirstAccessTest()
        {
            var sp = new ServiceCollection()
                .AddSingleton<IRequiredService, RequiredServiceImpl>()
                .BuildServiceProvider();

            var instance = new ServiceWithConstructorSP(sp);

            Assert.That(instance.Required, Is.Not.Null);
            Assert.That(instance.Required.Name, Is.EqualTo("Required"));
        }

        [Test]
        public void LazyGetterCachesAfterFirstAccessTest()
        {
            var sp = new ServiceCollection()
                .AddSingleton<IRequiredService, RequiredServiceImpl>()
                .BuildServiceProvider();

            var instance = new ServiceWithConstructorSP(sp);

            var first = instance.Required;
            var second = instance.Required;

            Assert.That(first, Is.SameAs(second));
        }

        [Test]
        public void LazyGetterOptionalPropertyNullWhenNotRegisteredTest()
        {
            var sp = new ServiceCollection()
                .AddSingleton<IRequiredService, RequiredServiceImpl>()
                .BuildServiceProvider();

            var instance = new ServiceWithConstructorSP(sp);

            Assert.That(instance.Optional, Is.Null);
        }

        [Test]
        public void LazyGetterRequiredPropertyThrowsWhenNotRegisteredTest()
        {
            var sp = new ServiceCollection().BuildServiceProvider();
            var instance = new ServiceWithConstructorSP(sp);

            Assert.Throws<InvalidOperationException>(() =>
            {
                _ = instance.Required;
            });
        }

        #endregion

        #region Transient Mode

        [Test]
        public void TransientGetterResolvesEveryTimeTest()
        {
            var sp = new ServiceCollection()
                .AddTransient<IRequiredService, RequiredServiceImpl>()
                .BuildServiceProvider();

            var instance = new ServiceWithTransient(sp);

            var first = instance.Required;
            var second = instance.Required;

            Assert.That(first, Is.Not.Null);
            Assert.That(second, Is.Not.Null);
            Assert.That(first, Is.Not.SameAs(second));
        }

        #endregion

        #region Scoped Mode

        [Test]
        public void ScopedProviderModeUsesDedicatedScopeAndCachesServiceTest()
        {
            var sp = new ServiceCollection()
                .AddScoped<IScopedMarkerService, ScopedMarkerService>()
                .BuildServiceProvider();

            var instance = new ServiceWithScopedProviderMode(sp);

            var first = instance.Scoped;
            var second = instance.Scoped;

            Assert.That(first, Is.Not.Null);
            Assert.That(second, Is.Not.Null);
            Assert.That(first, Is.SameAs(second));
        }

        [Test]
        public void ScopedProviderScopeIsIsolatedFromRootContainerTest()
        {
            var sp = new ServiceCollection()
                .AddScoped<IScopedMarkerService, ScopedMarkerService>()
                .BuildServiceProvider();

            // The owner's dedicated scope produces a marker different from
            // resolving the same scoped service against the root provider's scope.
            var instance = new ServiceWithScopedProviderMode(sp);

            using var rootScope = sp.CreateScope();
            var rootResolved = rootScope.ServiceProvider.GetRequiredService<IScopedMarkerService>();

            Assert.That(instance.Scoped.Id, Is.Not.EqualTo(rootResolved.Id));
        }

        #endregion

        #region Passive Mode

        [Test]
        public void GetterPassiveWhenServiceProviderNotSetTest()
        {
            // No SP available → aspect returns the backing field as-is.
            var instance = new ServiceWithInject();

            Assert.That(instance.Required, Is.Null);
            Assert.That(instance.Optional, Is.Null);
        }

        #endregion

        #region Standard DI Registration

        [Test]
        public void StandardDiRegistrationWorksTest()
        {
            var sp = new ServiceCollection()
                .AddSingleton<IRequiredService, RequiredServiceImpl>()
                .AddSingleton<ServiceWithConstructorSP>()
                .BuildServiceProvider();

            var instance = sp.GetRequiredService<ServiceWithConstructorSP>();

            Assert.That(instance.Required, Is.Not.Null);
            Assert.That(instance.Required.Name, Is.EqualTo("Required"));
        }

        #endregion
    }
}
