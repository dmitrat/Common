using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Model;

namespace OutWit.Common.MVVM.Navigation.Services
{
    /// <summary>
    /// One open dialog with a typed result. Every close attempt — the view model's own
    /// request, the host's dismiss, the service's Close — goes through
    /// <see cref="IDialogAware{TResult}.CanCloseAsync"/> here, one at a time.
    /// </summary>
    internal sealed class DialogSession<TResult> : DialogSession
    {
        #region Fields

        private readonly IDialogAware<TResult> m_viewModel;
        private readonly IDialogHost m_host;
        private readonly CancellationToken m_cancellation;
        private readonly ILogger? m_logger;

        private bool m_closing;

        #endregion

        #region Constructors

        public DialogSession(string host, IDialogAware<TResult> viewModel, IDialogHost dialogHost, CancellationToken cancellation, ILogger? logger)
            : base(host)
        {
            m_viewModel = viewModel;
            m_host = dialogHost;
            m_cancellation = cancellation;
            m_logger = logger;

            Result = DialogResult<TResult>.Cancelled();
        }

        #endregion

        #region Functions

        public void Attach()
        {
            m_viewModel.CloseRequested += OnCloseRequested;
        }

        public void Detach()
        {
            m_viewModel.CloseRequested -= OnCloseRequested;
        }

        /// <summary>
        /// Asks the view model and, when allowed, records the result and closes the host.
        /// </summary>
        public async Task TryCloseAsync(DialogResult<TResult> result)
        {
            if (m_closing)
                return;

            m_closing = true;

            try
            {
                if (!await m_viewModel.CanCloseAsync(result, m_cancellation))
                    return;

                Result = result;
                m_host.Close(Host);
            }
            catch (Exception e)
            {
                m_logger?.LogError(e, "Closing dialog {ViewModel} failed", m_viewModel.GetType().FullName);
            }
            finally
            {
                m_closing = false;
            }
        }

        /// <summary>
        /// The host's canDismiss: asks the view model whether a UI-initiated close may go
        /// ahead. When it may, the result is Cancelled and the host does the closing.
        /// </summary>
        public async Task<bool> CanDismissAsync()
        {
            if (m_closing)
                return false;

            m_closing = true;

            try
            {
                var cancelled = DialogResult<TResult>.Cancelled();

                if (!await m_viewModel.CanCloseAsync(cancelled, m_cancellation))
                    return false;

                Result = cancelled;
                return true;
            }
            catch (Exception e)
            {
                m_logger?.LogError(e, "Dismissing dialog {ViewModel} failed", m_viewModel.GetType().FullName);
                return false;
            }
            finally
            {
                m_closing = false;
            }
        }

        public override Task RequestCancelAsync()
        {
            return TryCloseAsync(DialogResult<TResult>.Cancelled());
        }

        #endregion

        #region Event Handlers

        private async void OnCloseRequested(DialogResult<TResult> result)
        {
            try
            {
                await TryCloseAsync(result);
            }
            catch (Exception e)
            {
                m_logger?.LogError(e, "Close request of dialog {ViewModel} failed", m_viewModel.GetType().FullName);
            }
        }

        #endregion

        #region Properties

        public DialogResult<TResult> Result { get; private set; }

        #endregion
    }
}
