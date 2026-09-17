namespace OutWit.Common.Fat.Allocation
{
    /// <summary>
    /// What an allocation table entry says about its cluster.
    /// </summary>
    internal enum FatTableEntryKind
    {
        /// <summary>
        /// The cluster is free.
        /// </summary>
        Free,

        /// <summary>
        /// The chain continues at <see cref="FatTableEntry.Next"/>.
        /// </summary>
        Link,

        /// <summary>
        /// The chain ends here.
        /// </summary>
        End,

        /// <summary>
        /// The cluster is marked bad.
        /// </summary>
        Bad,

        /// <summary>
        /// A reserved value, or a cluster number outside the volume.
        /// </summary>
        Invalid
    }
}
