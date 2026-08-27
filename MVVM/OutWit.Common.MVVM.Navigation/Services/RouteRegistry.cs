using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Model;

namespace OutWit.Common.MVVM.Navigation.Services
{
    /// <summary>
    /// Default <see cref="IRouteRegistry"/>: locked dictionaries that keep registration order.
    /// Routes and groups share one key space, and a group is one level deep — it lists
    /// routes, never other groups.
    /// </summary>
    public sealed class RouteRegistry : IRouteRegistry
    {
        #region Fields

        private readonly object m_sync = new();
        private readonly Dictionary<string, NavigationRoute> m_routes = new(StringComparer.Ordinal);
        private readonly List<NavigationRoute> m_order = new();
        private readonly Dictionary<string, NavigationGroup> m_groups = new(StringComparer.Ordinal);
        private readonly List<NavigationGroup> m_groupsOrder = new();

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the registry, pre-loaded with the routes and groups AddNavigation collected.
        /// </summary>
        /// <param name="options">What AddNavigation collected; null means none.</param>
        public RouteRegistry(NavigationOptions? options = null)
        {
            if (options == null)
                return;

            foreach (var route in options.Routes)
                Register(route);

            foreach (var group in options.Groups)
                RegisterGroup(group);
        }

        #endregion

        #region IRouteRegistry

        public void Register(NavigationRoute route)
        {
            if (route == null)
                throw new ArgumentNullException(nameof(route));

            lock (m_sync)
            {
                if (m_groups.ContainsKey(route.Key))
                    throw new InvalidOperationException($"'{route.Key}' is registered as a group. Route keys and group keys share one namespace.");

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

        public void RegisterGroup(NavigationGroup group)
        {
            if (group == null)
                throw new ArgumentNullException(nameof(group));

            lock (m_sync)
            {
                Validate(group);

                // A group only grows. Modules register in whichever order they load, so a
                // member added through AddToGroup before the owner declared the group has to
                // survive the declaration — and nothing ever unloads, so there is no removal
                // to provide. The declaration owns the default, the outlet and the metadata.
                var merged = m_groups.TryGetValue(group.Key, out var existing)
                    ? new NavigationGroup(group.Key, group.DefaultRouteKey, group.RouteKeys.Concat(existing.RouteKeys), group.Outlet, group.Metadata)
                    : group;

                Put(merged, existing);
            }
        }

        public void RegisterGroup(string key,
                                  string defaultRouteKey,
                                  IEnumerable<string>? routeKeys = null,
                                  string outlet = NavigationOutlets.MAIN,
                                  object? metadata = null)
        {
            RegisterGroup(new NavigationGroup(key, defaultRouteKey, routeKeys, outlet, metadata));
        }

        public void AddToGroup(string key, string routeKey)
        {
            if (string.IsNullOrEmpty(routeKey))
                throw new ArgumentException("Route key must be a non-empty string.", nameof(routeKey));

            lock (m_sync)
            {
                if (!m_groups.TryGetValue(key, out var existing))
                {
                    var created = new NavigationGroup(key, routeKey);
                    Validate(created);
                    Put(created, null);
                    return;
                }

                if (existing.Contains(routeKey))
                    return;

                var grown = new NavigationGroup(existing.Key, existing.DefaultRouteKey, existing.RouteKeys.Append(routeKey), existing.Outlet, existing.Metadata);
                Validate(grown);
                Put(grown, existing);
            }
        }

        public bool ContainsGroup(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            lock (m_sync)
                return m_groups.ContainsKey(key);
        }

        public bool TryGetGroup(string key, [NotNullWhen(true)] out NavigationGroup? group)
        {
            if (string.IsNullOrEmpty(key))
            {
                group = null;
                return false;
            }

            lock (m_sync)
                return m_groups.TryGetValue(key, out group);
        }

        #endregion

        #region Tools

        private void Put(NavigationGroup group, NavigationGroup? existing)
        {
            if (existing != null)
                m_groupsOrder[m_groupsOrder.IndexOf(existing)] = group;
            else
                m_groupsOrder.Add(group);

            m_groups[group.Key] = group;
        }

        /// <summary>
        /// One namespace and one level. Called under the lock.
        /// </summary>
        private void Validate(NavigationGroup group)
        {
            if (m_routes.ContainsKey(group.Key))
                throw new InvalidOperationException($"'{group.Key}' is registered as a route. Route keys and group keys share one namespace.");

            foreach (var other in m_groupsOrder)
            {
                if (other.Key != group.Key && other.Contains(group.Key))
                    throw new InvalidOperationException($"'{group.Key}' is a member of group '{other.Key}'. Groups do not nest.");
            }

            foreach (var routeKey in group.RouteKeys)
            {
                if (m_groups.ContainsKey(routeKey))
                    throw new InvalidOperationException($"Group '{group.Key}' lists '{routeKey}', which is a group. Groups do not nest.");
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

        public IReadOnlyList<NavigationGroup> Groups
        {
            get
            {
                lock (m_sync)
                    return m_groupsOrder.ToArray();
            }
        }

        #endregion
    }
}
