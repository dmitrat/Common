namespace OutWit.Common.MVVM.Navigation.Model
{
    /// <summary>
    /// Outcome of a navigation request.
    /// </summary>
    public enum NavigationStatus
    {
        /// <summary>
        /// The outlet now shows the requested route.
        /// </summary>
        Success = 0,

        /// <summary>
        /// The outlet already showed the requested route with equal parameters
        /// (Cached mode, new navigation). Nothing was done.
        /// </summary>
        Unchanged,

        /// <summary>
        /// A guard refused: unsaved changes, a licence feature, a navigation lock,
        /// or the target view model declined to be shown. Also returned by
        /// GoBack/GoForward/Refresh when there is nothing to go to.
        /// </summary>
        Rejected,

        /// <summary>
        /// Cancelled through the caller's token or displaced by a newer navigation
        /// into the same outlet before the point of no return. If the cancellation
        /// arrived after the point of no return, the screen is shown and only its
        /// OnNavigatedTo work was cut short.
        /// </summary>
        Cancelled,

        /// <summary>
        /// No route is registered under the requested key.
        /// </summary>
        RouteNotFound,

        /// <summary>
        /// No outlet is registered under the requested name.
        /// </summary>
        OutletNotFound,

        /// <summary>
        /// An exception escaped view model creation, or OnNavigatedFrom/OnNavigatedTo
        /// after the point of no return. See <see cref="NavigationResult.Error"/>.
        /// </summary>
        Failed
    }
}
