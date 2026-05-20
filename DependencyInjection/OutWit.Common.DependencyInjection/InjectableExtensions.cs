using System;

namespace OutWit.Common.DependencyInjection
{
    /// <summary>
    /// Extension methods for the <see cref="IInjectable"/> mixin added by
    /// <see cref="InjectAspect"/>.
    /// </summary>
    public static class InjectableExtensions
    {
        /// <summary>
        /// Sets the <see cref="IServiceProvider"/> used to resolve
        /// <see cref="InjectAttribute"/> properties on <paramref name="instance"/>.
        /// Call when field auto-discovery does not apply (the class does not store
        /// the service provider in a field). Typical usage:
        /// <code>this.InitInject(serviceProvider);</code>
        /// </summary>
        /// <param name="instance">The object with at least one <see cref="InjectAttribute"/> property.</param>
        /// <param name="serviceProvider">The service provider used for resolution.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="instance"/> or <paramref name="serviceProvider"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// <paramref name="instance"/> does not implement <see cref="IInjectable"/>
        /// (no <see cref="InjectAttribute"/> property triggered the mixin).
        /// </exception>
        public static void InitInject(this object instance, IServiceProvider serviceProvider)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
            if (serviceProvider == null)
                throw new ArgumentNullException(nameof(serviceProvider));

            if (instance is IInjectable injectable)
            {
                injectable.ServiceProvider = serviceProvider;
                return;
            }

            throw new InvalidOperationException(
                $"Type '{instance.GetType().Name}' does not implement IInjectable. " +
                "Ensure at least one property is annotated with [Inject].");
        }
    }
}
