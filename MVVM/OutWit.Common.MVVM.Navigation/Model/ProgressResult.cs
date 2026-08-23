using System;

namespace OutWit.Common.MVVM.Navigation.Model
{
    /// <summary>
    /// What a run under a progress dialog came to. Like navigation, it does not throw at the
    /// caller: an operation that failed reports <see cref="Error"/>.
    /// </summary>
    /// <typeparam name="TResult">What the operation produces.</typeparam>
    public readonly struct ProgressResult<TResult>
    {
        #region Constructors

        private ProgressResult(bool isCompleted, bool isCancelled, TResult? value, Exception? error)
        {
            IsCompleted = isCompleted;
            IsCancelled = isCancelled;
            Value = value;
            Error = error;
        }

        #endregion

        #region Functions

        /// <summary>
        /// The operation ran to the end.
        /// </summary>
        /// <param name="value">Its result.</param>
        /// <returns>The result.</returns>
        public static ProgressResult<TResult> Completed(TResult value)
        {
            return new ProgressResult<TResult>(true, false, value, null);
        }

        /// <summary>
        /// The user stopped it, or the caller's token did.
        /// </summary>
        /// <returns>The result.</returns>
        public static ProgressResult<TResult> Cancelled()
        {
            return new ProgressResult<TResult>(false, true, default, null);
        }

        /// <summary>
        /// The operation threw.
        /// </summary>
        /// <param name="error">What it threw.</param>
        /// <returns>The result.</returns>
        public static ProgressResult<TResult> Failed(Exception error)
        {
            return new ProgressResult<TResult>(false, false, default, error);
        }

        public override string ToString()
        {
            if (IsCancelled)
                return "Cancelled";

            return IsCompleted ? $"Completed({Value})" : $"Failed({Error?.Message})";
        }

        #endregion

        #region Properties

        /// <summary>
        /// True when the operation ran to the end.
        /// </summary>
        public bool IsCompleted { get; }

        /// <summary>
        /// True when it was stopped before it finished.
        /// </summary>
        public bool IsCancelled { get; }

        /// <summary>
        /// What it produced; default unless <see cref="IsCompleted"/>.
        /// </summary>
        public TResult? Value { get; }

        /// <summary>
        /// What it threw, if it threw.
        /// </summary>
        public Exception? Error { get; }

        #endregion
    }
}
