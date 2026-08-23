using System;

namespace OutWit.Common.MVVM.Navigation.Model
{
    /// <summary>
    /// Describes the target of a navigation: which outlet, which route, with which
    /// parameters and why. Handed to <see cref="Interfaces.INavigationAware"/> and
    /// <see cref="Interfaces.INavigationGuard"/> both on the way in and on the way out —
    /// in both cases it describes where the navigation is going.
    /// </summary>
    public sealed class NavigationContext
    {
        #region Constructors

        /// <summary>
        /// Creates a context.
        /// </summary>
        /// <param name="outlet">The outlet name.</param>
        /// <param name="route">The target route.</param>
        /// <param name="parameters">The parameters; null means empty.</param>
        /// <param name="kind">Why the navigation is happening.</param>
        public NavigationContext(string outlet, NavigationRoute route, NavigationParameters? parameters, NavigationKind kind)
        {
            if (string.IsNullOrEmpty(outlet))
                throw new ArgumentException("Outlet name must be a non-empty string.", nameof(outlet));

            Outlet = outlet;
            Route = route ?? throw new ArgumentNullException(nameof(route));
            Parameters = parameters ?? NavigationParameters.EMPTY;
            Kind = kind;
        }

        #endregion

        #region Functions

        public override string ToString()
        {
            return $"{Kind} -> {Outlet}/{Route.Key} {Parameters}";
        }

        #endregion

        #region Properties

        /// <summary>
        /// The outlet the navigation targets.
        /// </summary>
        public string Outlet { get; }

        /// <summary>
        /// The target route.
        /// </summary>
        public NavigationRoute Route { get; }

        /// <summary>
        /// Shortcut for <c>Route.Key</c>.
        /// </summary>
        public string RouteKey => Route.Key;

        /// <summary>
        /// The parameters. Never null.
        /// </summary>
        public NavigationParameters Parameters { get; }

        /// <summary>
        /// Why the navigation is happening.
        /// </summary>
        public NavigationKind Kind { get; }

        #endregion
    }
}
