using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Model;

namespace OutWit.Common.MVVM.Navigation.Services
{
    /// <summary>
    /// Default <see cref="IRouteRegistry"/>: a locked dictionary that keeps registration order.
    /// </summary>
    public sealed class RouteRegistry : IRouteRegistry
    {
        #region Fields

        private readonly object m_sync = new();
        private readonly Dictionary<string, NavigationRoute> m_routes = new(StringComparer.Ordinal);
        private readonly List<NavigationRoute> m_order = new();

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the registry, pre-loaded with the routes AddNavigation collected.
        /// </summary>
        /// <param name="options">What AddNavigation collected; null means none.</param>
        public RouteRegistry(NavigationOptions? options = null)
        {
            if (options == null)
                return;

            foreach (var route in options.Routes)
                Register(route);
        }

        #endregion

        #region IRouteRegistry

        public void Register(NavigationRoute route)
        {
            if (route == null)
                throw new ArgumentNullException(nameof(route));

            lock (m_sync)
            {
                if (m_routes.TryGetValue(route.Key, out var existing))
                    m_order[m_order.IndexOf(existing)] = route;
                else
                    m_order.Add(route);

                m_routes[route.Key] = route;
            }
        }

        public void Register<TViewModel>(string key,
                                         NavigationRouteMode mode = NavigationRouteMode.Cached,
                                         string outlet = NavigationOutlets.MAIN,
                                         object? metadata = null)
            where TViewModel : class
        {
            Register(new NavigationRoute(key, typeof(TViewModel), mode, outlet, metadata));
        }

        public bool Contains(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            lock (m_sync)
                return m_routes.ContainsKey(key);
        }

        public bool TryGet(string key, [NotNullWhen(true)] out NavigationRoute? route)
        {
            if (string.IsNullOrEmpty(key))
            {
                route = null;
                return false;
            }

            lock (m_sync)
                return m_routes.TryGetValue(key, out route);
        }

        public bool TryGetFor<TViewModel>([NotNullWhen(true)] out NavigationRoute? route)
            where TViewModel : class
        {
            lock (m_sync)
            {
                route = m_order.FirstOrDefault(candidate => candidate.ViewModelType == typeof(TViewModel));
                return route != null;
            }
        }

        #endregion

        #region Properties

        public IReadOnlyList<NavigationRoute> Routes
        {
            get
            {
                lock (m_sync)
                    return m_order.ToArray();
            }
        }

        #endregion
    }
}
