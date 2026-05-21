using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.DependencyInjection.Tests.Mock;

namespace OutWit.Common.DependencyInjection.Tests
{
    /// <summary>
    /// End-to-end coverage for the <c>InjectableHostGenerator</c>: the generator
    /// runs during test compilation; if it works the mocks under
    /// <c>Mock/GeneratedHostServices.cs</c> compile (ctor + Services) and the
    /// aspect resolves their [Inject] properties as usual.
    /// </summary>
    [TestFixture]
    public class InjectableHostGeneratorTests
    {
        [Test]
        public void GeneratedCtorTakesServiceProviderAndAspectResolvesPropertiesTest()
        {
            var sp = new ServiceCollection()
                .AddSingleton<IRequiredService, RequiredServiceImpl>()
                .BuildServiceProvider();

            // The (IServiceProvider) ctor is emitted by the generator.
            var instance = new GeneratedSimpleHost(sp);

            Assert.That(instance.Required, Is.Not.Null);
            Assert.That(instance.Required.Name, Is.EqualTo("Required"));
        }

        [Test]
        public void GeneratedHostExposesIInjectableMixinTest()
        {
            // The aspect still mixes IInjectable in even though the ctor is generated.
            var sp = new ServiceCollection().BuildServiceProvider();
            var instance = new GeneratedSimpleHost(sp);

            Assert.That(instance, Is.InstanceOf<IInjectable>());
        }

        [Test]
        public void GeneratedHostWorksWithAliasesTest()
        {
            var sp = new ServiceCollection()
                .AddScoped<IScopedMarkerService, ScopedMarkerService>()
                .BuildServiceProvider();

            var instance = new GeneratedAliasHost(sp);

            Assert.That(instance.Scoped, Is.Not.Null);
            // [InjectOptional] downgrades the missing required service to null.
            Assert.That(instance.MaybeRequired, Is.Null);
        }

        [Test]
        public void GeneratedHostResolvesBaseClassInjectPropertiesTest()
        {
            // Generator emits Services on the derived class; locator must walk
            // the hierarchy from the base class's [Inject] property up to find it.
            var sp = new ServiceCollection()
                .AddSingleton<IRequiredService, RequiredServiceImpl>()
                .AddSingleton<IOptionalService, OptionalServiceImpl>()
                .BuildServiceProvider();

            var instance = new GeneratedDerivedHost(sp);

            Assert.That(instance.BaseRequired, Is.Not.Null);
            Assert.That(instance.Optional, Is.Not.Null);
        }

        [Test]
        public void GeneratedInternalHostCompilesAndWorksTest()
        {
            // The generator emits a public ctor on internal hosts so the
            // `new GeneratedInternalHost(sp)` path works from within the
            // same assembly just like a public host.
            var sp = new ServiceCollection()
                .AddSingleton<IRequiredService, RequiredServiceImpl>()
                .BuildServiceProvider();

            var instance = new GeneratedInternalHost(sp);

            Assert.That(instance.Required, Is.Not.Null);
        }

        [Test]
        public void GeneratedHostWorksWithDiContainerRegistrationTest()
        {
            var sp = new ServiceCollection()
                .AddSingleton<IRequiredService, RequiredServiceImpl>()
                .AddSingleton<GeneratedSimpleHost>()
                .BuildServiceProvider();

            var instance = sp.GetRequiredService<GeneratedSimpleHost>();

            Assert.That(instance.Required, Is.Not.Null);
        }

        [Test]
        public void GeneratedInternalHostResolvesViaPlainAddSingletonTest()
        {
            // Regression: before the generator emitted a `public` ctor
            // unconditionally, an `internal` [InjectableHost] type registered
            // with plain `AddSingleton<T>()` failed at resolve time with
            // "A suitable constructor for type 'T' could not be located"
            // because MS.DI's default activator only considers public ctors.
            // This is the exact failure mode that motivated TI-18 in WitCloud's
            // test-infrastructure audit (@Docs/Audit/test-infrastructure.md).
            var sp = new ServiceCollection()
                .AddSingleton<IRequiredService, RequiredServiceImpl>()
                .AddSingleton<GeneratedInternalHost>()
                .BuildServiceProvider();

            var instance = sp.GetRequiredService<GeneratedInternalHost>();

            Assert.That(instance.Required, Is.Not.Null);
            Assert.That(instance.Required.Name, Is.EqualTo("Required"));
        }
    }
}
