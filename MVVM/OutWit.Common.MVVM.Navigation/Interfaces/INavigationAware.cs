using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.MVVM.Navigation.Model;

namespace OutWit.Common.MVVM.Navigation.Interfaces
{
    /// <summary>
    /// Implemented by a view model that wants to know when it is shown and left.
    /// Optional, and deliberately separate from <see cref="INavigationGuard"/>: most
    /// screens need only this.
    /// </summary>
    public interface INavigationAware
    {
        #region Functions

        /// <summary>
        /// Called once the outlet shows this view model. Load data here; honour the token —
        /// a newer navigation or the caller may cut the work short, and the screen stays shown.
        /// </summary>
        /// <param name="context">The navigation that brought the view model in.</param>
        /// <param name="cancellation">Cancellation token.</param>
        Task OnNavigatedToAsync(NavigationContext context, CancellationToken cancellation);

        /// <summary>
        /// Called just before the outlet shows another view model. The context describes
        /// where the outlet is going.
        /// </summary>
        /// <param name="context">The navigation that is leaving this view model.</param>
        /// <param name="cancellation">Cancellation token.</param>
        Task OnNavigatedFromAsync(NavigationContext context, CancellationToken cancellation);

        #endregion
    }
}
