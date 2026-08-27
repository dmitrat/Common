using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.MVVM.Navigation.Model;

namespace OutWit.Common.MVVM.Navigation.Interfaces
{
    /// <summary>
    /// Raised with the outlet a navigation targeted (null when the outlet itself was not found) and its result.
    /// </summary>
    public delegate void NavigatedEventHandler(INavigationOutlet? outlet, NavigationResult result);

    /// <summary>
    /// ViewModel-first navigation across named outlets. Every call can be made from any
    /// thread; the pipeline itself runs on the UI thread. Nothing here throws for a
    /// navigation that cannot happen — the <see cref="NavigationResult"/> says why.
    /// </summary>
    public interface INavigationService
    {
        #region Events

        /// <summary>
        /// The content of an outlet changed. Also raised when the change committed but
        /// OnNavigatedTo then failed or was cancelled — the screen is shown either way.
        /// </summary>
        event NavigatedEventHandler? Navigated;

        /// <summary>
        /// A navigation could not be performed: <see cref="NavigationStatus.Failed"/>,
        /// <see cref="NavigationStatus.RouteNotFound"/> or <see cref="NavigationStatus.OutletNotFound"/>.
        /// Rejected and Cancelled are ordinary outcomes and do not raise it.
        /// </summary>
        event NavigatedEventHandler? NavigationFailed;

        #endregion

        #region Outlets

        /// <summary>
        /// Gets an outlet by name.
        /// </summary>
        /// <param name="name">The outlet name; null means <see cref="NavigationOutlets.MAIN"/>.</param>
        /// <returns>The outlet.</returns>
        /// <exception cref="System.InvalidOperationException">No outlet has that name.</exception>
        INavigationOutlet Outlet(string? name = null);

        /// <summary>
        /// Tells whether an outlet with the given name exists.
        /// </summary>
        /// <param name="name">The outlet name.</param>
        /// <returns>True when it exists.</returns>
        bool HasOutlet(string name);

        /// <summary>
        /// Creates an outlet, or returns the existing one with that name. Can be called at
        /// run time — document tabs, a sub-outlet inside a module's screen.
        /// </summary>
        /// <param name="name">The outlet name.</param>
        /// <returns>The outlet.</returns>
        INavigationOutlet AddOutlet(string name);

        #endregion

        #region Navigation

        /// <summary>
        /// Navigates an outlet to a route.
        /// </summary>
        /// <param name="routeKey">The route key.</param>
        /// <param name="parameters">The parameters; null means empty.</param>
        /// <param name="outlet">The outlet name; null means the route's default outlet.</param>
        /// <param name="cancellation">Cancels the navigation before its point of no return, and cuts OnNavigatedTo short after it.</param>
        /// <returns>The result.</returns>
        Task<NavigationResult> NavigateAsync(string routeKey,
                                             NavigationParameters? parameters = null,
                                             string? outlet = null,
                                             CancellationToken cancellation = default);

        /// <summary>
        /// Navigates an outlet to the first route registered for a view model type.
        /// </summary>
        /// <typeparam name="TViewModel">The view model type.</typeparam>
        /// <param name="parameters">The parameters; null means empty.</param>
        /// <param name="outlet">The outlet name; null means the route's default outlet.</param>
        /// <param name="cancellation">Cancels the navigation before its point of no return, and cuts OnNavigatedTo short after it.</param>
        /// <returns>The result.</returns>
        Task<NavigationResult> NavigateAsync<TViewModel>(NavigationParameters? parameters = null,
                                                         string? outlet = null,
                                                         CancellationToken cancellation = default)
            where TViewModel : class;

        /// <summary>
        /// Asks the guards whether a navigation would be allowed, without performing it.
        /// Global guards and the current view model are asked; the target view model is
        /// asked only when a cached instance already exists.
        /// </summary>
        /// <param name="routeKey">The route key.</param>
        /// <param name="parameters">The parameters; null means empty.</param>
        /// <param name="outlet">The outlet name; null means the route's default outlet.</param>
        /// <param name="cancellation">Cancellation token.</param>
        /// <returns>True when nothing refused.</returns>
        Task<bool> CanNavigateAsync(string routeKey,
                                    NavigationParameters? parameters = null,
                                    string? outlet = null,
                                    CancellationToken cancellation = default);

        /// <summary>
        /// Moves an outlet back in its journal. <see cref="NavigationStatus.Rejected"/> when there is nothing to go back to.
        /// </summary>
        /// <param name="outlet">The outlet name; null means <see cref="NavigationOutlets.MAIN"/>.</param>
        /// <param name="cancellation">Cancellation token.</param>
        /// <returns>The result.</returns>
        Task<NavigationResult> GoBackAsync(string? outlet = null, CancellationToken cancellation = default);

        /// <summary>
        /// Moves an outlet forward in its journal. <see cref="NavigationStatus.Rejected"/> when there is nothing to go forward to.
        /// </summary>
        /// <param name="outlet">The outlet name; null means <see cref="NavigationOutlets.MAIN"/>.</param>
        /// <param name="cancellation">Cancellation token.</param>
        /// <returns>The result.</returns>
        Task<NavigationResult> GoForwardAsync(string? outlet = null, CancellationToken cancellation = default);

        /// <summary>
        /// Re-enters the outlet's current route with its current parameters: OnNavigatedFrom/To
        /// again on a Cached view model, a fresh instance for a Transient one.
        /// <see cref="NavigationStatus.Rejected"/> when the outlet is empty.
        /// </summary>
        /// <param name="outlet">The outlet name; null means <see cref="NavigationOutlets.MAIN"/>.</param>
        /// <param name="cancellation">Cancellation token.</param>
        /// <returns>The result.</returns>
        Task<NavigationResult> RefreshAsync(string? outlet = null, CancellationToken cancellation = default);

        /// <summary>
        /// Empties an outlet's journal. Cached view models are kept.
        /// </summary>
        /// <param name="outlet">The outlet name; null means <see cref="NavigationOutlets.MAIN"/>.</param>
        void ClearHistory(string? outlet = null);

        /// <summary>
        /// Drops and disposes the cached view model of a route in an outlet. The outlet's
        /// current view model is never evicted.
        /// </summary>
        /// <param name="routeKey">The route key.</param>
        /// <param name="outlet">The outlet name; null means the route's default outlet.</param>
        /// <returns>True when an instance was evicted.</returns>
        Task<bool> EvictAsync(string routeKey, string? outlet = null);

        #endregion

        #region Groups

        /// <summary>
        /// Forgets where an outlet was in a group, so the next navigation to the group opens
        /// its default. Null narrows nothing: no group means every group, no outlet means
        /// every outlet. Not called by <see cref="ClearHistory"/> — where a section was left
        /// is not history.
        /// </summary>
        /// <param name="groupKey">The group; null means all.</param>
        /// <param name="outlet">The outlet; null means all.</param>
        void ForgetGroup(string? groupKey = null, string? outlet = null);

        /// <summary>
        /// What navigating to the group would open right now: the page remembered for the
        /// outlet, else the group's default. For hints and tests.
        /// </summary>
        /// <param name="groupKey">The group key.</param>
        /// <param name="outlet">The outlet; null means the group's own.</param>
        /// <returns>The page and its parameters; null when the key is not a group, or neither the remembered page nor the default is registered.</returns>
        NavigationEntry? ResolveGroup(string groupKey, string? outlet = null);

        #endregion

        #region Properties

        /// <summary>
        /// All outlets.
        /// </summary>
        IReadOnlyList<INavigationOutlet> Outlets { get; }

        #endregion
    }
}
