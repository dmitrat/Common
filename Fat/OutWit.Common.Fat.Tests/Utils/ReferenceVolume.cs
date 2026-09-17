using OutWit.Common.Fat.Model;

namespace OutWit.Common.Fat.Tests.Utils
{
    /// <summary>
    /// A volume as fsck.vfat or dump.exfat describes it, with its listing as the kernel
    /// reads it. A <c>null</c> means the oracle does not report the value.
    /// </summary>
    internal sealed class ReferenceVolume
    {
        #region Properties

        public int? PartitionIndex { get; init; }

        public long FirstSector { get; init; }

        public long SectorCount { get; init; }

        public FatKind Kind { get; init; }

        public int SectorSize { get; init; }

        public int SectorsPerCluster { get; init; }

        public long TotalSectors { get; init; }

        public long FatOffset { get; init; }

        public int? FatCount { get; init; }

        public long FatSectors { get; init; }

        public long? RootDirectorySector { get; init; }

        public int? RootDirectoryEntries { get; init; }

        public uint? RootCluster { get; init; }

        public long ClusterHeapSector { get; init; }

        public uint ClusterCount { get; init; }

        public uint VolumeSerial { get; init; }

        public string? Label { get; init; }

        /// <summary>
        /// Free clusters: the table's count from fsck.vfat, the bitmap's from dump.exfat.
        /// </summary>
        public long FreeClusters { get; init; }

        public ReferenceRootEntry? Bitmap { get; init; }

        public ReferenceRootEntry? Upcase { get; init; }

        public List<ReferenceEntry> Entries { get; init; } = new();

        #endregion
    }
}
