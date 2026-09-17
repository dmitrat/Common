using OutWit.Common.Abstract;
using OutWit.Common.Attributes;
using OutWit.Common.Values;

namespace OutWit.Common.Fat
{
    /// <summary>
    /// The layout of a FAT or exFAT volume, as its boot sector declares it.
    /// </summary>
    /// <remarks>
    /// Sector numbers are relative to the start of the volume. A property that does not
    /// apply to the volume's kind is <c>null</c>.
    /// </remarks>
    public sealed class FatVolumeInfo : ModelBase
    {
        #region Model Base

        /// <inheritdoc />
        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not FatVolumeInfo other)
                return false;

            return Kind.Is(other.Kind)
                   && SectorSize.Is(other.SectorSize)
                   && SectorsPerCluster.Is(other.SectorsPerCluster)
                   && TotalSectors.Is(other.TotalSectors)
                   && FatOffset.Is(other.FatOffset)
                   && FatCount.Is(other.FatCount)
                   && FatSectors.Is(other.FatSectors)
                   && ActiveFat.Is(other.ActiveFat)
                   && IsFatMirrored.Is(other.IsFatMirrored)
                   && RootDirectorySector.Is(other.RootDirectorySector)
                   && RootDirectoryEntries.Is(other.RootDirectoryEntries)
                   && RootCluster.Is(other.RootCluster)
                   && ClusterHeapSector.Is(other.ClusterHeapSector)
                   && ClusterCount.Is(other.ClusterCount)
                   && VolumeSerial.Is(other.VolumeSerial)
                   && FsInfoSector.Is(other.FsInfoSector)
                   && BackupBootSector.Is(other.BackupBootSector);
        }

        /// <inheritdoc />
        public override FatVolumeInfo Clone()
        {
            return new FatVolumeInfo
            {
                Kind = Kind,
                SectorSize = SectorSize,
                SectorsPerCluster = SectorsPerCluster,
                TotalSectors = TotalSectors,
                FatOffset = FatOffset,
                FatCount = FatCount,
                FatSectors = FatSectors,
                ActiveFat = ActiveFat,
                IsFatMirrored = IsFatMirrored,
                RootDirectorySector = RootDirectorySector,
                RootDirectoryEntries = RootDirectoryEntries,
                RootCluster = RootCluster,
                ClusterHeapSector = ClusterHeapSector,
                ClusterCount = ClusterCount,
                VolumeSerial = VolumeSerial,
                FsInfoSector = FsInfoSector,
                BackupBootSector = BackupBootSector
            };
        }

        #endregion

        #region Properties

        /// <summary>
        /// The FAT variant.
        /// </summary>
        [ToString]
        public FatKind Kind { get; init; }

        /// <summary>
        /// The size of a sector in bytes.
        /// </summary>
        [ToString]
        public int SectorSize { get; init; }

        /// <summary>
        /// The number of sectors in a cluster; a power of two.
        /// </summary>
        public int SectorsPerCluster { get; init; }

        /// <summary>
        /// The size of a cluster in bytes.
        /// </summary>
        [ToString]
        public int ClusterSize => SectorSize * SectorsPerCluster;

        /// <summary>
        /// The length of the volume in sectors.
        /// </summary>
        [ToString]
        public long TotalSectors { get; init; }

        /// <summary>
        /// The first sector of the first allocation table.
        /// </summary>
        public long FatOffset { get; init; }

        /// <summary>
        /// The number of allocation tables.
        /// </summary>
        public int FatCount { get; init; }

        /// <summary>
        /// The length of one allocation table in sectors.
        /// </summary>
        public long FatSectors { get; init; }

        /// <summary>
        /// The table that is read and written when the tables are not mirrored; zero-based.
        /// </summary>
        public int ActiveFat { get; init; }

        /// <summary>
        /// Whether every write to the allocation table goes to all copies. Always so for
        /// FAT12 and FAT16; optional for FAT32; never for exFAT, whose second table
        /// belongs to TexFAT.
        /// </summary>
        public bool IsFatMirrored { get; init; }

        /// <summary>
        /// The first sector of the fixed root directory; FAT12 and FAT16 only.
        /// </summary>
        public long? RootDirectorySector { get; init; }

        /// <summary>
        /// The number of entries the fixed root directory holds; FAT12 and FAT16 only.
        /// </summary>
        public int? RootDirectoryEntries { get; init; }

        /// <summary>
        /// The first cluster of the root directory; FAT32 and exFAT only.
        /// </summary>
        public uint? RootCluster { get; init; }

        /// <summary>
        /// The first sector of cluster 2, where the data area begins.
        /// </summary>
        public long ClusterHeapSector { get; init; }

        /// <summary>
        /// The number of clusters in the data area; they are numbered from 2.
        /// </summary>
        [ToString]
        public uint ClusterCount { get; init; }

        /// <summary>
        /// The volume serial number, or zero when the boot sector does not carry one.
        /// </summary>
        [ToString(Format = "X8")]
        public uint VolumeSerial { get; init; }

        /// <summary>
        /// The sector of the FSInfo structure; FAT32 only, and <c>null</c> when the volume has none.
        /// </summary>
        public int? FsInfoSector { get; init; }

        /// <summary>
        /// The first sector of the backup boot sector or boot region, or <c>null</c> when
        /// the volume has none.
        /// </summary>
        public int? BackupBootSector { get; init; }

        #endregion
    }
}
