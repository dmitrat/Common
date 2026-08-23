using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.MVVM.Navigation.Model;

namespace OutWit.Common.MVVM.Navigation.Interfaces
{
    /// <summary>
    /// The untyped half of a dialog view model. Use <see cref="IDialogAware{TResult}"/>.
    /// </summary>
    public interface IDialogAware
    {
        #region Functions

        /// <summary>
        /// Called once the dialog is being shown. Load data here.
        /// </summary>
        /// <param name="parameters">The parameters the dialog was opened with. Never null.</param>
        /// <param name="cancellation">Cancellation token.</param>
        Task OnOpenedAsync(NavigationParameters parameters, CancellationToken cancellation);

        #endregion
    }
}
