using System;
using System.Threading;
using System.Threading.Tasks;

namespace OutWit.Common.MVVM.Navigation.Interfaces
{
    /// <summary>
    /// The platform side of dialogs. The core does not know whether a host is a
    /// window, an overlay layer or DialogHost.Avalonia; it only needs to show a view,
    /// learn when it closed, and close it.
    /// </summary>
    public interface IDialogHost
    {
        #region Functions

        /// <summary>
        /// Tells whether the named host currently shows a dialog.
        /// </summary>
        /// <param name="host">The host name.</param>
        /// <returns>True when a dialog is open.</returns>
        bool IsOpen(string host);

        /// <summary>
        /// Shows a view on the named host and completes when it has closed. Any close the
        /// UI initiates — the window's close button, a click on the overlay backdrop —
        /// must go through <paramref name="canDismiss"/> and be abandoned when it returns false.
        /// Cancellation through the token closes without asking.
        /// </summary>
        /// <param name="host">The host name.</param>
        /// <param name="view">The view to show.</param>
        /// <param name="canDismiss">Asks the dialog whether a UI-initiated close may go ahead.</param>
        /// <param name="cancellation">Closes the dialog.</param>
        Task ShowAsync(string host, object view, Func<Task<bool>> canDismiss, CancellationToken cancellation);

        /// <summary>
        /// Closes the topmost dialog of the named host without asking — the questions
        /// have been asked by the dialog service already. No-op when nothing is open.
        /// </summary>
        /// <param name="host">The host name.</param>
        void Close(string host);

        #endregion

        #region Properties

        /// <summary>
        /// Whether a second dialog may be shown on a host while the first is open.
        /// True for windows, false for an overlay layer.
        /// </summary>
        bool SupportsNesting { get; }

        #endregion
    }
}
