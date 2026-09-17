namespace OutWit.Common.Fat
{
    /// <summary>
    /// The variant of the FAT family a volume is formatted with.
    /// </summary>
    public enum FatKind
    {
        /// <summary>
        /// 12-bit allocation table: fewer than 4085 clusters, fixed root directory.
        /// </summary>
        Fat12,

        /// <summary>
        /// 16-bit allocation table: 4085 to 65524 clusters, fixed root directory.
        /// </summary>
        Fat16,

        /// <summary>
        /// 32-bit allocation table (28 bits used), root directory in a cluster chain.
        /// </summary>
        Fat32,

        /// <summary>
        /// exFAT: allocation bitmap, entry sets, and files that need no chain when contiguous.
        /// </summary>
        ExFat
    }
}
