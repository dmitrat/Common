using System;
using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.DependencyInjection.Internal;

namespace OutWit.Common.DependencyInjection
{
    /// <summary>
    /// Resolves <see cref="InjectAttribute"/> properties on an object eagerly via reflection.
    /// Useful as a manual entry point in tests, factories, or scenarios where the
    /// aspect's lazy getter advice is not desirable.
    /// </summary>
    public static class PropertyInjector
    {
        #region Functions

        /// <summary>
        /// Populates every <see cref="InjectAttribute"/> property on <paramref name="instance"/>
        /// using the given <paramref name="serviceProvider"/>. Required properties throw
        /// when the service is not registered; optional properties are left at their default value.
        /// </summary>
        /// <param name="instance">The object whose <see cref="InjectAttribute"/> properties should be resolved.</param>
        /// <param name="serviceProvider">The service provider used for resolution.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="instance"/> or <paramref name="serviceProvider"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// A required service is not registered in the service provider, or an
        /// <see cref="InjectAttribute"/> property has no setter.
        /// </exception>
        public static void Inject(object instance, IServiceProvider serviceProvider)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
            if (serviceProvider == null)
                throw new ArgumentNullException(nameof(serviceProvider));

            var properties = InjectPropertyMetadata.GetMap(instance.GetType());

            foreach (var meta in properties.Values)
            {
                object? service = meta.IsRequired
                    ? serviceProvider.GetRequiredService(meta.ServiceType)
                    : serviceProvider.GetService(meta.ServiceType);

                if (service != null)
                    meta.Property.SetValue(instance, service);
            }
        }

        #endregion
    }
}
