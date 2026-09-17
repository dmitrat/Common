using System;
using System.Buffers.Binary;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Fat.Boot;
using OutWit.Common.Fat.Devices;
using OutWit.Common.Fat.Directories;
using OutWit.Common.Fat.Model;

namespace OutWit.Common.Fat.Formatting
{
    /// <summary>
    /// Where the structures of a new FAT12, FAT16 or FAT32 volume go, and writing them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// FAT12 and FAT16 have one reserved sector, FAT32 thirty-two with FSInfo in sector 1
    /// and the backups of both in sectors 6 and 7. The tables are sized to address every
    /// cluster the data area holds, as the specification sizes them, and the count must fall
    /// where readers agree on the kind: below 4085 clusters for FAT12, from 4086 to 65524 for
    /// FAT16, from 65525 for FAT32.
    /// </para>
    /// <para>
    /// The boot sector is written last, after the sector it replaces has been cleared first,
    /// so an interrupted format is not taken for a volume. The data area is not cleared,
    /// except FAT32's root cluster.
    /// </para>
    /// </remarks>
    internal sealed class FatLayoutVfat
    {
        #region Constants

        private const uint MAX_FAT12_CLUSTERS = 4084;

        /// <summary>
        /// The fewest clusters a FAT16 volume is given. Linux reads 4085 and more as FAT16, but
        /// Windows reads fewer than 4087 as FAT12, so mkfs.fat makes neither 4085 nor 4086.
        /// </summary>
        private const uint MIN_FAT16_CLUSTERS = 4087;

        private const uint MAX_FAT16_CLUSTERS = 65524;

        private const uint MIN_FAT32_CLUSTERS = 65525;

        private const uint MAX_FAT32_CLUSTERS = 0x0FFFFFF5;

        private const int FAT_RESERVED_SECTORS = 1;

        private const int ZERO_CHUNK_BYTES = 1 << 20;

        #endregion

        #region Constructors

        private FatLayoutVfat()
        {
        }

        #endregion

        #region Functions

        /// <summary>
        /// Lays out a volume.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The table count or the root size is out of range.</exception>
        /// <exception cref="ArgumentException">The volume would have a number of clusters the kind cannot have.</exception>
        public static FatLayoutVfat Plan(FatKind kind, int sectorSize, long totalSectors, int sectorsPerCluster, int fatCount, int rootEntries)
        {
            if (fatCount is < 1 or > 2)
                throw new ArgumentOutOfRangeException(nameof(fatCount), fatCount, "A volume has one or two allocation tables.");

            bool isFat32 = kind == FatKind.Fat32;
            int rootSectors = 0;
            if (!isFat32)
            {
                if (rootEntries is < 1 or > ushort.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(rootEntries), rootEntries, "The root directory holds from 1 to 65535 entries.");
                rootSectors = (rootEntries * DirectorySlot.SIZE + sectorSize - 1) / sectorSize;
                rootEntries = rootSectors * sectorSize / DirectorySlot.SIZE;
                if (rootEntries > ushort.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(rootEntries), rootEntries, "The root directory holds at most 65535 entries.");
            }

            int reserved = isFat32 ? FatBootSectorWriter.FAT32_RESERVED_SECTORS : FAT_RESERVED_SECTORS;
            int entryBits = kind switch { FatKind.Fat12 => 12, FatKind.Fat16 => 16, _ => 32 };
            long fatSectors = 1;
            long clusters;
            while (true)
            {
                long dataStart = reserved + fatCount * fatSectors + rootSectors;
                clusters = (totalSectors - dataStart) / sectorsPerCluster;
                if (clusters < 1)
                    throw new ArgumentException($"A {kind} volume of {totalSectors} sectors has no room for a cluster.", nameof(totalSectors));

                long needed = ((clusters + 2) * entryBits / 8 + 1 + sectorSize - 1) / sectorSize;
                if (needed <= fatSectors)
                    break;
                fatSectors = needed;
            }

            CheckClusters(kind, clusters, totalSectors, sectorsPerCluster * sectorSize);
            return new FatLayoutVfat
            {
                Kind = kind,
                SectorSize = sectorSize,
                SectorsPerCluster = sectorsPerCluster,
                ReservedSectors = reserved,
                FatCount = fatCount,
                FatSectors = fatSectors,
                RootEntries = isFat32 ? 0 : rootEntries,
                RootSectors = rootSectors,
                TotalSectors = totalSectors,
                ClusterCount = clusters
            };
        }

        /// <summary>
        /// Writes the volume's structures.
        /// </summary>
        public async ValueTask WriteAsync(IBlockDevice device, long hiddenSectors, uint serial, string? label, DateTime now,
            CancellationToken cancellationToken)
        {
            var labelBytes = FatFormatPlan.LabelBytes(label);
            await ZeroAsync(device, 0, ReservedSectors, cancellationToken).ConfigureAwait(false);

            var firstTableSector = new byte[SectorSize];
            WriteReservedEntries(firstTableSector);
            for (int copy = 0; copy < FatCount; copy++)
            {
                long start = ReservedSectors + copy * FatSectors;
                await ZeroAsync(device, start, FatSectors, cancellationToken).ConfigureAwait(false);
                await device.WriteAsync(start, firstTableSector, cancellationToken).ConfigureAwait(false);
            }

            long rootSector = Kind == FatKind.Fat32 ? ClusterHeapSector : ReservedSectors + FatCount * FatSectors;
            await ZeroAsync(device, rootSector, Kind == FatKind.Fat32 ? SectorsPerCluster : RootSectors, cancellationToken).ConfigureAwait(false);
            if (label != null)
            {
                var slot = new byte[SectorSize];
                DirectorySlotEncoder.WriteShort(slot, labelBytes, 0, FatAttributes.VolumeLabel, 0, 0, now, now);
                await device.WriteAsync(rootSector, slot, cancellationToken).ConfigureAwait(false);
            }

            var boot = FatBootSectorWriter.Build(Kind, SectorSize, SectorsPerCluster, ReservedSectors, FatCount, RootEntries,
                TotalSectors, FatSectors, hiddenSectors, serial, labelBytes);
            if (Kind == FatKind.Fat32)
            {
                var fsInfo = FatBootSectorWriter.BuildFsInfo(SectorSize, (uint)(ClusterCount - 1), FatBootSectorWriter.FAT32_ROOT_CLUSTER + 1);
                await device.WriteAsync(FatBootSectorWriter.FS_INFO_SECTOR, fsInfo, cancellationToken).ConfigureAwait(false);
                await device.WriteAsync(FatBootSectorWriter.BACKUP_BOOT_SECTOR + FatBootSectorWriter.FS_INFO_SECTOR, fsInfo, cancellationToken).ConfigureAwait(false);
                await device.WriteAsync(FatBootSectorWriter.BACKUP_BOOT_SECTOR, boot, cancellationToken).ConfigureAwait(false);
            }

            await device.WriteAsync(0, boot, cancellationToken).ConfigureAwait(false);
            await device.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Clears sectors, a large piece at a time.
        /// </summary>
        public static async ValueTask ZeroAsync(IBlockDevice device, long sector, long count, CancellationToken cancellationToken)
        {
            int chunk = (int)Math.Max(1, Math.Min(ZERO_CHUNK_BYTES / device.SectorSize, count));
            var zeros = new byte[chunk * device.SectorSize];
            for (long done = 0; done < count; done += chunk)
            {
                int sectors = (int)Math.Min(chunk, count - done);
                await device.WriteAsync(sector + done, zeros.AsMemory(0, sectors * device.SectorSize), cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Entries 0 and 1 — the media byte, and an end of chain with the clean flags set —
        /// and on FAT32 the root's end of chain.
        /// </summary>
        private void WriteReservedEntries(Span<byte> sector)
        {
            switch (Kind)
            {
                case FatKind.Fat12:
                    sector[0] = FatBootSectorWriter.MEDIA_FIXED;
                    sector[1] = 0xFF;
                    sector[2] = 0xFF;
                    break;
                case FatKind.Fat16:
                    BinaryPrimitives.WriteUInt16LittleEndian(sector, 0xFF00 | FatBootSectorWriter.MEDIA_FIXED);
                    BinaryPrimitives.WriteUInt16LittleEndian(sector[2..], 0xFFFF);
                    break;
                default:
                    BinaryPrimitives.WriteUInt32LittleEndian(sector, 0x0FFFFF00 | FatBootSectorWriter.MEDIA_FIXED);
                    BinaryPrimitives.WriteUInt32LittleEndian(sector[4..], 0x0FFFFFFF);
                    BinaryPrimitives.WriteUInt32LittleEndian(sector[8..], 0x0FFFFFFF);
                    break;
            }
        }

        private static void CheckClusters(FatKind kind, long clusters, long totalSectors, int clusterSize)
        {
            var (min, max) = kind switch
            {
                FatKind.Fat12 => (1u, MAX_FAT12_CLUSTERS),
                FatKind.Fat16 => (MIN_FAT16_CLUSTERS, MAX_FAT16_CLUSTERS),
                _ => (MIN_FAT32_CLUSTERS, MAX_FAT32_CLUSTERS)
            };

            if (clusters < min || clusters > max)
                throw new ArgumentException(
                    $"A {kind} volume of {totalSectors} sectors with {clusterSize}-byte clusters would have {clusters} clusters; {kind} needs {min} to {max}.");
            if (totalSectors > uint.MaxValue)
                throw new ArgumentException($"A FAT volume holds at most {uint.MaxValue} sectors; this one has {totalSectors}.");
        }

        #endregion

        #region Properties

        public FatKind Kind { get; private init; }

        public int SectorSize { get; private init; }

        public int SectorsPerCluster { get; private init; }

        public int ReservedSectors { get; private init; }

        public int FatCount { get; private init; }

        public long FatSectors { get; private init; }

        public int RootEntries { get; private init; }

        public int RootSectors { get; private init; }

        public long TotalSectors { get; private init; }

        public long ClusterCount { get; private init; }

        public long ClusterHeapSector => ReservedSectors + FatCount * FatSectors + RootSectors;

        #endregion
    }
}
