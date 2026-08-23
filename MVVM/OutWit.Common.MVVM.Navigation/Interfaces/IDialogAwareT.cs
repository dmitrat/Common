using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.MVVM.Navigation.Model;

namespace OutWit.Common.MVVM.Navigation.Interfaces
{
    /// <summary>
    /// Raised by a dialog view model that wants to close with a result.
    /// </summary>
    /// <typeparam name="TResult">The dialog's result type.</typeparam>
    public delegate void DialogCloseRequestedEventHandler<TResult>(DialogResult<TResult> result);

    /// <summary>
    /// A dialog view model with a typed result.
    /// </summary>
    /// <typeparam name="TResult">The dialog's result type.</typeparam>
    public interface IDialogAware<TResult> : IDialogAware
    {
        #region Events

        /// <summary>
        /// Raised when the view model wants to close. The dialog service asks
        /// <see cref="CanCloseAsync"/> first.
        /// </summary>
        event DialogCloseRequestedEventHandler<TResult>? CloseRequested;

        #endregion

        #region Functions

        /// <summary>
        /// Asked on every attempt to close: the view model's own request, the window's
        /// close button, a light-dismiss of an overlay, <see cref="IDialogService.Close"/>.
        /// Cancellation through the token is not asked — it is authoritative.
        /// </summary>
        /// <param name="result">The result the dialog would close with.</param>
        /// <param name="cancellation">Cancellation token.</param>
        /// <returns>False to keep the dialog open.</returns>
        Task<bool> CanCloseAsync(DialogResult<TResult> result, CancellationToken cancellation);

        #endregion
    }
}
