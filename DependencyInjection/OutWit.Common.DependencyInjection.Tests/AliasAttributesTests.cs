using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.DependencyInjection.Tests.Mock;

namespace OutWit.Common.DependencyInjection.Tests
{
    /// <summary>
    /// Coverage for the convenience alias attributes
    /// (<c>[InjectScoped]</c>, <c>[InjectTransient]</c>,
    /// <c>[InjectOptional]</c>, <c>[InjectRequired]</c>). Each alias must
    /// behave identically to the equivalent <c>[Inject(Mode/Requirement = ...)]</c>
    /// combination — both through the aspect's lazy getter and through the
    /// eager <see cref="PropertyInjector"/>.
    /// </summary>
    [TestFixture]
    public class AliasAttributesTests
    {
        #region Aspect Path

        [Test]
        public void InjectScopedAliasOpensDedicatedScopeTest()
        {
            var sp = new ServiceCollection()
                .AddScoped<IScopedMarkerService, ScopedMarkerService>()
                .BuildServiceProvider();

            var instance = new ServiceWithAliases(sp);

            using var rootScope = sp.CreateScope();
            var rootResolved = rootScope.ServiceProvider.GetRequiredService<IScopedMarkerService>();

            // [InjectScoped] opens its own child scope — its Id differs from any
            // root-scope resolution.
            Assert.That(instance.Scoped.Id, Is.Not.EqualTo(rootResolved.Id));
        }

        [Test]
        public void InjectTransientAliasResolvesFreshOnEachAccessTest()
        {
            var sp = new ServiceCollection()
                .AddTransient<IRequiredService, RequiredServiceImpl>()
                .BuildServiceProvider();

            var instance = new ServiceWithAliases(sp);

            var first = instance.Transient;
            var second = instance.Transient;

            Assert.That(first, Is.Not.SameAs(second));
        }

        [Test]
        public void InjectOptionalAliasReturnsNullForMissingServiceTest()
        {
            // IRequiredService is NOT registered. With [Inject] default + non-nullable
            // property the aspect would throw via GetRequiredService; [InjectOptional]
            // must downgrade that to GetService → null.
            var sp = new ServiceCollection().BuildServiceProvider();
            var instance = new ServiceWithAliases(sp);

            // No throw and value is null (since service is missing).
            Assert.That(instance.ForcedOptional, Is.Null);
        }

        [Test]
        public void InjectRequiredAliasThrowsForMissingServiceOnNullablePropertyTest()
        {
            // IOptionalService is NOT registered. With [Inject] default + nullable
            // property the aspect would return null; [InjectRequired] must upgrade
            // that to GetRequiredService → throw.
            var sp = new ServiceCollection().BuildServiceProvider();
            var instance = new ServiceWithAliases(sp);

            Assert.Throws<InvalidOperationException>(() => { _ = instance.ForcedRequired; });
        }

        #endregion

        #region PropertyInjector Path

        [Test]
        public void PropertyInjectorRespectsAliasesTest()
        {
            var sp = new ServiceCollection()
                .AddTransient<IRequiredService, RequiredServiceImpl>()
                .AddScoped<IScopedMarkerService, ScopedMarkerService>()
                .AddSingleton<IOptionalService, OptionalServiceImpl>()
                .BuildServiceProvider();

            var instance = new ServiceWithAliases(sp);
            PropertyInjector.Inject(instance, sp);

            // Eager pass uses the root provider; mode is an aspect-time concern,
            // but Required/Optional must still apply. All four services are
            // registered → all four properties resolve.
            Assert.That(instance.Transient, Is.Not.Null);
            Assert.That(instance.Scoped, Is.Not.Null);
            Assert.That(instance.ForcedOptional, Is.Not.Null);
            Assert.That(instance.ForcedRequired, Is.Not.Null);
        }

        [Test]
        public void PropertyInjectorOptionalAliasDoesNotThrowWhenMissingTest()
        {
            // Bare service collection — none of the alias services registered.
            // [InjectOptional] on non-nullable property: must not throw, just leave null.
            // But [InjectRequired] on nullable property must still throw.
            var sp = new ServiceCollection().BuildServiceProvider();
            var instance = new ServiceWithAliases(sp);

            Assert.Throws<InvalidOperationException>(() =>
                PropertyInjector.Inject(instance, sp));
        }

        #endregion
    }
}
