using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.MVVM.Navigation.Model;

namespace OutWit.Common.MVVM.Navigation.Interfaces
{
    /// <summary>
    /// Shows modal view models and returns their typed result. Dialogs are a separate
    /// axis: they do not occupy an outlet and do not enter any journal.
    /// </summary>
    public interface IDialogService
    {
        #region Functions

        /// <summary>
        /// Shows a view model and waits for it to close. The view is built by the
        /// platform's <see cref="IViewFactory"/>.
        /// </summary>
        /// <typeparam name="TResult">The dialog's result type.</typeparam>
        /// <param name="viewModel">The view model.</param>
        /// <param name="host">The host name; null means <see cref="DialogHosts.ROOT"/>.</param>
        /// <param name="cancellation">Closes the dialog as cancelled.</param>
        /// <returns>The result; cancelled when the host was busy and does not nest, or the view could not be built.</returns>
        Task<DialogResult<TResult>> ShowAsync<TResult>(IDialogAware<TResult> viewModel,
                                                       string? host = null,
                                                       CancellationToken cancellation = default);

        /// <summary>
        /// Creates a view model in its own DI scope, shows it and waits for it to close.
        /// The view model and the scope are disposed afterwards.
        /// </summary>
        /// <typeparam name="TViewModel">The view model type.</typeparam>
        /// <typeparam name="TResult">The dialog's result type.</typeparam>
        /// <param name="parameters">Handed to <see cref="IDialogAware.OnOpenedAsync"/>; null means empty.</param>
        /// <param name="host">The host name; null means <see cref="DialogHosts.ROOT"/>.</param>
        /// <param name="cancellation">Closes the dialog as cancelled.</param>
        /// <returns>The result; cancelled when the host was busy and does not nest, or the view could not be built.</returns>
        Task<DialogResult<TResult>> ShowAsync<TViewModel, TResult>(NavigationParameters? parameters = null,
                                                                   string? host = null,
                                                                   CancellationToken cancellation = default)
            where TViewModel : class, IDialogAware<TResult>;

        /// <summary>
        /// Tells whether a host currently shows a dialog.
        /// </summary>
        /// <param name="host">The host name; null means <see cref="DialogHosts.ROOT"/>.</param>
        /// <returns>True when a dialog is open.</returns>
        bool IsOpen(string? host = null);

        /// <summary>
        /// Asks the topmost dialog of a host to close as cancelled. The dialog's
        /// <see cref="IDialogAware{TResult}.CanCloseAsync"/> may refuse.
        /// </summary>
        /// <param name="host">The host name; null means <see cref="DialogHosts.ROOT"/>.</param>
        void Close(string? host = null);

        #endregion
    }
}
