using System.Buffers.Binary;

namespace OutWit.Common.Fat.Tests.Utils
{
    /// <summary>
    /// Writes an exFAT main boot region — sectors 0 to 11 with their checksum — from the
    /// specification's offsets.
    /// </summary>
    /// <remarks>
    /// The defaults describe a consistent 16 MiB volume with 4 KiB clusters.
    /// </remarks>
    internal sealed class ExFatBootRegionBuilder
    {
        #region Constants

        public const int REGION_SECTORS = 12;

        #endregion

        #region Functions

        public byte[] Build()
        {
            var region = new byte[REGION_SECTORS * BytesPerSector];
            var boot = region.AsSpan(0, BytesPerSector);

            new byte[] { 0xEB, 0x76, 0x90 }.CopyTo(boot);
            "EXFAT   "u8.CopyTo(boot[3..]);
            if (MustBeZeroViolation)
                boot[20] = 1;
            BinaryPrimitives.WriteUInt64LittleEndian(boot[72..], VolumeLength);
            BinaryPrimitives.WriteUInt32LittleEndian(boot[80..], FatOffset);
            BinaryPrimitives.WriteUInt32LittleEndian(boot[84..], FatLength);
            BinaryPrimitives.WriteUInt32LittleEndian(boot[88..], ClusterHeapOffset);
            BinaryPrimitives.WriteUInt32LittleEndian(boot[92..], ClusterCount);
            BinaryPrimitives.WriteUInt32LittleEndian(boot[96..], RootCluster);
            BinaryPrimitives.WriteUInt32LittleEndian(boot[100..], VolumeSerial);
            BinaryPrimitives.WriteUInt16LittleEndian(boot[104..], Revision);
            boot[108] = SectorShift;
            boot[109] = ClusterShift;
            boot[110] = FatCount;
            boot[111] = 0x80;
            boot[510] = 0x55;
            boot[511] = 0xAA;

            for (int sector = 1; sector <= 8; sector++)
            {
                region[(sector + 1) * BytesPerSector - 2] = 0x55;
                region[(sector + 1) * BytesPerSector - 1] = 0xAA;
            }

            uint checksum = Checksum(region.AsSpan(0, 11 * BytesPerSector));
            for (int offset = 11 * BytesPerSector; offset < region.Length; offset += 4)
                BinaryPrimitives.WriteUInt32LittleEndian(region.AsSpan(offset), checksum);

            // Written after the checksum: these fields are outside it.
            BinaryPrimitives.WriteUInt16LittleEndian(boot[106..], VolumeFlags);
            boot[112] = PercentInUse;
            return region;
        }

        private static uint Checksum(ReadOnlySpan<byte> data)
        {
            uint sum = 0;
            for (int i = 0; i < data.Length; i++)
            {
                if (i == 106 || i == 107 || i == 112)
                    continue;
                sum = (sum << 31 | sum >> 1) + data[i];
            }
            return sum;
        }

        #endregion

        #region Properties

        public byte SectorShift { get; set; } = 9;

        public byte ClusterShift { get; set; } = 3;

        public ulong VolumeLength { get; set; } = 32768;

        public uint FatOffset { get; set; } = 2048;

        public uint FatLength { get; set; } = 30;

        public uint ClusterHeapOffset { get; set; } = 4096;

        public uint ClusterCount { get; set; } = 3584;

        public uint RootCluster { get; set; } = 5;

        public uint VolumeSerial { get; set; } = 0x0BADCAFE;

        public ushort Revision { get; set; } = 0x0100;

        public ushort VolumeFlags { get; set; }

        public byte PercentInUse { get; set; } = 0xFF;

        public byte FatCount { get; set; } = 1;

        public bool MustBeZeroViolation { get; set; }

        public int BytesPerSector => 1 << SectorShift;

        #endregion
    }
}
