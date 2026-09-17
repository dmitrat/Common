namespace OutWit.Common.Fat.Tests.Utils
{
    /// <summary>
    /// exFAT's allocation bitmap or up-case table, as dump.exfat finds it in the root.
    /// </summary>
    internal sealed class ReferenceRootEntry
    {
        #region Properties

        public uint FirstCluster { get; init; }

        public long Length { get; init; }

        public uint? Checksum { get; init; }

        public List<long[]> Clusters { get; init; } = new();

        #endregion
    }
}
