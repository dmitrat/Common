namespace OutWit.Common.Fat
{
    /// <summary>
    /// What <see cref="FatChecker"/> found wrong with a volume.
    /// </summary>
    public enum FatProblemKind
    {
        /// <summary>
        /// The backup boot sector or boot region differs from the main one, or exFAT's
        /// backup region fails its checksum.
        /// </summary>
        BootRegion,

        /// <summary>
        /// The boot sector says the volume was not closed cleanly: exFAT's volume flags, or
        /// the state byte Linux sets on FAT while a volume is mounted.
        /// </summary>
        VolumeDirty,

        /// <summary>
        /// The copies of a mirrored allocation table differ.
        /// </summary>
        TablesDiffer,

        /// <summary>
        /// FAT32's FSInfo counts a different number of free clusters than the table holds.
        /// </summary>
        FreeCount,

        /// <summary>
        /// exFAT's boot sector gives a percentage in use above 100 that is not the mark for
        /// unknown. A percentage that is merely stale is not reported: Linux never updates it.
        /// </summary>
        PercentInUse,

        /// <summary>
        /// A chain runs into a free, bad or reserved entry, leaves the volume, or loops.
        /// </summary>
        BrokenChain,

        /// <summary>
        /// A cluster belongs to more than one file or directory.
        /// </summary>
        CrossLinked,

        /// <summary>
        /// An entry's clusters are not as many as its length needs.
        /// </summary>
        LengthMismatch,

        /// <summary>
        /// Clusters are in use but belong to nothing.
        /// </summary>
        LostClusters,

        /// <summary>
        /// exFAT's bitmap marks free a cluster something holds.
        /// </summary>
        BitmapMismatch,

        /// <summary>
        /// A directory entry is broken: a long name that belongs to no short entry, an exFAT
        /// entry set that is cut short or does not match its checksum, or an entry that
        /// cannot be read.
        /// </summary>
        BrokenEntry,

        /// <summary>
        /// An exFAT entry's name hash is not the hash of its name.
        /// </summary>
        WrongNameHash,

        /// <summary>
        /// A directory is one of its own ancestors.
        /// </summary>
        DirectoryLoop,

        /// <summary>
        /// A FAT12/16/32 directory's <c>.</c> or <c>..</c> entry is missing or points
        /// elsewhere.
        /// </summary>
        DotEntries,

        /// <summary>
        /// exFAT's up-case table or allocation bitmap is missing or broken.
        /// </summary>
        Metadata,

        /// <summary>
        /// A directory lies under a path longer than any system follows, 32,767 characters;
        /// it is not entered, and what it holds counts as lost.
        /// </summary>
        TooDeep
    }
}
