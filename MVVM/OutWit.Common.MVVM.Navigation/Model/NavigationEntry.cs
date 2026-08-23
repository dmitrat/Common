using System;
using OutWit.Common.Abstract;
using OutWit.Common.Attributes;
using OutWit.Common.Values;

namespace OutWit.Common.MVVM.Navigation.Model
{
    /// <summary>
    /// One journal entry: a route and its parameters. The journal stores entries,
    /// not view model instances — going back to a Transient route builds a fresh
    /// view model with the old parameters.
    /// </summary>
    public sealed class NavigationEntry : ModelBase
    {
        #region Constructors

        /// <summary>
        /// Creates an entry stamped with the current UTC time.
        /// </summary>
        /// <param name="routeKey">The route key.</param>
        /// <param name="parameters">The parameters; null means empty.</param>
        public NavigationEntry(string routeKey, NavigationParameters? parameters)
            : this(routeKey, parameters, DateTime.UtcNow)
        {
        }

        /// <summary>
        /// Creates an entry with an explicit timestamp.
        /// </summary>
        /// <param name="routeKey">The route key.</param>
        /// <param name="parameters">The parameters; null means empty.</param>
        /// <param name="timestampUtc">When the entry was written.</param>
        public NavigationEntry(string routeKey, NavigationParameters? parameters, DateTime timestampUtc)
        {
            if (string.IsNullOrEmpty(routeKey))
                throw new ArgumentException("Route key must be a non-empty string.", nameof(routeKey));

            RouteKey = routeKey;
            Parameters = parameters ?? NavigationParameters.EMPTY;
            TimestampUtc = timestampUtc;
        }

        #endregion

        #region ModelBase

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not NavigationEntry other)
                return false;

            return RouteKey.Is(other.RouteKey)
                   && Parameters.Is(other.Parameters, tolerance)
                   && TimestampUtc == other.TimestampUtc;
        }

        public override ModelBase Clone()
        {
            return new NavigationEntry(RouteKey, Parameters, TimestampUtc);
        }

        #endregion

        #region Properties

        /// <summary>
        /// The route key.
        /// </summary>
        [ToString]
        public string RouteKey { get; }

        /// <summary>
        /// The parameters. Never null.
        /// </summary>
        [ToString]
        public NavigationParameters Parameters { get; }

        /// <summary>
        /// When the entry was written.
        /// </summary>
        [ToString(Format = "O")]
        public DateTime TimestampUtc { get; }

        #endregion
    }
}
