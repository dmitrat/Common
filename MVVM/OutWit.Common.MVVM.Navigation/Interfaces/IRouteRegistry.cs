using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using OutWit.Common.MVVM.Navigation.Model;

namespace OutWit.Common.MVVM.Navigation.Interfaces
{
    /// <summary>
    /// Routes known to the application. A singleton, safe to use from any thread:
    /// modules register in OnInitialized, after the container is built.
    /// </summary>
    public interface IRouteRegistry
    {
        #region Functions

        /// <summary>
        /// Registers a route. A route with the same key is replaced.
        /// </summary>
        /// <param name="route">The route.</param>
        void Register(NavigationRoute route);

        /// <summary>
        /// Registers a route for a view model type. A route with the same key is replaced.
        /// </summary>
        /// <typeparam name="TViewModel">The view model type.</typeparam>
        /// <param name="key">The route key.</param>
        /// <param name="mode">How the view model is created and kept.</param>
        /// <param name="outlet">The outlet the route targets when the caller names none.</param>
        /// <param name="metadata">Opaque data for guards, zones and the application.</param>
        void Register<TViewModel>(string key,
                                  NavigationRouteMode mode = NavigationRouteMode.Cached,
                                  string outlet = NavigationOutlets.MAIN,
                                  object? metadata = null)
            where TViewModel : class;

        /// <summary>
        /// Tells whether a route with the given key is registered.
        /// </summary>
        /// <param name="key">The route key.</param>
        /// <returns>True when registered.</returns>
        bool Contains(string key);

        /// <summary>
        /// Looks a route up by key.
        /// </summary>
        /// <param name="key">The route key.</param>
        /// <param name="route">The route.</param>
        /// <returns>True when found.</returns>
        bool TryGet(string key, [NotNullWhen(true)] out NavigationRoute? route);

        /// <summary>
        /// Looks up the first route registered for a view model type.
        /// </summary>
        /// <typeparam name="TViewModel">The view model type.</typeparam>
        /// <param name="route">The route.</param>
        /// <returns>True when found.</returns>
        bool TryGetFor<TViewModel>([NotNullWhen(true)] out NavigationRoute? route)
            where TViewModel : class;

        #endregion

        #region Properties

        /// <summary>
        /// All routes, in registration order.
        /// </summary>
        IReadOnlyList<NavigationRoute> Routes { get; }

        #endregion
    }
}
