using System;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.MVVM.Navigation.Model;

namespace OutWit.Common.MVVM.Navigation.Interfaces
{
    /// <summary>
    /// Runs a long operation behind a modal progress dialog. The dialog appears only if the
    /// operation outlasts <see cref="ProgressOptions.Delay"/> and, once up, stays at least
    /// <see cref="ProgressOptions.MinimumDuration"/> — so a fast run shows nothing and a
    /// borderline one does not flash.
    /// </summary>
    /// <remarks>
    /// The work runs on the calling context: this is a UI service, not a scheduler. An
    /// operation that would block the UI thread should do its own <c>Task.Run</c> — the dialog
    /// cannot repaint a thread that is busy.
    /// </remarks>
    public interface IProgressDialogService
    {
        #region Functions

        /// <summary>
        /// Runs an operation that produces a value.
        /// </summary>
        /// <typeparam name="TResult">What the operation produces.</typeparam>
        /// <param name="work">The operation. It gets a reporter to write progress to and a token that trips when the user cancels.</param>
        /// <param name="options">Title, cancellability and the two durations; null means the defaults.</param>
        /// <param name="host">The dialog host; null means <see cref="DialogHosts.ROOT"/>.</param>
        /// <param name="cancellation">Cancels the operation from the caller's side.</param>
        /// <returns>What the run came to.</returns>
        Task<ProgressResult<TResult>> RunAsync<TResult>(Func<IProgressReporter, CancellationToken, Task<TResult>> work,
                                                        ProgressOptions? options = null,
                                                        string? host = null,
                                                        CancellationToken cancellation = default);

        /// <summary>
        /// Runs an operation that produces nothing.
        /// </summary>
        /// <param name="work">The operation.</param>
        /// <param name="options">Title, cancellability and the two durations; null means the defaults.</param>
        /// <param name="host">The dialog host; null means <see cref="DialogHosts.ROOT"/>.</param>
        /// <param name="cancellation">Cancels the operation from the caller's side.</param>
        /// <returns>What the run came to.</returns>
        Task<ProgressResult<bool>> RunAsync(Func<IProgressReporter, CancellationToken, Task> work,
                                            ProgressOptions? options = null,
                                            string? host = null,
                                            CancellationToken cancellation = default);

        #endregion
    }
}
