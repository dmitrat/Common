using System;
using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.Settings.Configuration;
using OutWit.Common.Settings.Interfaces;

namespace OutWit.Common.Settings
{
    /// <summary>
    /// Extension methods for registering settings in <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        #region Functions

        /// <summary>
        /// Configures a settings manager and registers it along with all container types as singletons.
        /// Automatically calls <see cref="ISettingsManager.Merge"/> and <see cref="ISettingsManager.Load"/>
        /// after building.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Action to configure the <see cref="SettingsBuilder"/>.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddSettings(settings => settings
        ///     .AddCustomSerializers()
        ///     .UseJson()
        ///     .RegisterContainer&lt;IAppSettings, ApplicationSettings&gt;()
        ///     .RegisterContainer&lt;NetworkSettings&gt;()
        /// );
        ///
        /// // Resolve typed container from DI:
        /// var appSettings = provider.GetRequiredService&lt;IAppSettings&gt;();
        /// </code>
        /// </example>
        public static IServiceCollection AddSettings(
            this IServiceCollection services,
            Action<SettingsBuilder> configure)
        {
            var builder = new SettingsBuilder();
            configure(builder);

            var manager = builder.Build();
            manager.Merge();
            manager.Load();

            services.AddSingleton<ISettingsManager>(manager);

            foreach (var (serviceType, implType) in builder.ContainerRegistrations)
            {
                services.AddSingleton(serviceType, _ =>
                    Activator.CreateInstance(implType, manager)!);
            }

            return services;
        }

        #endregion
    }
}
