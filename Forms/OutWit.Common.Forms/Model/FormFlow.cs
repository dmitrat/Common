namespace OutWit.Common.Forms.Model
{
    /// <summary>
    /// Which way the things inside are filled in when there is more than one column.
    /// </summary>
    public enum FormFlow : byte
    {
        /// <summary>
        /// Across, then down: the first thing goes in the first column, the second beside it, and
        /// what does not fit starts the next row. The reading order of a page.
        /// </summary>
        Rows = 0,

        /// <summary>
        /// Down, then across: the first column is filled to the bottom before the second is begun.
        /// </summary>
        /// <remarks>
        /// What a form of several short sections wants. Filled across, a short section beside a
        /// tall one leaves the row half empty and the page reads as a staircase; filled down, the
        /// columns come out near the same length and each column is a list of related things.
        /// </remarks>
        Columns = 1
    }
}
