using System.Buffers.Binary;

namespace OutWit.Common.Fat.Tests.Utils
{
    /// <summary>
    /// Writes FAT12/16/32 boot sectors field by field, from the specification's offsets,
    /// so tests do not check the parser against itself.
    /// </summary>
    internal sealed class FatBootSectorBuilder
    {
        #region Functions

        /// <summary>
        /// A layout with exactly <paramref name="clusters"/> clusters and a table just large
        /// enough for them in entries of <paramref name="entryBits"/> bits. The default of 32
        /// leaves room for any width, so only the cluster count decides the kind.
        /// </summary>
        public static FatBootSectorBuilder ForClusters(long clusters, bool fat32Layout, int sectorsPerCluster = 1,
            int bytesPerSector = 512, int entryBits = 32)
        {
            var builder = new FatBootSectorBuilder
            {
                BytesPerSector = bytesPerSector,
                SectorsPerCluster = sectorsPerCluster,
                IsFat32Layout = fat32Layout,
                ReservedSectors = fat32Layout ? 32 : 1,
                RootEntries = fat32Layout ? 0 : 512
            };

            long tableBits = (clusters + 2) * entryBits;
            builder.FatSectors = (tableBits + 8L * bytesPerSector - 1) / (8L * bytesPerSector);
            builder.TotalSectors = builder.DataStart + clusters * sectorsPerCluster;
            return builder;
        }

        public byte[] Build()
        {
            // At least 512 bytes, so implausible sector sizes still make a whole sector.
            var sector = new byte[Math.Max(BytesPerSector, 512)];
            var span = sector.AsSpan();

            span[0] = Jump;
            span[1] = 0x3C;
            span[2] = 0x90;
            "OUTWIT  "u8.CopyTo(span[3..]);
            BinaryPrimitives.WriteUInt16LittleEndian(span[11..], (ushort)BytesPerSector);
            span[13] = (byte)SectorsPerCluster;
            BinaryPrimitives.WriteUInt16LittleEndian(span[14..], (ushort)ReservedSectors);
            span[16] = (byte)FatCount;
            BinaryPrimitives.WriteUInt16LittleEndian(span[17..], (ushort)RootEntries);
            span[21] = Media;
            BinaryPrimitives.WriteUInt32LittleEndian(span[28..], HiddenSectors);

            bool smallTotal = !IsFat32Layout && TotalSectors < 0x10000;
            BinaryPrimitives.WriteUInt16LittleEndian(span[19..], smallTotal ? (ushort)TotalSectors : (ushort)0);
            BinaryPrimitives.WriteUInt32LittleEndian(span[32..], smallTotal ? 0 : (uint)TotalSectors);

            int extended = 36;
            if (IsFat32Layout)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(span[36..], (uint)FatSectors);
                BinaryPrimitives.WriteUInt16LittleEndian(span[40..], ExtendedFlags);
                BinaryPrimitives.WriteUInt16LittleEndian(span[42..], Version);
                BinaryPrimitives.WriteUInt32LittleEndian(span[44..], RootCluster);
                BinaryPrimitives.WriteUInt16LittleEndian(span[48..], FsInfoSector);
                BinaryPrimitives.WriteUInt16LittleEndian(span[50..], BackupBootSector);
                extended = 64;
            }
            else
            {
                BinaryPrimitives.WriteUInt16LittleEndian(span[22..], (ushort)FatSectors);
            }

            span[extended] = 0x80;
            if (HasSerial)
            {
                span[extended + 2] = 0x29;
                BinaryPrimitives.WriteUInt32LittleEndian(span[(extended + 3)..], VolumeSerial);
            }

            if (HasSignature)
            {
                span[510] = 0x55;
                span[511] = 0xAA;
            }

            return sector;
        }

        #endregion

        #region Properties

        public byte Jump { get; set; } = 0xEB;

        public int BytesPerSector { get; set; } = 512;

        public int SectorsPerCluster { get; set; } = 1;

        public int ReservedSectors { get; set; } = 1;

        public int FatCount { get; set; } = 2;

        public int RootEntries { get; set; } = 512;

        public long TotalSectors { get; set; }

        public uint HiddenSectors { get; set; }

        public byte Media { get; set; } = 0xF8;

        public long FatSectors { get; set; }

        public bool IsFat32Layout { get; set; }

        public ushort ExtendedFlags { get; set; }

        public ushort Version { get; set; }

        public uint RootCluster { get; set; } = 2;

        public ushort FsInfoSector { get; set; } = 1;

        public ushort BackupBootSector { get; set; } = 6;

        public bool HasSerial { get; set; } = true;

        public uint VolumeSerial { get; set; } = 0xCAFEF00D;

        public bool HasSignature { get; set; } = true;

        public long DataStart => ReservedSectors + FatCount * FatSectors + (RootEntries * 32L + BytesPerSector - 1) / BytesPerSector;

        #endregion
    }
}
