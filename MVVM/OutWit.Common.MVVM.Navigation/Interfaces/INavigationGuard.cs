using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.MVVM.Navigation.Model;

namespace OutWit.Common.MVVM.Navigation.Interfaces
{
    /// <summary>
    /// The right to refuse a navigation. Implemented by a view model, which is asked
    /// about its own arrival and departure, or by an application service registered
    /// with <c>nav.AddGuard&lt;T&gt;()</c>, which is asked about every navigation in every
    /// outlet — a navigation lock, a role check, a licence feature read from
    /// <see cref="NavigationRoute.Metadata"/>.
    /// </summary>
    public interface INavigationGuard
    {
        #region Functions

        /// <summary>
        /// Asked before the target is shown. For a global guard this happens before the
        /// target view model is even created.
        /// </summary>
        /// <param name="context">The navigation being attempted.</param>
        /// <param name="cancellation">Cancellation token.</param>
        /// <returns>False to refuse.</returns>
        Task<bool> CanNavigateToAsync(NavigationContext context, CancellationToken cancellation);

        /// <summary>
        /// Asked before the current view model is left: unsaved edits, a running
        /// computation, "navigation is locked". The context describes where the outlet
        /// is going.
        /// </summary>
        /// <param name="context">The navigation being attempted.</param>
        /// <param name="cancellation">Cancellation token.</param>
        /// <returns>False to refuse.</returns>
        Task<bool> CanNavigateFromAsync(NavigationContext context, CancellationToken cancellation);

        #endregion
    }
}
