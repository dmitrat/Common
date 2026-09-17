using System.Buffers.Binary;
using OutWit.Common.Fat.Devices;

namespace OutWit.Common.Fat.Tests.Utils
{
    /// <summary>
    /// Formats a small, empty exFAT volume in memory the way mkfs.exfat lays one out: the
    /// allocation bitmap, the up-case table and the root in the first clusters, each with a
    /// table chain, and the up-case table mkfs.exfat writes, taken from a reference image.
    /// </summary>
    internal static class BlankVolumeExFat
    {
        #region Constants

        public const uint SERIAL = 0x0E1A4C00;

        private const int MIN_FAT_OFFSET = 24;

        private const int MIN_VOLUME_BYTES = 1 << 20;

        private static readonly Lazy<Task<byte[]>> UPCASE = new(LoadUpcaseAsync);

        #endregion

        #region Functions

        /// <summary>
        /// A formatted volume with no files, of exactly <paramref name="clusters"/> clusters.
        /// </summary>
        public static async Task<BlockDeviceMemory> CreateAsync(long clusters, int sectorsPerCluster = 1, int sectorSize = 512,
            int fatCount = 1)
        {
            var upcase = await UPCASE.Value;
            int clusterSize = sectorsPerCluster * sectorSize;
            long bitmapBytes = (clusters + 7) / 8;
            uint bitmapClusters = (uint)((bitmapBytes + clusterSize - 1) / clusterSize);
            uint upcaseClusters = (uint)((upcase.Length + clusterSize - 1) / clusterSize);
            uint root = 2 + bitmapClusters + upcaseClusters;

            uint fatLength = (uint)(((clusters + 2) * 4 + sectorSize - 1) / sectorSize);
            long minSectors = MIN_VOLUME_BYTES / sectorSize;
            long fatOffset = Math.Max(MIN_FAT_OFFSET, minSectors - fatLength * fatCount - clusters * sectorsPerCluster);
            long heap = fatOffset + fatLength * fatCount;
            var builder = new ExFatBootRegionBuilder
            {
                SectorShift = (byte)Math.Log2(sectorSize),
                ClusterShift = (byte)Math.Log2(sectorsPerCluster),
                VolumeLength = (ulong)(heap + clusters * sectorsPerCluster),
                FatOffset = (uint)fatOffset,
                FatLength = fatLength,
                ClusterHeapOffset = (uint)heap,
                ClusterCount = (uint)clusters,
                RootCluster = root,
                VolumeSerial = SERIAL,
                FatCount = (byte)fatCount,
                PercentInUse = 0
            };

            var device = new BlockDeviceMemory((long)builder.VolumeLength, sectorSize);
            var region = builder.Build();
            await device.WriteAsync(0, region);
            await device.WriteAsync(12, region);

            var table = new byte[fatLength * sectorSize];
            BinaryPrimitives.WriteUInt32LittleEndian(table, 0xFFFFFFF8);
            BinaryPrimitives.WriteUInt32LittleEndian(table.AsSpan(4), 0xFFFFFFFF);
            Chain(table, 2, bitmapClusters);
            Chain(table, 2 + bitmapClusters, upcaseClusters);
            Chain(table, root, 1);
            for (int copy = 0; copy < fatCount; copy++)
                await device.WriteAsync(fatOffset + copy * fatLength, table);

            var bitmap = new byte[bitmapClusters * clusterSize];
            for (uint cluster = 2; cluster <= root; cluster++)
                bitmap[(cluster - 2) / 8] |= (byte)(1 << (int)((cluster - 2) % 8));
            await device.WriteAsync(heap, bitmap);
            var upcaseData = new byte[upcaseClusters * clusterSize];
            upcase.CopyTo(upcaseData, 0);
            await device.WriteAsync(heap + bitmapClusters * sectorsPerCluster, upcaseData);

            var rootData = new byte[clusterSize];
            ExFatSetBuilder.Bitmap(2, (ulong)bitmapBytes).CopyTo(rootData, 0);
            ExFatSetBuilder.Upcase(2 + bitmapClusters, upcase).CopyTo(rootData, ExFatSetBuilder.ENTRY);
            await device.WriteAsync(heap + (root - 2) * sectorsPerCluster, rootData);
            return device;
        }

        /// <summary>
        /// The up-case table mkfs.exfat writes.
        /// </summary>
        public static Task<byte[]> UpcaseTableAsync()
        {
            return UPCASE.Value;
        }

        private static void Chain(byte[] table, uint first, uint count)
        {
            for (uint i = 0; i < count; i++)
                BinaryPrimitives.WriteUInt32LittleEndian(table.AsSpan((int)(first + i) * 4), i + 1 < count ? first + i + 1 : 0xFFFFFFFF);
        }

        private static async Task<byte[]> LoadUpcaseAsync()
        {
            var image = ReferenceImages.Get("exfat-c8");
            var volume = image.Volumes[0];
            var entry = volume.Upcase!;
            await using var disk = await ReferenceImages.OpenAsync(image);
            var data = new byte[(entry.Length + volume.SectorSize - 1) / volume.SectorSize * volume.SectorSize];
            await disk.ReadAsync(volume.FirstSector + volume.ClusterHeapSector + (entry.FirstCluster - 2) * volume.SectorsPerCluster, data);
            return data[..(int)entry.Length];
        }

        #endregion
    }
}
