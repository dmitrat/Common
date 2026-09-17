namespace OutWit.Common.Fat.Files
{
    /// <summary>
    /// Where a file's data lies and how much of it there is, as its entry records it.
    /// </summary>
    /// <param name="FirstCluster">The first cluster, or zero when the file has none.</param>
    /// <param name="Length">The length in bytes; a directory's size on exFAT.</param>
    /// <param name="ValidLength">How much of it was written; exFAT only, the length elsewhere.</param>
    /// <param name="IsContiguous">Whether the clusters follow each other without a table chain; exFAT only.</param>
    internal readonly record struct FatFileState(uint FirstCluster, long Length, long ValidLength, bool IsContiguous);
}
