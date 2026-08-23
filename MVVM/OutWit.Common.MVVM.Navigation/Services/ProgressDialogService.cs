using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Model;
using OutWit.Common.MVVM.Navigation.ViewModels;

namespace OutWit.Common.MVVM.Navigation.Services
{
    /// <summary>
    /// Default <see cref="IProgressDialogService"/>, built on <see cref="IDialogService"/>:
    /// the dialog is an ordinary dialog, and what this class adds is the timing.
    /// </summary>
    public sealed class ProgressDialogService : IProgressDialogService
    {
        #region Fields

        private readonly IDialogService m_dialogs;
        private readonly ILogger<ProgressDialogService>? m_logger;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the service. Resolved from DI by <c>services.AddNavigation()</c>.
        /// </summary>
        /// <param name="dialogs">The dialog service the progress dialog is shown through.</param>
        /// <param name="logger">Optional logger.</param>
        public ProgressDialogService(IDialogService dialogs, ILogger<ProgressDialogService>? logger = null)
        {
            m_dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
            m_logger = logger;
        }

        #endregion

        #region IProgressDialogService

        public async Task<ProgressResult<TResult>> RunAsync<TResult>(Func<IProgressReporter, CancellationToken, Task<TResult>> work,
                                                                     ProgressOptions? options = null,
                                                                     string? host = null,
                                                                     CancellationToken cancellation = default)
        {
            if (work == null)
                throw new ArgumentNullException(nameof(work));

            options ??= new ProgressOptions();

            using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            var viewModel = new ProgressDialogViewModel(options, cancellationSource);

            var running = Start(work, viewModel, cancellationSource.Token);

            // an operation that beats the delay never shows anything
            if (options.Delay > TimeSpan.Zero)
            {
                var raced = await Task.WhenAny(running, Task.Delay(options.Delay, cancellationSource.Token).ContinueWith(NoOp, TaskScheduler.Default));

                if (ReferenceEquals(raced, running))
                    return await running;
            }

            if (running.IsCompleted)
                return await running;

            var shown = Stopwatch.StartNew();
            var showing = m_dialogs.ShowAsync(viewModel, host, CancellationToken.None);

            try
            {
                var result = await running;

                var remaining = options.MinimumDuration - shown.Elapsed;
                if (remaining > TimeSpan.Zero)
                    await Task.Delay(remaining);

                return result;
            }
            finally
            {
                viewModel.Finish();

                try
                {
                    await showing;
                }
                catch (Exception e)
                {
                    m_logger?.LogError(e, "Progress dialog '{Title}' failed to close cleanly", options.Title);
                }
            }
        }

        public async Task<ProgressResult<bool>> RunAsync(Func<IProgressReporter, CancellationToken, Task> work,
                                                         ProgressOptions? options = null,
                                                         string? host = null,
                                                         CancellationToken cancellation = default)
        {
            if (work == null)
                throw new ArgumentNullException(nameof(work));

            return await RunAsync(async (reporter, token) =>
            {
                await work(reporter, token);
                return true;
            }, options, host, cancellation);
        }

        #endregion

        #region Functions

        /// <summary>
        /// Runs the work and turns whatever happens into a result — the caller of a progress
        /// dialog should not have to wrap it in a try.
        /// </summary>
        private async Task<ProgressResult<TResult>> Start<TResult>(Func<IProgressReporter, CancellationToken, Task<TResult>> work,
                                                                   ProgressDialogViewModel viewModel,
                                                                   CancellationToken cancellation)
        {
            try
            {
                var value = await work(viewModel, cancellation);

                return cancellation.IsCancellationRequested
                    ? ProgressResult<TResult>.Cancelled()
                    : ProgressResult<TResult>.Completed(value);
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                return ProgressResult<TResult>.Cancelled();
            }
            catch (Exception e)
            {
                m_logger?.LogError(e, "Operation behind progress dialog '{Title}' failed", viewModel.Title);

                return ProgressResult<TResult>.Failed(e);
            }
        }

        private static void NoOp(Task task)
        {
            _ = task.Exception;
        }

        #endregion
    }
}
