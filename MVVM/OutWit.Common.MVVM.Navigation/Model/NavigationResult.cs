using System;
using OutWit.Common.Abstract;
using OutWit.Common.Attributes;
using OutWit.Common.Values;

namespace OutWit.Common.MVVM.Navigation.Model
{
    /// <summary>
    /// What a navigation request came to. Navigation never throws at the caller: a
    /// failure is a <see cref="NavigationStatus.Failed"/> result with the exception in
    /// <see cref="Error"/>.
    /// </summary>
    public sealed class NavigationResult : ModelBase
    {
        #region Constructors

        /// <summary>
        /// Creates a result.
        /// </summary>
        /// <param name="status">The outcome.</param>
        /// <param name="routeKey">The requested route key.</param>
        /// <param name="outlet">The outlet the request targeted; empty when it could not be resolved.</param>
        /// <param name="error">The exception behind a <see cref="NavigationStatus.Failed"/> outcome.</param>
        public NavigationResult(NavigationStatus status, string routeKey, string outlet, Exception? error = null)
        {
            Status = status;
            RouteKey = routeKey ?? string.Empty;
            Outlet = outlet ?? string.Empty;
            Error = error;
        }

        #endregion

        #region ModelBase

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not NavigationResult other)
                return false;

            return Status.Is(other.Status)
                   && RouteKey.Is(other.RouteKey)
                   && Outlet.Is(other.Outlet)
                   && ReferenceEquals(Error, other.Error);
        }

        public override ModelBase Clone()
        {
            return new NavigationResult(Status, RouteKey, Outlet, Error);
        }

        #endregion

        #region Properties

        /// <summary>
        /// The outcome.
        /// </summary>
        [ToString]
        public NavigationStatus Status { get; }

        /// <summary>
        /// The requested route key.
        /// </summary>
        [ToString]
        public string RouteKey { get; }

        /// <summary>
        /// The outlet the request targeted.
        /// </summary>
        [ToString]
        public string Outlet { get; }

        /// <summary>
        /// The exception behind a <see cref="NavigationStatus.Failed"/> outcome.
        /// </summary>
        public Exception? Error { get; }

        /// <summary>
        /// True for <see cref="NavigationStatus.Success"/> and <see cref="NavigationStatus.Unchanged"/>.
        /// </summary>
        public bool IsSuccess => Status is NavigationStatus.Success or NavigationStatus.Unchanged;

        #endregion
    }
}
