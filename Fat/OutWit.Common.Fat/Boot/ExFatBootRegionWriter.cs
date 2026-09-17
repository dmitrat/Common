using System;
using System.Buffers.Binary;
using System.Numerics;

namespace OutWit.Common.Fat.Boot
{
    /// <summary>
    /// Writes an exFAT boot region: the main boot sector, eight extended boot sectors, the
    /// OEM parameters and reserved sectors, and the checksum sector.
    /// </summary>
    /// <remarks>
    /// The OEM parameters are left empty, which the specification allows. The same twelve
    /// sectors serve as the backup region.
    /// </remarks>
    internal static class ExFatBootRegionWriter
    {
        #region Constants

        public const int FAT_OFFSET = 24;

        private const int EXTENDED_BOOT_SECTORS = 8;

        private const ushort REVISION = 0x0100;

        private const byte FAT_COUNT = 1;

        private const byte DRIVE_SELECT = 0x80;

        private const uint EXTENDED_BOOT_SIGNATURE = 0xAA550000;

        private const byte HALT = 0xF4;

        private static ReadOnlySpan<byte> JUMP => new byte[] { 0xEB, 0x76, 0x90 };

        private static ReadOnlySpan<byte> NAME => "EXFAT   "u8;

        #endregion

        #region Functions

        /// <summary>
        /// A boot region.
        /// </summary>
        public static byte[] Build(int sectorSize, int sectorsPerCluster, long partitionOffset, long volumeLength, uint fatOffset,
            uint fatLength, uint clusterHeapOffset, uint clusterCount, uint rootCluster, uint serial, byte percentInUse)
        {
            var region = new byte[ExFatBootSector.BOOT_REGION_SECTORS * sectorSize];
            var boot = region.AsSpan(0, sectorSize);

            JUMP.CopyTo(boot);
            NAME.CopyTo(boot[3..]);
            BinaryPrimitives.WriteUInt64LittleEndian(boot[64..], (ulong)partitionOffset);
            BinaryPrimitives.WriteUInt64LittleEndian(boot[72..], (ulong)volumeLength);
            BinaryPrimitives.WriteUInt32LittleEndian(boot[80..], fatOffset);
            BinaryPrimitives.WriteUInt32LittleEndian(boot[84..], fatLength);
            BinaryPrimitives.WriteUInt32LittleEndian(boot[88..], clusterHeapOffset);
            BinaryPrimitives.WriteUInt32LittleEndian(boot[92..], clusterCount);
            BinaryPrimitives.WriteUInt32LittleEndian(boot[96..], rootCluster);
            BinaryPrimitives.WriteUInt32LittleEndian(boot[100..], serial);
            BinaryPrimitives.WriteUInt16LittleEndian(boot[104..], REVISION);
            boot[108] = (byte)BitOperations.Log2((uint)sectorSize);
            boot[109] = (byte)BitOperations.Log2((uint)sectorsPerCluster);
            boot[110] = FAT_COUNT;
            boot[111] = DRIVE_SELECT;
            boot[112] = percentInUse;
            boot[120..BootSignature.OFFSET].Fill(HALT);
            BootSignature.Write(boot);

            for (int sector = 1; sector <= EXTENDED_BOOT_SECTORS; sector++)
                BinaryPrimitives.WriteUInt32LittleEndian(region.AsSpan((sector + 1) * sectorSize - sizeof(uint)), EXTENDED_BOOT_SIGNATURE);

            uint checksum = ExFatBootChecksum.Compute(region.AsSpan(0, ExFatBootChecksum.CHECKED_SECTORS * sectorSize));
            var checksumSector = region.AsSpan(ExFatBootChecksum.CHECKED_SECTORS * sectorSize, sectorSize);
            for (int offset = 0; offset < sectorSize; offset += sizeof(uint))
                BinaryPrimitives.WriteUInt32LittleEndian(checksumSector[offset..], checksum);

            return region;
        }

        #endregion
    }
}
