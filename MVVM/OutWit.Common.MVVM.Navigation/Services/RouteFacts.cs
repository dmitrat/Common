using System;
using OutWit.Common.MVVM.Navigation.Interfaces;

namespace OutWit.Common.MVVM.Navigation.Services
{
    /// <summary>
    /// <see cref="IRouteFacts"/> answered from the route registry.
    /// </summary>
    internal sealed class RouteFacts : IRouteFacts
    {
        #region Fields

        private readonly IRouteRegistry m_routes;

        #endregion

        #region Constructors

        public RouteFacts(IRouteRegistry routes)
        {
            m_routes = routes ?? throw new ArgumentNullException(nameof(routes));
        }

        #endregion

        #region IRouteFacts

        public string? DefaultOutletOf(string key)
        {
            if (m_routes.TryGet(key, out var route))
                return route.Outlet;

            if (m_routes.TryGetGroup(key, out var group))
                return group.Outlet;

            return null;
        }

        public bool GroupContains(string groupKey, string routeKey)
        {
            return m_routes.TryGetGroup(groupKey, out var group) && group.Contains(routeKey);
        }

        #endregion
    }
}
