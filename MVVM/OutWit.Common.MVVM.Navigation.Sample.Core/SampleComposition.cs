using System;
using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Model;
using OutWit.Common.MVVM.Navigation.Modules.Utils;
using OutWit.Common.MVVM.Navigation.Sample.Core.Guards;
using OutWit.Common.MVVM.Navigation.Sample.Core.Modules;
using OutWit.Common.MVVM.Navigation.Sample.Core.Services;
using OutWit.Common.MVVM.Navigation.Sample.Core.ViewModels;
using OutWit.Common.MVVM.Navigation.Utils;

namespace OutWit.Common.MVVM.Navigation.Sample.Core
{
    /// <summary>
    /// Everything the two sample applications share: the same services, routes, guards,
    /// contributions and modules. What is left for each platform is its views, its dialog
    /// host and its window — which is the point the sample is making.
    /// </summary>
    public static class SampleComposition
    {
        #region Functions

        /// <summary>
        /// Registers the sample's services and navigation. The platform package
        /// (<c>AddAvaloniaNavigation</c> / <c>AddWpfNavigation</c>) is added by the application
        /// itself, before or after this call.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddSample(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.AddSingleton<StudyStore>();
            services.AddSingleton<BusyGuard>();
            services.AddSingleton<ApplicationViewModel>();

            services.AddNavigation(nav =>
            {
                nav.AddZone(Zones.NAVIGATION_BAR);
                nav.AddZone(Zones.MENU_FILE);

                nav.AddRoute<StudiesViewModel>(Routes.STUDIES);
                nav.AddRoute<StudyViewModel>(Routes.STUDY, NavigationRouteMode.Transient);
                nav.AddRoute<SettingsViewModel>(Routes.SETTINGS);

                // the same instance the Settings screen toggles
                nav.Services.AddSingleton<INavigationGuard>(provider => provider.GetRequiredService<BusyGuard>());

                nav.HistoryDepth = 20;
            });

            // Two modules, arriving two different ways. Reports is compiled in; Audit is a DLL
            // the application does not reference, staged into @Modules by the build and found
            // there by OutWit.Common.Plugins. Neither the shell nor this method names Audit.
            services.AddUiModules(modules =>
            {
                modules.AddModule<ReportsModule>();
            });

            return services;
        }

        /// <summary>
        /// The shell's own contributions: what the application puts into its zones before any
        /// module runs. Called after the container is built, alongside the modules.
        /// </summary>
        /// <param name="provider">The built container.</param>
        public static IServiceProvider AddSampleContributions(this IServiceProvider provider)
        {
            var contributions = provider.GetRequiredService<IContributionRegistry>();

            contributions.AddRange(new[]
            {
                new ContributionItem
                {
                    Zone = Zones.NAVIGATION_BAR,
                    Key = "Studies",
                    Order = 100,
                    Header = "Studies",
                    Icon = "🗂",
                    RouteKey = Routes.STUDIES
                },
                new ContributionItem
                {
                    Zone = Zones.NAVIGATION_BAR,
                    Key = "Settings",
                    Order = 900,
                    Header = "Settings",
                    Icon = "⚙",
                    RouteKey = Routes.SETTINGS
                },

                // a nested menu: the parent carries no route, the children do — and the
                // Reports module adds a third child without touching this code
                new ContributionItem
                {
                    Zone = Zones.MENU_FILE,
                    Key = "File.Open",
                    Order = 10,
                    Header = "Open"
                },
                new ContributionItem
                {
                    Zone = Zones.MENU_FILE,
                    Key = "File.Studies",
                    ParentKey = "File.Open",
                    Order = 10,
                    Header = "Studies…",
                    RouteKey = Routes.STUDIES
                },
                new ContributionItem
                {
                    Zone = Zones.MENU_FILE,
                    Key = "File.Settings",
                    Order = 20,
                    Header = "Settings",
                    RouteKey = Routes.SETTINGS
                }
            });

            return provider;
        }

        #endregion
    }
}
