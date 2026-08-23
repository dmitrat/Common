namespace OutWit.Common.MVVM.Navigation.Interfaces
{
    /// <summary>
    /// What a long operation writes its progress to. Handed to the work by
    /// <see cref="IProgressDialogService"/>; the dialog shows whatever it reports.
    /// </summary>
    public interface IProgressReporter
    {
        #region Functions

        /// <summary>
        /// Reports what the operation is doing now, and optionally how far along it is.
        /// </summary>
        /// <param name="status">A line for the user; null leaves the previous one.</param>
        /// <param name="progress">Fraction between 0 and 1; null means "no idea yet", which the dialog shows as indeterminate.</param>
        void Report(string? status, double? progress = null);

        /// <summary>
        /// Reports how far along the operation is, leaving the status line alone.
        /// </summary>
        /// <param name="progress">Fraction between 0 and 1.</param>
        void Report(double progress);

        #endregion

        #region Properties

        /// <summary>
        /// The last reported status line.
        /// </summary>
        string? Status { get; }

        /// <summary>
        /// The last reported fraction, or null while the operation is indeterminate.
        /// </summary>
        double? Progress { get; }

        /// <summary>
        /// True once the user has asked to cancel. The operation's own
        /// <see cref="System.Threading.CancellationToken"/> says the same thing; this is here
        /// for code that polls rather than awaits.
        /// </summary>
        bool IsCancellationRequested { get; }

        #endregion
    }
}
