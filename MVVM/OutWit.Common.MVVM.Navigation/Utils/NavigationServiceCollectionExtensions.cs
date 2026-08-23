using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OutWit.Common.MVVM.Abstractions;
using OutWit.Common.MVVM.Interfaces;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Services;

namespace OutWit.Common.MVVM.Navigation.Utils
{
    /// <summary>
    /// Registers navigation in <see cref="IServiceCollection"/>.
    /// </summary>
    public static class NavigationServiceCollectionExtensions
    {
        #region Functions

        /// <summary>
        /// Registers the navigation service, the route, view and contribution registries and
        /// the dialog service as singletons. Call once.
        /// </summary>
        /// <remarks>
        /// <see cref="IDispatcher"/> is registered as <see cref="DispatcherImmediate"/> only if
        /// nothing else registered one — a platform package (AddAvaloniaNavigation,
        /// AddWpfNavigation) supplies the real one, before or after this call.
        /// <see cref="IDialogService"/> resolves only once the platform package has supplied
        /// <see cref="IViewFactory"/> and <see cref="IDialogHost"/>.
        /// </remarks>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Declares outlets, zones, routes, views and guards.</param>
        /// <returns>The service collection.</returns>
        /// <example>
        /// <code>
        /// services.AddNavigation(nav =>
        /// {
        ///     nav.AddOutlet("Inspector");
        ///     nav.AddRoute&lt;StudiesViewModel&gt;(Routes.STUDIES);
        ///     nav.AddRoute&lt;StudyViewModel&gt;(Routes.STUDY, NavigationRouteMode.Transient);
        ///     nav.AddGuard&lt;LicenseGuard&gt;();
        ///     nav.HistoryDepth = 20;
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddNavigation(this IServiceCollection services, Action<NavigationBuilder>? configure = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            var builder = new NavigationBuilder(services);
            configure?.Invoke(builder);

            services.AddSingleton(builder.Options);
            services.TryAddSingleton<IDispatcher, DispatcherImmediate>();
            services.TryAddSingleton<IRouteRegistry, RouteRegistry>();
            services.TryAddSingleton<IViewRegistry, ViewRegistry>();
            services.TryAddSingleton<INavigationService, NavigationService>();
            services.TryAddSingleton<IContributionRegistry, ContributionRegistry>();
            services.TryAddSingleton<IDialogService, DialogService>();

            return services;
        }

        #endregion
    }
}
