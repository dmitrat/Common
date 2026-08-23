using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Model;

namespace OutWit.Common.MVVM.Navigation.Utils
{
    /// <summary>
    /// What <c>services.AddNavigation(nav => ...)</c> hands the application. Collects
    /// outlets, zones, routes and views into <see cref="NavigationOptions"/> and registers
    /// guards straight into the service collection.
    /// </summary>
    public sealed class NavigationBuilder
    {
        #region Constructors

        internal NavigationBuilder(IServiceCollection services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
            Options = new NavigationOptions();
        }

        #endregion

        #region Functions

        /// <summary>
        /// Declares an outlet. <see cref="NavigationOutlets.MAIN"/> is declared by default.
        /// </summary>
        /// <param name="name">The outlet name.</param>
        /// <returns>This builder.</returns>
        public NavigationBuilder AddOutlet(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Outlet name must be a non-empty string.", nameof(name));

            if (!Options.Outlets.Contains(name))
                Options.Outlets.Add(name);

            return this;
        }

        /// <summary>
        /// Declares a zone up front. Zones are also created on first use, so this is optional.
        /// </summary>
        /// <param name="name">The zone name.</param>
        /// <returns>This builder.</returns>
        public NavigationBuilder AddZone(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Zone name must be a non-empty string.", nameof(name));

            if (!Options.Zones.Contains(name))
                Options.Zones.Add(name);

            return this;
        }

        /// <summary>
        /// Registers a route.
        /// </summary>
        /// <param name="route">The route.</param>
        /// <returns>This builder.</returns>
        public NavigationBuilder AddRoute(NavigationRoute route)
        {
            Options.Routes.Add(route ?? throw new ArgumentNullException(nameof(route)));
            return this;
        }

        /// <summary>
        /// Registers a route for a view model type.
        /// </summary>
        /// <typeparam name="TViewModel">The view model type.</typeparam>
        /// <param name="key">The route key.</param>
        /// <param name="mode">How the view model is created and kept.</param>
        /// <param name="outlet">The outlet the route targets when the caller names none.</param>
        /// <param name="metadata">Opaque data for guards, zones and the application.</param>
        /// <returns>This builder.</returns>
        public NavigationBuilder AddRoute<TViewModel>(string key,
                                                      NavigationRouteMode mode = NavigationRouteMode.Cached,
                                                      string outlet = NavigationOutlets.MAIN,
                                                      object? metadata = null)
            where TViewModel : class
        {
            return AddRoute(new NavigationRoute(key, typeof(TViewModel), mode, outlet, metadata));
        }

        /// <summary>
        /// Maps a view model type to a view type. Optional where the platform's naming
        /// convention finds the view; required under trimming or AOT.
        /// </summary>
        /// <typeparam name="TViewModel">The view model type.</typeparam>
        /// <typeparam name="TView">The view type.</typeparam>
        /// <returns>This builder.</returns>
        public NavigationBuilder AddView<TViewModel, TView>()
            where TViewModel : class
            where TView : class
        {
            Options.Views.Add(new KeyValuePair<Type, Type>(typeof(TViewModel), typeof(TView)));
            return this;
        }

        /// <summary>
        /// Registers a global guard: a singleton asked about every navigation in every outlet.
        /// </summary>
        /// <typeparam name="TGuard">The guard type.</typeparam>
        /// <returns>This builder.</returns>
        public NavigationBuilder AddGuard<TGuard>()
            where TGuard : class, INavigationGuard
        {
            Services.AddSingleton<INavigationGuard, TGuard>();
            return this;
        }

        /// <summary>
        /// Registers a global guard instance.
        /// </summary>
        /// <param name="guard">The guard.</param>
        /// <returns>This builder.</returns>
        public NavigationBuilder AddGuard(INavigationGuard guard)
        {
            Services.AddSingleton(guard ?? throw new ArgumentNullException(nameof(guard)));
            return this;
        }

        #endregion

        #region Properties

        /// <summary>
        /// The service collection, for extension packages to hang their registrations on.
        /// </summary>
        public IServiceCollection Services { get; }

        /// <summary>
        /// Maximum journal entries per outlet. Zero disables the journal.
        /// </summary>
        public int HistoryDepth
        {
            get => Options.HistoryDepth;
            set => Options.HistoryDepth = Math.Max(0, value);
        }

        internal NavigationOptions Options { get; }

        #endregion
    }
}
