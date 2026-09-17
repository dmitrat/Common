namespace OutWit.Common.Fat.Tests.Utils
{
    /// <summary>
    /// One disk image in the manifest.
    /// </summary>
    internal sealed class ReferenceImage
    {
        #region Functions

        public override string ToString()
        {
            return Name;
        }

        #endregion

        #region Properties

        public string Name { get; init; } = "";

        public string File { get; init; } = "";

        public string Description { get; init; } = "";

        public string Sha256 { get; init; } = "";

        public int SectorSize { get; init; }

        public long SectorCount { get; init; }

        public string PartitionTable { get; init; } = "";

        public uint? DiskSignature { get; init; }

        public List<ReferencePartition> Partitions { get; init; } = new();

        public List<ReferenceVolume> Volumes { get; init; } = new();

        #endregion
    }
}
