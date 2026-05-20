using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OutWit.Common.DependencyInjection.Tests.Mock;

namespace OutWit.Common.DependencyInjection.Tests
{
    /// <summary>
    /// Coverage for the eager reflection-based injector. The lazy aspect-based path
    /// is covered in <see cref="InjectAspectGetterTests"/>.
    /// </summary>
    [TestFixture]
    public class PropertyInjectorTests
    {
        #region Argument Guards

        [Test]
        public void InjectThrowsWhenInstanceIsNullTest()
        {
            var sp = new ServiceCollection().BuildServiceProvider();

            Assert.Throws<ArgumentNullException>(() =>
                PropertyInjector.Inject(null!, sp));
        }

        [Test]
        public void InjectThrowsWhenServiceProviderIsNullTest()
        {
            var instance = new ServiceWithInject();

            Assert.Throws<ArgumentNullException>(() =>
                PropertyInjector.Inject(instance, null!));
        }

        #endregion

        #region Required Property Tests

        [Test]
        public void RequiredPropertyResolvedTest()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IRequiredService, RequiredServiceImpl>();
            var sp = services.BuildServiceProvider();

            var instance = new ServiceWithInject();
            PropertyInjector.Inject(instance, sp);

            Assert.That(instance.Required, Is.Not.Null);
            Assert.That(instance.Required.Name, Is.EqualTo("Required"));
        }

        [Test]
        public void RequiredPropertyThrowsWhenNotRegisteredTest()
        {
            var sp = new ServiceCollection().BuildServiceProvider();
            var instance = new ServiceWithInject();

            Assert.Throws<InvalidOperationException>(() =>
                PropertyInjector.Inject(instance, sp));
        }

        #endregion

        #region Optional Property Tests

        [Test]
        public void OptionalPropertyResolvedWhenRegisteredTest()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IRequiredService, RequiredServiceImpl>();
            services.AddSingleton<IOptionalService, OptionalServiceImpl>();
            var sp = services.BuildServiceProvider();

            var instance = new ServiceWithInject();
            PropertyInjector.Inject(instance, sp);

            Assert.That(instance.Optional, Is.Not.Null);
            Assert.That(instance.Optional!.Value, Is.EqualTo(42));
        }

        [Test]
        public void OptionalPropertyNullWhenNotRegisteredTest()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IRequiredService, RequiredServiceImpl>();
            var sp = services.BuildServiceProvider();

            var instance = new ServiceWithInject();
            PropertyInjector.Inject(instance, sp);

            Assert.That(instance.Optional, Is.Null);
        }

        #endregion

        #region Explicit Requirement Tests

        [Test]
        public void ExplicitOptionalOnNonNullableWorksTest()
        {
            var sp = new ServiceCollection().BuildServiceProvider();
            var instance = new ServiceWithExplicitRequirement();

            // ForcedOptional is non-nullable but tagged Optional — no throw.
            // ForcedRequired is nullable but tagged Required — will throw.
            Assert.Throws<InvalidOperationException>(() =>
                PropertyInjector.Inject(instance, sp));
        }

        [Test]
        public void ExplicitRequiredOnNullableThrowsTest()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IRequiredService, RequiredServiceImpl>();
            // IOptionalService not registered, but ForcedRequired demands it.
            var sp = services.BuildServiceProvider();

            var instance = new ServiceWithExplicitRequirement();

            Assert.Throws<InvalidOperationException>(() =>
                PropertyInjector.Inject(instance, sp));
        }

        [Test]
        public void ExplicitRequirementsBothSatisfiedTest()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IRequiredService, RequiredServiceImpl>();
            services.AddSingleton<IOptionalService, OptionalServiceImpl>();
            var sp = services.BuildServiceProvider();

            var instance = new ServiceWithExplicitRequirement();
            PropertyInjector.Inject(instance, sp);

            Assert.That(instance.ForcedOptional, Is.Not.Null);
            Assert.That(instance.ForcedRequired, Is.Not.Null);
        }

        #endregion

        #region Non-Injected Property Tests

        [Test]
        public void NonInjectedPropertyUntouchedTest()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IRequiredService, RequiredServiceImpl>();
            var sp = services.BuildServiceProvider();

            var instance = new ServiceWithInject();
            PropertyInjector.Inject(instance, sp);

            Assert.That(instance.NotInjected, Is.EqualTo("untouched"));
        }

        #endregion

        #region Inheritance Tests

        [Test]
        public void InheritedPropertiesResolvedTest()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IRequiredService, RequiredServiceImpl>();
            services.AddSingleton<IOptionalService, OptionalServiceImpl>();
            var sp = services.BuildServiceProvider();

            var instance = new DerivedServiceWithInject();
            PropertyInjector.Inject(instance, sp);

            Assert.That(instance.BaseRequired, Is.Not.Null);
            Assert.That(instance.DerivedOptional, Is.Not.Null);
        }

        #endregion

        #region Generic Service Tests

        [Test]
        public void GenericServiceTypeResolvedTest()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            var sp = services.BuildServiceProvider();

            var instance = new ServiceWithLogger();
            PropertyInjector.Inject(instance, sp);

            Assert.That(instance.Logger, Is.Not.Null);
        }

        #endregion

        #region Idempotency Tests

        [Test]
        public void DoubleInjectIdempotentTest()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IRequiredService, RequiredServiceImpl>();
            var sp = services.BuildServiceProvider();

            var instance = new ServiceWithInject();
            PropertyInjector.Inject(instance, sp);
            PropertyInjector.Inject(instance, sp); // Second call should not throw.

            Assert.That(instance.Required, Is.Not.Null);
        }

        #endregion

        #region No-setter Diagnostics

        [Test]
        public void InjectPropertyWithoutSetterThrowsTest()
        {
            // [Inject] on a property with no setter at all is a programmer error —
            // metadata building must surface it loudly instead of silently skipping.
            var sp = new ServiceCollection()
                .AddSingleton<IRequiredService, RequiredServiceImpl>()
                .BuildServiceProvider();

            var instance = new ServiceWithReadOnlyInjectProperty();

            var ex = Assert.Throws<InvalidOperationException>(() =>
                PropertyInjector.Inject(instance, sp));

            Assert.That(ex!.Message, Does.Contain("must have a setter"));
        }

        #endregion
    }
}
