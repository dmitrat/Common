using System;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Abstract;
using OutWit.Common.Aspects;
using OutWit.Common.MVVM.Commands;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Model;

namespace OutWit.Common.MVVM.Navigation.ViewModels
{
    /// <summary>
    /// The view model behind a progress dialog. The platform packages ship a plain view for
    /// it; an application that wants its own registers one for this type and everything else
    /// stays the same.
    /// </summary>
    /// <remarks>
    /// It refuses every close it is asked about until the service says the work is over.
    /// Escape, a click on the backdrop and the Cancel button all mean the same thing — ask
    /// the operation to stop — and the dialog stays up until it actually has.
    /// </remarks>
    public class ProgressDialogViewModel : NotifyPropertyChangedBase, IDialogAware<bool>, IProgressReporter
    {
        #region Events

        public event DialogCloseRequestedEventHandler<bool>? CloseRequested;

        #endregion

        #region Fields

        private readonly CancellationTokenSource m_cancellation;

        private bool m_isFinishing;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the view model. Built by <see cref="IProgressDialogService"/>, not by DI.
        /// </summary>
        /// <param name="options">Title, cancellability, durations.</param>
        /// <param name="cancellation">Cancelled when the user asks the operation to stop.</param>
        public ProgressDialogViewModel(ProgressOptions options, CancellationTokenSource cancellation)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            m_cancellation = cancellation ?? throw new ArgumentNullException(nameof(cancellation));

            Title = options.Title;
            Status = options.Status;
            IsCancellable = options.IsCancellable;

            InitCommands();
        }

        #endregion

        #region Initialization

        private void InitCommands()
        {
            CancelCommand = new RelayCommand(Cancel, () => IsCancellable && !IsCancellationRequested);
        }

        #endregion

        #region Functions

        /// <summary>
        /// Asks the operation to stop. The dialog stays up until it does.
        /// </summary>
        public void Cancel()
        {
            if (!IsCancellable || IsCancellationRequested)
                return;

            IsCancellationRequested = true;
            Status = CancellingStatus ?? Status;

            CancelCommand.RaiseCanExecuteChanged();

            m_cancellation.Cancel();
        }

        /// <summary>
        /// Called by the service when the work is over: the next close request goes through.
        /// </summary>
        internal void Finish()
        {
            m_isFinishing = true;
            CloseRequested?.Invoke(DialogResult<bool>.Confirmed(true));
        }

        #endregion

        #region IProgressReporter

        public void Report(string? status, double? progress = null)
        {
            if (status != null)
                Status = status;

            if (progress.HasValue)
                Progress = Clamp(progress.Value);
        }

        public void Report(double progress)
        {
            Progress = Clamp(progress);
        }

        private static double Clamp(double value)
        {
            return value < 0 ? 0 : value > 1 ? 1 : value;
        }

        #endregion

        #region IDialogAware

        public Task OnOpenedAsync(NavigationParameters parameters, CancellationToken cancellation)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Refuses every close until the work is over. A close the user asked for is read as
        /// a cancellation request instead.
        /// </summary>
        public Task<bool> CanCloseAsync(DialogResult<bool> result, CancellationToken cancellation)
        {
            if (m_isFinishing)
                return Task.FromResult(true);

            Cancel();

            return Task.FromResult(false);
        }

        #endregion

        #region Properties

        [Notify]
        public string Title { get; set; }

        [Notify]
        public string? Status { get; set; }

        /// <summary>
        /// What the status line becomes once the user has asked to stop; null leaves it alone.
        /// </summary>
        [Notify]
        public string? CancellingStatus { get; set; } = "Cancelling…";

        /// <summary>
        /// Fraction between 0 and 1, or null while the operation is indeterminate.
        /// </summary>
        [Notify(NotifyAlso = nameof(IsIndeterminate))]
        public double? Progress { get; set; }

        /// <summary>
        /// True while there is no fraction to show — the view puts its bar in marquee mode.
        /// </summary>
        public bool IsIndeterminate => !Progress.HasValue;

        /// <summary>
        /// Whether the user may ask the operation to stop.
        /// </summary>
        [Notify]
        public bool IsCancellable { get; set; }

        /// <summary>
        /// True once the user has asked. The operation is still running.
        /// </summary>
        [Notify]
        public bool IsCancellationRequested { get; set; }

        #endregion

        #region Commands

        public RelayCommand CancelCommand { get; private set; } = null!;

        #endregion
    }
}
