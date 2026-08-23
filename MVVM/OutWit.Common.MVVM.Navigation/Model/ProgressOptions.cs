using System;

namespace OutWit.Common.MVVM.Navigation.Model
{
    /// <summary>
    /// How a progress dialog behaves. The two durations are the reason this contract exists
    /// separately from <see cref="Interfaces.IDialogService"/>: a dialog that appears for
    /// eighty milliseconds and vanishes reads as a glitch, and one that appears for every
    /// operation regardless of length is noise.
    /// </summary>
    public sealed class ProgressOptions
    {
        #region Constants

        /// <summary>
        /// How long an operation may run before the dialog appears at all.
        /// </summary>
        public static readonly TimeSpan DEFAULT_DELAY = TimeSpan.FromMilliseconds(400);

        /// <summary>
        /// How long the dialog stays up once it has appeared.
        /// </summary>
        public static readonly TimeSpan DEFAULT_MINIMUM_DURATION = TimeSpan.FromMilliseconds(600);

        #endregion

        #region Properties

        /// <summary>
        /// The dialog's title, already localized.
        /// </summary>
        public string Title { get; set; } = "Working…";

        /// <summary>
        /// The first status line, before the operation reports one of its own.
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Whether the user may ask the operation to stop. When false the dialog has no cancel
        /// button and ignores Escape.
        /// </summary>
        public bool IsCancellable { get; set; } = true;

        /// <summary>
        /// An operation that finishes within this never shows a dialog at all.
        /// </summary>
        public TimeSpan Delay { get; set; } = DEFAULT_DELAY;

        /// <summary>
        /// Once shown, the dialog stays at least this long, even if the work finishes sooner.
        /// </summary>
        public TimeSpan MinimumDuration { get; set; } = DEFAULT_MINIMUM_DURATION;

        #endregion
    }
}
