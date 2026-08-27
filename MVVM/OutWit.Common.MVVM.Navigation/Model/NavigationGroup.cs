using System;
using System.Collections.Generic;
using System.Linq;
using OutWit.Common.Abstract;
using OutWit.Common.Attributes;
using OutWit.Common.Values;

namespace OutWit.Common.MVVM.Navigation.Model
{
    /// <summary>
    /// A named set of routes with a default. Navigating to the group's key opens the page of
    /// the group last shown in the outlet, or the default when none has been. A section of a
    /// navigation bar points at a group, so it opens on whichever page the user left it at
    /// without the section — or the module behind it — keeping track.
    /// </summary>
    public sealed class NavigationGroup : ModelBase
    {
        #region Constructors

        /// <summary>
        /// Creates a group. The default is always a member: when <paramref name="routeKeys"/>
        /// does not list it, it goes first.
        /// </summary>
        /// <param name="key">The group key. Group keys and route keys share one namespace.</param>
        /// <param name="defaultRouteKey">The route opened when nothing is remembered.</param>
        /// <param name="routeKeys">The member routes, in order; null means the default alone.</param>
        /// <param name="outlet">The outlet the group targets when the caller names none.</param>
        /// <param name="metadata">Opaque data for guards, zones and the application.</param>
        /// <exception cref="ArgumentException">A key is empty, or the group lists itself.</exception>
        public NavigationGroup(string key,
                               string defaultRouteKey,
                               IEnumerable<string>? routeKeys = null,
                               string outlet = NavigationOutlets.MAIN,
                               object? metadata = null)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Group key must be a non-empty string.", nameof(key));

            if (string.IsNullOrEmpty(defaultRouteKey))
                throw new ArgumentException("Default route key must be a non-empty string.", nameof(defaultRouteKey));

            if (string.IsNullOrEmpty(outlet))
                throw new ArgumentException("Outlet name must be a non-empty string.", nameof(outlet));

            var members = new List<string>();

            foreach (var routeKey in routeKeys ?? Enumerable.Empty<string>())
            {
                if (string.IsNullOrEmpty(routeKey))
                    throw new ArgumentException("Route keys must be non-empty strings.", nameof(routeKeys));

                if (!members.Contains(routeKey))
                    members.Add(routeKey);
            }

            if (!members.Contains(defaultRouteKey))
                members.Insert(0, defaultRouteKey);

            if (members.Contains(key))
                throw new ArgumentException($"Group '{key}' cannot contain itself.", nameof(routeKeys));

            Key = key;
            DefaultRouteKey = defaultRouteKey;
            RouteKeys = members.AsReadOnly();
            Outlet = outlet;
            Metadata = metadata;
        }

        #endregion

        #region Functions

        /// <summary>
        /// Tells whether a route belongs to the group.
        /// </summary>
        /// <param name="routeKey">The route key.</param>
        /// <returns>True when the route is a member.</returns>
        public bool Contains(string routeKey)
        {
            return routeKey != null && RouteKeys.Contains(routeKey);
        }

        #endregion

        #region ModelBase

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not NavigationGroup other)
                return false;

            return Key.Is(other.Key)
                   && DefaultRouteKey.Is(other.DefaultRouteKey)
                   && RouteKeys.SequenceEqual(other.RouteKeys)
                   && Outlet.Is(other.Outlet)
                   && Equals(Metadata, other.Metadata);
        }

        public override ModelBase Clone()
        {
            return new NavigationGroup(Key, DefaultRouteKey, RouteKeys, Outlet, Metadata);
        }

        #endregion

        #region Properties

        /// <summary>
        /// The group key.
        /// </summary>
        [ToString]
        public string Key { get; }

        /// <summary>
        /// The route opened when the outlet has not shown a page of the group yet.
        /// </summary>
        [ToString]
        public string DefaultRouteKey { get; }

        /// <summary>
        /// The member routes, in the order they were listed. Always includes the default.
        /// </summary>
        public IReadOnlyList<string> RouteKeys { get; }

        /// <summary>
        /// The outlet the group targets when the caller names none.
        /// </summary>
        [ToString]
        public string Outlet { get; }

        /// <summary>
        /// Opaque data for guards, zones and the application.
        /// </summary>
        public object? Metadata { get; }

        #endregion
    }
}
