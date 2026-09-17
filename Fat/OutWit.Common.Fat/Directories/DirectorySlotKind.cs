namespace OutWit.Common.Fat.Directories
{
    /// <summary>
    /// What a directory slot turned out to be.
    /// </summary>
    internal enum DirectorySlotKind
    {
        /// <summary>
        /// The end marker: this slot and all after it are unused.
        /// </summary>
        End,

        /// <summary>
        /// A deleted entry, a long-name slot, a dot entry, or one that is not valid.
        /// </summary>
        Skipped,

        /// <summary>
        /// A file or directory.
        /// </summary>
        Entry,

        /// <summary>
        /// The volume label.
        /// </summary>
        Label,

        /// <summary>
        /// exFAT's allocation bitmap or up-case table entry, which only the root holds.
        /// </summary>
        Critical
    }
}
