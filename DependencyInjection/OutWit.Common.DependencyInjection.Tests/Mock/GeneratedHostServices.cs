using OutWit.Common.DependencyInjection.Tests.Mock;

namespace OutWit.Common.DependencyInjection.Tests.Mock
{
    /// <summary>
    /// Simple host — the generator must emit the
    /// <c>(IServiceProvider)</c> ctor and the matching <c>Services</c> property,
    /// so the only thing this file declares is the <c>[Inject]</c> property.
    /// </summary>
    [InjectableHost]
    public partial class GeneratedSimpleHost
    {
        [Inject]
        public IRequiredService Required { get; set; } = null!;
    }

    /// <summary>
    /// Host using only alias attributes — verifies the generator's emitted
    /// service-provider field is found regardless of which [Inject*] variant
    /// triggers the aspect.
    /// </summary>
    [InjectableHost]
    public partial class GeneratedAliasHost
    {
        [InjectScoped]
        public IScopedMarkerService Scoped { get; set; } = null!;

        [InjectOptional]
        public IRequiredService MaybeRequired { get; set; } = null!;
    }

    /// <summary>
    /// Host inside a derived class — the base class has its own
    /// <c>[Inject]</c> property; the generator only writes ctor + Services on
    /// the derived class. The aspect's locator must still find Services via
    /// the type hierarchy when reading the base property.
    /// </summary>
    public class GeneratedHostBase
    {
        [Inject]
        public IRequiredService BaseRequired { get; set; } = null!;
    }

    [InjectableHost]
    public partial class GeneratedDerivedHost : GeneratedHostBase
    {
        [Inject]
        public IOptionalService? Optional { get; set; }
    }

    /// <summary>
    /// Internal accessibility — the generator emits an internal constructor.
    /// </summary>
    [InjectableHost]
    internal partial class GeneratedInternalHost
    {
        [Inject]
        public IRequiredService Required { get; set; } = null!;
    }
}
