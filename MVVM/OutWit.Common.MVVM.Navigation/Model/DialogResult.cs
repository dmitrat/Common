namespace OutWit.Common.MVVM.Navigation.Model
{
    /// <summary>
    /// Typed outcome of a dialog. <see cref="IsConfirmed"/> is explicit, so "confirmed
    /// with no value" and "cancelled" are different things even when
    /// <typeparamref name="TResult"/> is a reference type.
    /// </summary>
    /// <typeparam name="TResult">The value type the dialog produces.</typeparam>
    public readonly struct DialogResult<TResult>
    {
        #region Constructors

        private DialogResult(bool isConfirmed, TResult? value)
        {
            IsConfirmed = isConfirmed;
            Value = value;
        }

        #endregion

        #region Functions

        /// <summary>
        /// A confirmed outcome carrying <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result.</returns>
        public static DialogResult<TResult> Confirmed(TResult value)
        {
            return new DialogResult<TResult>(true, value);
        }

        /// <summary>
        /// A cancelled outcome.
        /// </summary>
        /// <returns>The result.</returns>
        public static DialogResult<TResult> Cancelled()
        {
            return new DialogResult<TResult>(false, default);
        }

        public override string ToString()
        {
            return IsConfirmed ? $"Confirmed({Value})" : "Cancelled";
        }

        #endregion

        #region Properties

        /// <summary>
        /// True when the dialog was confirmed.
        /// </summary>
        public bool IsConfirmed { get; }

        /// <summary>
        /// The value; default when cancelled.
        /// </summary>
        public TResult? Value { get; }

        #endregion
    }
}
