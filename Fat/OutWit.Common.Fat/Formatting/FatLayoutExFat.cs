using System;
using System.Buffers.Binary;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Fat.Boot;
using OutWit.Common.Fat.Devices;
using OutWit.Common.Fat.ExFat;

namespace OutWit.Common.Fat.Formatting
{
    /// <summary>
    /// Where the structures of a new exFAT volume go, and writing them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The table follows the two boot regions, the cluster heap starts at the next cluster
    /// boundary, and the first clusters hold the allocation bitmap, the up-case table the
    /// specification recommends, and the root, each with a table chain, as mkfs.exfat and
    /// Windows lay them out. The root's first slot holds the label entry, with no characters
    /// when there is no label, as mkfs.exfat writes it.
    /// </para>
    /// <para>
    /// Both boot regions are written last, the backup first, after the main boot sector has
    /// been cleared, so an interrupted format is not taken for a volume.
    /// </para>
    /// </remarks>
    internal sealed class FatLayoutExFat
    {
        #region Constants

        private const long MIN_VOLUME_BYTES = 1L << 20;

        private const uint MAX_CLUSTERS = 0xFFFFFFF5;

        private const uint END_OF_CHAIN = 0xFFFFFFFF;

        private const uint MEDIA_ENTRY = 0xFFFFFFF8;

        #endregion

        #region Constructors

        private FatLayoutExFat()
        {
        }

        #endregion

        #region Functions

        /// <summary>
        /// Lays out a volume.
        /// </summary>
        /// <exception cref="ArgumentException">The volume is below 1 MiB, or too small for its own structures.</exception>
        public static FatLayoutExFat Plan(int sectorSize, long totalSectors, int sectorsPerCluster)
        {
            if (totalSectors * sectorSize < MIN_VOLUME_BYTES)
                throw new ArgumentException($"An exFAT volume is at least 1 MiB; this one has {totalSectors} sectors of {sectorSize} bytes.");

            uint fatOffset = ExFatBootRegionWriter.FAT_OFFSET;
            long fatLength = 1;
            long heap;
            long clusters;
            while (true)
            {
                heap = (fatOffset + fatLength + sectorsPerCluster - 1) / sectorsPerCluster * sectorsPerCluster;
                clusters = Math.Min((totalSectors - heap) / sectorsPerCluster, MAX_CLUSTERS);
                long needed = ((clusters + 2) * sizeof(uint) + sectorSize - 1) / sectorSize;
                if (needed <= fatLength)
                    break;
                fatLength = needed;
            }

            int clusterSize = sectorsPerCluster * sectorSize;
            long bitmapBytes = (clusters + 7) / 8;
            long bitmapClusters = (bitmapBytes + clusterSize - 1) / clusterSize;
            long upcaseClusters = (ExFatUpcaseDefault.Data.Length + clusterSize - 1) / clusterSize;
            if (clusters < bitmapClusters + upcaseClusters + 1)
                throw new ArgumentException($"An exFAT volume of {clusters} clusters has no room for its own structures.");

            return new FatLayoutExFat
            {
                SectorSize = sectorSize,
                SectorsPerCluster = sectorsPerCluster,
                TotalSectors = totalSectors,
                FatOffset = fatOffset,
                FatLength = (uint)fatLength,
                ClusterHeapOffset = (uint)heap,
                ClusterCount = (uint)clusters,
                BitmapBytes = bitmapBytes,
                BitmapClusters = (uint)bitmapClusters,
                UpcaseClusters = (uint)upcaseClusters
            };
        }

        /// <summary>
        /// Writes the volume's structures, the boot regions having been cleared.
        /// </summary>
        public async ValueTask WriteAsync(IBlockDevice device, long partitionOffset, uint serial, string? label, CancellationToken cancellationToken)
        {
            await WriteTableAsync(device, cancellationToken).ConfigureAwait(false);
            await WriteBitmapAsync(device, cancellationToken).ConfigureAwait(false);

            var upcase = new byte[UpcaseClusters * ClusterSize];
            ExFatUpcaseDefault.Data.Span.CopyTo(upcase);
            await device.WriteAsync(ClusterSector(UpcaseCluster), upcase, cancellationToken).ConfigureAwait(false);

            var root = new byte[ClusterSize];
            ExFatEntryEncoder.WriteLabel(root.AsSpan(0, ExFatEntry.SIZE), label);
            var bitmap = root.AsSpan(ExFatEntry.SIZE, ExFatEntry.SIZE);
            bitmap[ExFatEntry.TYPE] = ExFatEntry.ALLOCATION_BITMAP;
            BinaryPrimitives.WriteUInt32LittleEndian(bitmap[ExFatEntry.FIRST_CLUSTER..], BitmapCluster);
            BinaryPrimitives.WriteUInt64LittleEndian(bitmap[ExFatEntry.DATA_LENGTH..], (ulong)BitmapBytes);
            var table = root.AsSpan(2 * ExFatEntry.SIZE, ExFatEntry.SIZE);
            table[ExFatEntry.TYPE] = ExFatEntry.UPCASE_TABLE;
            BinaryPrimitives.WriteUInt32LittleEndian(table[ExFatEntry.UPCASE_CHECKSUM..], ExFatUpcaseDefault.CHECKSUM);
            BinaryPrimitives.WriteUInt32LittleEndian(table[ExFatEntry.FIRST_CLUSTER..], UpcaseCluster);
            BinaryPrimitives.WriteUInt64LittleEndian(table[ExFatEntry.DATA_LENGTH..], (ulong)ExFatUpcaseDefault.Data.Length);
            await device.WriteAsync(ClusterSector(RootCluster), root, cancellationToken).ConfigureAwait(false);

            byte percent = (byte)((RootCluster - 1L) * 100 / ClusterCount);
            var region = ExFatBootRegionWriter.Build(SectorSize, SectorsPerCluster, partitionOffset, TotalSectors, FatOffset, FatLength,
                ClusterHeapOffset, ClusterCount, RootCluster, serial, percent);
            await device.WriteAsync(ExFatBootSector.BACKUP_BOOT_SECTOR, region, cancellationToken).ConfigureAwait(false);
            await device.WriteAsync(0, region, cancellationToken).ConfigureAwait(false);
            await device.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        private async ValueTask WriteTableAsync(IBlockDevice device, CancellationToken cancellationToken)
        {
            await FatLayoutVfat.ZeroAsync(device, FatOffset, FatLength, cancellationToken).ConfigureAwait(false);

            int entries = (int)RootCluster + 1;
            var head = new byte[(entries * sizeof(uint) + SectorSize - 1) / SectorSize * SectorSize];
            BinaryPrimitives.WriteUInt32LittleEndian(head, MEDIA_ENTRY);
            BinaryPrimitives.WriteUInt32LittleEndian(head.AsSpan(4), END_OF_CHAIN);
            Chain(head, BitmapCluster, BitmapClusters);
            Chain(head, UpcaseCluster, UpcaseClusters);
            Chain(head, RootCluster, 1);
            await device.WriteAsync(FatOffset, head, cancellationToken).ConfigureAwait(false);
        }

        private async ValueTask WriteBitmapAsync(IBlockDevice device, CancellationToken cancellationToken)
        {
            var bitmap = new byte[BitmapClusters * ClusterSize];
            for (uint cluster = BitmapCluster; cluster <= RootCluster; cluster++)
                bitmap[(cluster - 2) / 8] |= (byte)(1 << (int)((cluster - 2) % 8));
            await device.WriteAsync(ClusterSector(BitmapCluster), bitmap, cancellationToken).ConfigureAwait(false);
        }

        private static void Chain(byte[] table, uint first, uint count)
        {
            for (uint i = 0; i < count; i++)
                BinaryPrimitives.WriteUInt32LittleEndian(table.AsSpan((int)(first + i) * sizeof(uint)), i + 1 < count ? first + i + 1 : END_OF_CHAIN);
        }

        private long ClusterSector(uint cluster)
        {
            return ClusterHeapOffset + (cluster - 2L) * SectorsPerCluster;
        }

        #endregion

        #region Properties

        public int SectorSize { get; private init; }

        public int SectorsPerCluster { get; private init; }

        public long TotalSectors { get; private init; }

        public uint FatOffset { get; private init; }

        public uint FatLength { get; private init; }

        public uint ClusterHeapOffset { get; private init; }

        public uint ClusterCount { get; private init; }

        public long BitmapBytes { get; private init; }

        public uint BitmapClusters { get; private init; }

        public uint UpcaseClusters { get; private init; }

        public int ClusterSize => SectorSize * SectorsPerCluster;

        public uint BitmapCluster => 2;

        public uint UpcaseCluster => BitmapCluster + BitmapClusters;

        public uint RootCluster => UpcaseCluster + UpcaseClusters;

        #endregion
    }
}
