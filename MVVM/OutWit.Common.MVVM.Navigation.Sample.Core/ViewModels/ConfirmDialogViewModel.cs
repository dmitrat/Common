using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Abstract;
using OutWit.Common.Aspects;
using OutWit.Common.MVVM.Commands;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Model;

namespace OutWit.Common.MVVM.Navigation.Sample.Core.ViewModels
{
    /// <summary>
    /// A yes/no dialog with a typed result. Nothing about it knows whether it will end up in
    /// a window or an overlay — that is the host's business.
    /// </summary>
    /// <remarks>
    /// It derives from <see cref="NotifyPropertyChangedBase"/>, and it has to: the view is
    /// built before <see cref="OnOpenedAsync"/> runs, so the title and the message reach the
    /// screen as change notifications. A dialog view model that carries [Notify] properties
    /// without implementing INotifyPropertyChanged binds once, to its defaults, and never
    /// updates — silently. ValidateNavigation reports that for routes; for dialogs, this is
    /// the rule to remember.
    /// </remarks>
    public class ConfirmDialogViewModel : NotifyPropertyChangedBase, IDialogAware<bool>
    {
        #region Events

        public event DialogCloseRequestedEventHandler<bool>? CloseRequested;

        #endregion

        #region Constructors

        public ConfirmDialogViewModel()
        {
            InitCommands();
        }

        #endregion

        #region Initialization

        private void InitCommands()
        {
            ConfirmCommand = new RelayCommand(() => CloseRequested?.Invoke(DialogResult<bool>.Confirmed(true)));
            CancelCommand = new RelayCommand(() => CloseRequested?.Invoke(DialogResult<bool>.Cancelled()));
        }

        #endregion

        #region IDialogAware

        public Task OnOpenedAsync(NavigationParameters parameters, CancellationToken cancellation)
        {
            Title = parameters.Get("title", "Confirm");
            Message = parameters.Get("message", string.Empty);

            return Task.CompletedTask;
        }

        public Task<bool> CanCloseAsync(DialogResult<bool> result, CancellationToken cancellation)
        {
            return Task.FromResult(true);
        }

        #endregion

        #region Properties

        [Notify]
        public string Title { get; set; } = "Confirm";

        [Notify]
        public string Message { get; set; } = string.Empty;

        #endregion

        #region Commands

        public RelayCommand ConfirmCommand { get; private set; } = null!;

        public RelayCommand CancelCommand { get; private set; } = null!;

        #endregion
    }
}
