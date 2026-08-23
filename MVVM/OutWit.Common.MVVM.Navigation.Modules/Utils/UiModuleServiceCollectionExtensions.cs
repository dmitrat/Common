using System;
using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.MVVM.Navigation.Modules.Model;

namespace OutWit.Common.MVVM.Navigation.Modules.Utils
{
    /// <summary>
    /// Registers the UI-module axis in <see cref="IServiceCollection"/>.
    /// </summary>
    public static class UiModuleServiceCollectionExtensions
    {
        #region Functions

        /// <summary>
        /// Loads UI modules from <see cref="UiModulesOptions.DEFAULT_FOLDER"/> (and/or the
        /// compiled-in ones the options name), lets them register their services, and
        /// registers the <see cref="UiModules"/> axis as a singleton. After the container is
        /// built, call <c>provider.GetRequiredService&lt;UiModules&gt;().InitializeAsync(provider)</c>.
        /// Call after <c>AddNavigation</c>.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Folder, compiled-in modules, logger.</param>
        /// <returns>The service collection.</returns>
        /// <exception cref="AggregateException">The module folder could not be scanned; see the inner exceptions.</exception>
        /// <example>
        /// <code>
        /// services.AddUiModules();                                            // @Modules next to the app
        /// services.AddUiModules(o => o.Folder = "Plugins/UI");
        /// services.AddUiModules(o => { o.ScanFolder = false; o.AddModule&lt;SummaryModule&gt;(); });
        /// </code>
        /// </example>
        public static IServiceCollection AddUiModules(this IServiceCollection services, Action<UiModulesOptions>? configure = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            var options = new UiModulesOptions();
            configure?.Invoke(options);

            var modules = new UiModules(options);
            modules.RegisterServices(services);

            return services;
        }

        #endregion
    }
}
