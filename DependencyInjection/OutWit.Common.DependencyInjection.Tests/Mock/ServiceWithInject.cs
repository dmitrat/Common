using Microsoft.Extensions.Logging;

namespace OutWit.Common.DependencyInjection.Tests.Mock
{
    /// <summary>
    /// Basic class with required and optional [Inject] properties.
    /// </summary>
    public class ServiceWithInject
    {
        [Inject]
        public IRequiredService Required { get; private set; } = null!;

        [Inject]
        public IOptionalService? Optional { get; private set; }

        public string NotInjected { get; set; } = "untouched";
    }

    /// <summary>
    /// Class with explicit requirement overrides (decoupled from nullability).
    /// </summary>
    public class ServiceWithExplicitRequirement
    {
        [Inject(Requirement = InjectRequirement.Optional)]
        public IRequiredService ForcedOptional { get; private set; } = null!;

        [Inject(Requirement = InjectRequirement.Required)]
        public IOptionalService? ForcedRequired { get; private set; }
    }

    /// <summary>
    /// Class injecting an open-generic service type.
    /// </summary>
    public class ServiceWithLogger
    {
        [Inject]
        public ILogger<ServiceWithLogger> Logger { get; private set; } = null!;
    }

    /// <summary>
    /// Base class with an [Inject] property.
    /// </summary>
    public class BaseServiceWithInject
    {
        [Inject]
        public IRequiredService BaseRequired { get; private set; } = null!;
    }

    /// <summary>
    /// Derived class adding its own [Inject] property — verifies that the metadata
    /// scan walks the type hierarchy.
    /// </summary>
    public class DerivedServiceWithInject : BaseServiceWithInject
    {
        [Inject]
        public IOptionalService? DerivedOptional { get; private set; }
    }

    /// <summary>
    /// Class storing <see cref="IServiceProvider"/> in a field — the aspect's
    /// field-auto-discovery should locate it without any explicit hook-up.
    /// </summary>
    public class ServiceWithConstructorSP
    {
        private readonly IServiceProvider m_serviceProvider;

        public ServiceWithConstructorSP(IServiceProvider serviceProvider)
        {
            m_serviceProvider = serviceProvider;
        }

        [Inject]
        public IRequiredService Required { get; private set; } = null!;

        [Inject]
        public IOptionalService? Optional { get; private set; }
    }

    /// <summary>
    /// Class using <see cref="InjectableExtensions.InitInject"/> as the explicit
    /// fallback for setting the service provider.
    /// </summary>
    public class ServiceWithInitInject
    {
        public ServiceWithInitInject(IServiceProvider serviceProvider)
        {
            this.InitInject(serviceProvider);
        }

        [Inject]
        public IRequiredService Required { get; private set; } = null!;
    }

    /// <summary>
    /// Class with a transient [Inject] property — fresh resolution on every access.
    /// </summary>
    public class ServiceWithTransient
    {
        private readonly IServiceProvider m_serviceProvider;

        public ServiceWithTransient(IServiceProvider serviceProvider)
        {
            m_serviceProvider = serviceProvider;
        }

        [Inject(Mode = InjectMode.Transient)]
        public IRequiredService Required { get; private set; } = null!;
    }

    /// <summary>
    /// Class with a scoped [Inject] property — opens a dedicated child scope on
    /// first access and disposes it when the owner is disposed.
    /// </summary>
    public class ServiceWithScopedProviderMode : IDisposable
    {
        private readonly IServiceProvider m_serviceProvider;

        public ServiceWithScopedProviderMode(IServiceProvider serviceProvider)
        {
            m_serviceProvider = serviceProvider;
        }

        [Inject(Mode = InjectMode.Scoped)]
        public IScopedMarkerService Scoped { get; private set; } = null!;

        public void Dispose()
        {
        }
    }

    /// <summary>
    /// Class with an [Inject] property on a property without any setter at all —
    /// should fail metadata building.
    /// </summary>
    public class ServiceWithReadOnlyInjectProperty
    {
        [Inject]
        public IRequiredService Required => null!;
    }
}
