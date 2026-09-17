using System.Buffers.Binary;
using OutWit.Common.Fat.Devices;

namespace OutWit.Common.Fat.Tests.Utils
{
    /// <summary>
    /// Formats a small, empty volume in memory from the specification's offsets, for tests
    /// that need an exact number of clusters or root entries.
    /// </summary>
    internal static class BlankVolume
    {
        #region Constants

        public const uint SERIAL = 0x0B1A4C00;

        private const byte MEDIA = 0xF8;

        private const string NO_LABEL = "NO NAME    ";

        private const int FS_INFO_SECTOR = 1;

        private const int BACKUP_BOOT_SECTOR = 6;

        #endregion

        #region Functions

        /// <summary>
        /// A formatted volume with no files.
        /// </summary>
        /// <param name="kind">FAT12 below 4085 clusters, FAT16 up to 65524, FAT32 at any count.</param>
        /// <param name="clusters">The number of clusters; on FAT32 the root takes the first.</param>
        /// <param name="sectorsPerCluster">The cluster size in sectors.</param>
        /// <param name="rootEntries">The fixed root's size on FAT12 and FAT16.</param>
        /// <param name="fatCount">The number of table copies.</param>
        /// <param name="sectorSize">The sector size.</param>
        public static async Task<BlockDeviceMemory> CreateAsync(FatKind kind, long clusters, int sectorsPerCluster = 1,
            int rootEntries = 16, int fatCount = 2, int sectorSize = 512)
        {
            bool isFat32 = kind == FatKind.Fat32;
            int entryBits = kind switch { FatKind.Fat12 => 12, FatKind.Fat16 => 16, _ => 32 };
            var builder = FatBootSectorBuilder.ForClusters(clusters, isFat32, sectorsPerCluster, sectorSize, entryBits);
            builder.FatCount = fatCount;
            builder.RootEntries = isFat32 ? 0 : rootEntries;
            builder.VolumeSerial = SERIAL;
            builder.Media = MEDIA;
            builder.TotalSectors = builder.DataStart + clusters * sectorsPerCluster;

            var device = new BlockDeviceMemory(builder.TotalSectors, sectorSize);
            var boot = builder.Build();
            int extended = isFat32 ? 64 : 36;
            System.Text.Encoding.ASCII.GetBytes(NO_LABEL).CopyTo(boot, extended + 7);
            System.Text.Encoding.ASCII.GetBytes(kind.ToString().ToUpperInvariant().PadRight(8)).CopyTo(boot, extended + 18);
            await device.WriteAsync(0, boot);

            var table = new byte[builder.FatSectors * sectorSize];
            SetEntry(table, kind, 0, 0x0FFFFF00u | MEDIA);
            SetEntry(table, kind, 1, 0x0FFFFFFF);
            if (isFat32)
            {
                SetEntry(table, kind, 2, 0x0FFFFFFF);
                await device.WriteAsync(BACKUP_BOOT_SECTOR, boot);
                var fsInfo = FsInfo(sectorSize, (uint)(clusters - 1), 3);
                await device.WriteAsync(FS_INFO_SECTOR, fsInfo);
                await device.WriteAsync(BACKUP_BOOT_SECTOR + FS_INFO_SECTOR, fsInfo);
            }

            for (int copy = 0; copy < fatCount; copy++)
                await device.WriteAsync(builder.ReservedSectors + copy * builder.FatSectors, table);

            return device;
        }

        /// <summary>
        /// An FSInfo sector with the given hints.
        /// </summary>
        public static byte[] FsInfo(int sectorSize, uint freeCount, uint nextFree)
        {
            var sector = new byte[sectorSize];
            BinaryPrimitives.WriteUInt32LittleEndian(sector.AsSpan(0), 0x41615252);
            BinaryPrimitives.WriteUInt32LittleEndian(sector.AsSpan(484), 0x61417272);
            BinaryPrimitives.WriteUInt32LittleEndian(sector.AsSpan(488), freeCount);
            BinaryPrimitives.WriteUInt32LittleEndian(sector.AsSpan(492), nextFree);
            BinaryPrimitives.WriteUInt32LittleEndian(sector.AsSpan(508), 0xAA550000);
            return sector;
        }

        /// <summary>
        /// Reads the free count and next-free hint of an FSInfo sector, by default a blank volume's.
        /// </summary>
        public static async Task<(uint FreeCount, uint NextFree)> ReadFsInfoAsync(IBlockDevice device, long at = FS_INFO_SECTOR)
        {
            var sector = new byte[device.SectorSize];
            await device.ReadAsync(at, sector);
            return (BinaryPrimitives.ReadUInt32LittleEndian(sector.AsSpan(488)), BinaryPrimitives.ReadUInt32LittleEndian(sector.AsSpan(492)));
        }

        private static void SetEntry(byte[] table, FatKind kind, int cluster, uint value)
        {
            switch (kind)
            {
                case FatKind.Fat12:
                    int offset = cluster + cluster / 2;
                    if ((cluster & 1) == 0)
                    {
                        table[offset] = (byte)value;
                        table[offset + 1] = (byte)((table[offset + 1] & 0xF0) | (int)((value >> 8) & 0x0F));
                    }
                    else
                    {
                        table[offset] = (byte)((table[offset] & 0x0F) | (int)((value << 4) & 0xF0));
                        table[offset + 1] = (byte)(value >> 4);
                    }
                    break;
                case FatKind.Fat16:
                    BinaryPrimitives.WriteUInt16LittleEndian(table.AsSpan(cluster * 2), (ushort)value);
                    break;
                default:
                    BinaryPrimitives.WriteUInt32LittleEndian(table.AsSpan(cluster * 4), value);
                    break;
            }
        }

        #endregion
    }
}
