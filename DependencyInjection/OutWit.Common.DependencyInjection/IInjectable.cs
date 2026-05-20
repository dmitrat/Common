using System;

namespace OutWit.Common.DependencyInjection
{
    /// <summary>
    /// Mixin interface added by <see cref="InjectAspect"/> to every class that uses
    /// <see cref="InjectAttribute"/> properties. Carries the
    /// <see cref="IServiceProvider"/> used for lazy resolution of those properties.
    /// </summary>
    public interface IInjectable
    {
        /// <summary>
        /// Service provider used to resolve <see cref="InjectAttribute"/> properties.
        /// Set automatically when the target class stores an <see cref="IServiceProvider"/>
        /// in any instance field (field auto-discovery), or explicitly via
        /// <see cref="InjectableExtensions.InitInject"/>.
        /// </summary>
        IServiceProvider? ServiceProvider { get; set; }
    }
}
