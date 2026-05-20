using OutWit.Common.DependencyInjection.Tests.Mock;

namespace OutWit.Common.DependencyInjection.Tests.Mock
{
    /// <summary>
    /// Service using the alias attributes (no inline <c>Mode = </c> / <c>Requirement = </c>).
    /// </summary>
    public class ServiceWithAliases
    {
        private readonly IServiceProvider m_serviceProvider;

        public ServiceWithAliases(IServiceProvider serviceProvider)
        {
            m_serviceProvider = serviceProvider;
        }

        [InjectScoped]
        public IScopedMarkerService Scoped { get; private set; } = null!;

        [InjectTransient]
        public IRequiredService Transient { get; private set; } = null!;

        [InjectOptional]
        public IRequiredService ForcedOptional { get; private set; } = null!;

        [InjectRequired]
        public IOptionalService? ForcedRequired { get; private set; }
    }
}
