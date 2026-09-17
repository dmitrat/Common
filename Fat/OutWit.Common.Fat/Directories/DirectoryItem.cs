using OutWit.Common.Fat.Model;

namespace OutWit.Common.Fat.Directories
{
    /// <summary>
    /// An entry together with where it lies in its directory, and where its data lies.
    /// </summary>
    internal sealed class DirectoryItem
    {
        #region Constructors

        /// <param name="entry">The entry.</param>
        /// <param name="directoryCluster">The first cluster of the directory holding it; zero for the fixed root.</param>
        /// <param name="firstSlot">The slot it starts at: its first long-name slot or its short entry; on exFAT, its file entry.</param>
        /// <param name="lastSlot">The slot it ends at: its short entry; on exFAT, its last secondary entry.</param>
        public DirectoryItem(FatDirectoryEntry entry, uint directoryCluster, long firstSlot, long lastSlot)
        {
            Entry = entry;
            DirectoryCluster = directoryCluster;
            FirstSlot = firstSlot;
            LastSlot = lastSlot;
            DataLength = entry.Length;
            ValidLength = entry.Length;
        }

        #endregion

        #region Properties

        public FatDirectoryEntry Entry { get; }

        public uint DirectoryCluster { get; }

        public long FirstSlot { get; }

        public long LastSlot { get; }

        /// <summary>
        /// Whether the entry's clusters follow each other and the table does not describe
        /// them — exFAT's <c>NoFatChain</c>. Their number then comes from <see cref="DataLength"/>.
        /// </summary>
        public bool IsContiguous { get; init; }

        /// <summary>
        /// The length the entry records: a file's length; on exFAT, a directory's size too.
        /// Zero for a directory on FAT12/16/32, whose size only its chain tells.
        /// </summary>
        public long DataLength { get; init; }

        /// <summary>
        /// How much of the data has been written; what lies past it reads as zeros. Only
        /// exFAT records it apart from the length.
        /// </summary>
        public long ValidLength { get; init; }

        /// <summary>
        /// The directory the entry lies in, when it was found by listing that directory; on
        /// exFAT a directory that grows records its new size there.
        /// </summary>
        public DirectoryItem? Parent { get; init; }

        /// <summary>
        /// The name hash the entry records; exFAT only.
        /// </summary>
        public ushort NameHash { get; init; }

        /// <summary>
        /// The name as stored, when it holds characters the entry's name had to replace;
        /// exFAT only.
        /// </summary>
        public string? StoredName { get; init; }

        /// <summary>
        /// Whether this is the root, which lies in no directory.
        /// </summary>
        public bool IsRoot => LastSlot < 0;

        /// <summary>
        /// What identifies the entry while the volume is mounted.
        /// </summary>
        public (uint Directory, long Slot) Key => (DirectoryCluster, LastSlot);

        #endregion
    }
}
