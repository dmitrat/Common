using System;
using OutWit.Common.Abstract;
using OutWit.Common.Attributes;
using OutWit.Common.Values;

namespace OutWit.Common.MVVM.Navigation.Model
{
    /// <summary>
    /// A navigation route: a key mapped to a view model type, with the creation mode,
    /// the default outlet and whatever metadata the registering module wants to attach
    /// (an icon, a required licence feature, a role — the package does not interpret it).
    /// </summary>
    public sealed class NavigationRoute : ModelBase
    {
        #region Constructors

        /// <summary>
        /// Creates a route.
        /// </summary>
        /// <param name="key">The route key, unique within the registry.</param>
        /// <param name="viewModelType">The view model type.</param>
        /// <param name="mode">How the view model is created and kept.</param>
        /// <param name="outlet">The outlet the route targets when the caller names none.</param>
        /// <param name="metadata">Opaque data for guards, zones and the application.</param>
        /// <exception cref="ArgumentException">The key or outlet is empty.</exception>
        /// <exception cref="ArgumentNullException">The view model type is null.</exception>
        public NavigationRoute(string key,
                               Type viewModelType,
                               NavigationRouteMode mode = NavigationRouteMode.Cached,
                               string outlet = NavigationOutlets.MAIN,
                               object? metadata = null)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Route key must be a non-empty string.", nameof(key));

            if (string.IsNullOrEmpty(outlet))
                throw new ArgumentException("Outlet name must be a non-empty string.", nameof(outlet));

            Key = key;
            ViewModelType = viewModelType ?? throw new ArgumentNullException(nameof(viewModelType));
            Mode = mode;
            Outlet = outlet;
            Metadata = metadata;
        }

        #endregion

        #region ModelBase

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not NavigationRoute other)
                return false;

            return Key.Is(other.Key)
                   && ViewModelType.Is(other.ViewModelType)
                   && Mode.Is(other.Mode)
                   && Outlet.Is(other.Outlet)
                   && Equals(Metadata, other.Metadata);
        }

        public override ModelBase Clone()
        {
            return new NavigationRoute(Key, ViewModelType, Mode, Outlet, Metadata);
        }

        #endregion

        #region Properties

        /// <summary>
        /// The route key.
        /// </summary>
        [ToString]
        public string Key { get; }

        /// <summary>
        /// The view model type.
        /// </summary>
        [ToString]
        public Type ViewModelType { get; }

        /// <summary>
        /// How the view model is created and kept.
        /// </summary>
        [ToString]
        public NavigationRouteMode Mode { get; }

        /// <summary>
        /// The outlet the route targets when the caller names none.
        /// </summary>
        [ToString]
        public string Outlet { get; }

        /// <summary>
        /// Opaque data attached by the registering module.
        /// </summary>
        public object? Metadata { get; }

        #endregion
    }
}
