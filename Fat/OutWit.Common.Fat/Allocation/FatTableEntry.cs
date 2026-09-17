namespace OutWit.Common.Fat.Allocation
{
    /// <summary>
    /// One allocation table entry, independent of the table's width.
    /// </summary>
    /// <param name="Kind">What the entry says.</param>
    /// <param name="Value">The raw value, without FAT32's reserved high bits.</param>
    internal readonly record struct FatTableEntry(FatTableEntryKind Kind, uint Value)
    {
        #region Properties

        /// <summary>
        /// The next cluster of the chain; meaningful only for <see cref="FatTableEntryKind.Link"/>.
        /// </summary>
        public uint Next => Value;

        #endregion
    }
}
