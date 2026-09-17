using System.Buffers.Binary;
using OutWit.Common.Fat.Allocation;
using OutWit.Common.Fat.Devices;
using OutWit.Common.Fat.Model;

namespace OutWit.Common.Fat.Tests.Utils
{
    /// <summary>
    /// An allocation table built entry by entry, with packing of its own, on a volume of
    /// one-sector clusters whose table starts at sector 1.
    /// </summary>
    internal sealed class FatTableImage
    {
        #region Constants

        public const int SECTOR_SIZE = 512;

        public const int TABLE_OFFSET = 1;

        #endregion

        #region Constructors

        /// <param name="kind">The table's width.</param>
        /// <param name="clusters">The number of clusters.</param>
        /// <param name="fatSectors">The size of one copy.</param>
        /// <param name="fatCount">The number of copies.</param>
        /// <param name="isMirrored">Whether the copies are kept the same.</param>
        /// <param name="activeFat">The copy in use when they are not.</param>
        /// <param name="fsInfoSector">Where FSInfo lies, sector 0 being free for it; none by default.</param>
        public FatTableImage(FatKind kind, uint clusters, int fatSectors, int fatCount = 1, bool isMirrored = true, int activeFat = 0,
            int? fsInfoSector = null)
        {
            Info = new FatVolumeInfo
            {
                Kind = kind,
                SectorSize = SECTOR_SIZE,
                SectorsPerCluster = 1,
                FatOffset = TABLE_OFFSET,
                FatCount = fatCount,
                FatSectors = fatSectors,
                ActiveFat = activeFat,
                IsFatMirrored = isMirrored,
                ClusterHeapSector = TABLE_OFFSET + fatCount * fatSectors,
                TotalSectors = TABLE_OFFSET + fatCount * fatSectors + clusters,
                ClusterCount = clusters,
                RootCluster = kind is FatKind.Fat32 or FatKind.ExFat ? 2u : null,
                FsInfoSector = fsInfoSector
            };
            Table = new byte[fatSectors * SECTOR_SIZE];
        }

        #endregion

        #region Functions

        public FatTableImage Set(uint cluster, uint value)
        {
            switch (Info.Kind)
            {
                case FatKind.Fat12:
                    int offset = (int)(cluster + cluster / 2);
                    if ((cluster & 1) == 0)
                    {
                        Table[offset] = (byte)value;
                        Table[offset + 1] = (byte)((Table[offset + 1] & 0xF0) | (int)((value >> 8) & 0x0F));
                    }
                    else
                    {
                        Table[offset] = (byte)((Table[offset] & 0x0F) | (int)((value << 4) & 0xF0));
                        Table[offset + 1] = (byte)(value >> 4);
                    }
                    break;
                case FatKind.Fat16:
                    BinaryPrimitives.WriteUInt16LittleEndian(Table.AsSpan((int)cluster * 2), (ushort)value);
                    break;
                default:
                    BinaryPrimitives.WriteUInt32LittleEndian(Table.AsSpan((int)cluster * 4), value);
                    break;
            }

            return this;
        }

        /// <summary>
        /// Links clusters into a chain ending with an end-of-chain mark.
        /// </summary>
        public FatTableImage Chain(params uint[] clusters)
        {
            for (int i = 0; i < clusters.Length - 1; i++)
                Set(clusters[i], clusters[i + 1]);
            return Set(clusters[^1], EndOfChain);
        }

        /// <summary>
        /// A device holding the table as copy <paramref name="copy"/>, the other copies zero.
        /// </summary>
        public async Task<BlockDeviceMemory> CreateDeviceAsync(int copy = 0)
        {
            var device = new BlockDeviceMemory(Info.TotalSectors);
            await device.WriteAsync(TABLE_OFFSET + copy * Info.FatSectors, Table);
            return device;
        }

        public FatTable CreateTable(IBlockDevice device)
        {
            return FatTable.Create(device, Info);
        }

        #endregion

        #region Properties

        public FatVolumeInfo Info { get; }

        public byte[] Table { get; }

        public uint EndOfChain => Info.Kind switch { FatKind.Fat12 => 0xFFF, FatKind.Fat16 => 0xFFFF, FatKind.Fat32 => 0x0FFFFFFF, _ => 0xFFFFFFFF };

        /// <summary>
        /// The sector a cluster starts at.
        /// </summary>
        public long ClusterSector(uint cluster)
        {
            return Info.ClusterHeapSector + cluster - 2;
        }

        #endregion
    }
}
