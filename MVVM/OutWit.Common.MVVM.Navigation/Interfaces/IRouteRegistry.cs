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

        /// <summary>
        /// Declares a group of routes with a default. Declaring a key again replaces the
        /// default, the outlet and the metadata; members already added stay, because modules
        /// register in whichever order they load. Group keys and route keys share one
        /// namespace, and a group lists routes only — never another group.
        /// </summary>
        /// <param name="group">The group.</param>
        /// <exception cref="InvalidOperationException">The key is a route, a member of another group, or a member is itself a group.</exception>
        void RegisterGroup(NavigationGroup group);

        /// <summary>
        /// Declares a group of routes with a default. See <see cref="RegisterGroup(NavigationGroup)"/>.
        /// </summary>
        /// <param name="key">The group key.</param>
        /// <param name="defaultRouteKey">The route opened when nothing is remembered for the outlet.</param>
        /// <param name="routeKeys">The member routes; the default is always one of them.</param>
        /// <param name="outlet">The outlet the group targets when the caller names none.</param>
        /// <param name="metadata">Opaque data for guards, zones and the application.</param>
        /// <exception cref="InvalidOperationException">The key is a route, a member of another group, or a member is itself a group.</exception>
        void RegisterGroup(string key,
                           string defaultRouteKey,
                           IEnumerable<string>? routeKeys = null,
                           string outlet = NavigationOutlets.MAIN,
                           object? metadata = null);

        /// <summary>
        /// Adds a route to a group. A group not declared yet is created with the route as its
        /// default, so a module can extend a section before the section's owner has loaded.
        /// </summary>
        /// <param name="key">The group key.</param>
        /// <param name="routeKey">The route to add; adding a member again does nothing.</param>
        /// <exception cref="InvalidOperationException">The key is a route, or the route is a group.</exception>
        void AddToGroup(string key, string routeKey);

        /// <summary>
        /// Tells whether a group with the given key is declared.
        /// </summary>
        /// <param name="key">The group key.</param>
        /// <returns>True when declared.</returns>
        bool ContainsGroup(string key);

        /// <summary>
        /// Looks a group up by key.
        /// </summary>
        /// <param name="key">The group key.</param>
        /// <param name="group">The group.</param>
        /// <returns>True when found.</returns>
        bool TryGetGroup(string key, [NotNullWhen(true)] out NavigationGroup? group);

        #endregion

        #region Properties

        /// <summary>
        /// All routes, in registration order.
        /// </summary>
        IReadOnlyList<NavigationRoute> Routes { get; }

        /// <summary>
        /// All groups, in declaration order.
        /// </summary>
        IReadOnlyList<NavigationGroup> Groups { get; }

        #endregion
    }
}
