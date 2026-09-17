using System;
using System.Buffers.Binary;
using System.Text;
using OutWit.Common.Fat.Model;

namespace OutWit.Common.Fat.Boot
{
    /// <summary>
    /// Writes the boot sector of a FAT12, FAT16 or FAT32 volume, and FAT32's FSInfo sector.
    /// </summary>
    /// <remarks>
    /// The fields are those <see cref="FatBootSector"/> reads, at the specification's
    /// offsets, with the values Windows writes where the specification leaves a choice: the
    /// OEM name MSWIN4.1, a fixed disk's media byte, 63 sectors a track and 255 heads, the
    /// extended signature 0x29 with the serial number, label and type. The boot code only
    /// hands the machine back to the firmware.
    /// </remarks>
    internal static class FatBootSectorWriter
    {
        #region Constants

        public const byte MEDIA_FIXED = 0xF8;

        public const int FAT32_RESERVED_SECTORS = 32;

        public const int FS_INFO_SECTOR = 1;

        public const int BACKUP_BOOT_SECTOR = 6;

        private const ushort SECTORS_PER_TRACK = 63;

        private const ushort HEADS = 255;

        private const byte DRIVE_NUMBER = 0x80;

        private const byte EXTENDED_SIGNATURE = 0x29;

        private const int FAT16_EXTENDED = 36;

        private const int FAT32_EXTENDED = 64;

        /// <summary>The cluster a new FAT32 root takes.</summary>
        public const uint FAT32_ROOT_CLUSTER = 2;

        private static ReadOnlySpan<byte> OEM_NAME => "MSWIN4.1"u8;

        /// <summary>INT 18h, then halt: "no bootable system here".</summary>
        private static ReadOnlySpan<byte> BOOT_CODE => new byte[] { 0xCD, 0x18, 0xFA, 0xF4, 0xEB, 0xFD };

        #endregion

        #region Functions

        /// <summary>
        /// A boot sector.
        /// </summary>
        /// <param name="kind">FAT12, FAT16 or FAT32.</param>
        /// <param name="sectorSize">The sector size.</param>
        /// <param name="sectorsPerCluster">The cluster size in sectors.</param>
        /// <param name="reservedSectors">The sectors before the first table.</param>
        /// <param name="fatCount">The number of tables.</param>
        /// <param name="rootEntries">The fixed root's entries; zero on FAT32.</param>
        /// <param name="totalSectors">The length of the volume.</param>
        /// <param name="fatSectors">The length of one table.</param>
        /// <param name="hiddenSectors">Where the volume starts on its disk.</param>
        /// <param name="serial">The serial number.</param>
        /// <param name="label">The 11 bytes of the label.</param>
        public static byte[] Build(FatKind kind, int sectorSize, int sectorsPerCluster, int reservedSectors, int fatCount, int rootEntries,
            long totalSectors, long fatSectors, long hiddenSectors, uint serial, ReadOnlySpan<byte> label)
        {
            bool isFat32 = kind == FatKind.Fat32;
            var sector = new byte[sectorSize];
            var span = sector.AsSpan();

            span[0] = 0xEB;
            span[1] = (byte)((isFat32 ? FAT32_EXTENDED + 26 : FAT16_EXTENDED + 26) - 2);
            span[2] = 0x90;
            OEM_NAME.CopyTo(span[3..]);
            WriteUInt16(span, 11, (ushort)sectorSize);
            span[13] = (byte)sectorsPerCluster;
            WriteUInt16(span, 14, (ushort)reservedSectors);
            span[16] = (byte)fatCount;
            WriteUInt16(span, 17, (ushort)rootEntries);
            if (!isFat32 && totalSectors <= ushort.MaxValue)
                WriteUInt16(span, 19, (ushort)totalSectors);
            else
                WriteUInt32(span, 32, (uint)totalSectors);
            span[21] = MEDIA_FIXED;
            if (!isFat32)
                WriteUInt16(span, 22, (ushort)fatSectors);
            WriteUInt16(span, 24, SECTORS_PER_TRACK);
            WriteUInt16(span, 26, HEADS);
            WriteUInt32(span, 28, (uint)hiddenSectors);

            int extended = FAT16_EXTENDED;
            if (isFat32)
            {
                WriteUInt32(span, 36, (uint)fatSectors);
                WriteUInt32(span, 44, FAT32_ROOT_CLUSTER);
                WriteUInt16(span, 48, FS_INFO_SECTOR);
                WriteUInt16(span, 50, BACKUP_BOOT_SECTOR);
                extended = FAT32_EXTENDED;
            }

            span[extended] = DRIVE_NUMBER;
            span[extended + 2] = EXTENDED_SIGNATURE;
            WriteUInt32(span, extended + 3, serial);
            label[..11].CopyTo(span[(extended + 7)..]);
            Encoding.ASCII.GetBytes(TypeName(kind), span[(extended + 18)..]);
            BOOT_CODE.CopyTo(span[(extended + 26)..]);
            BootSignature.Write(span);
            return sector;
        }

        /// <summary>
        /// Replaces the label a boot sector keeps, when it keeps one: when its extended
        /// signature is 0x29.
        /// </summary>
        /// <param name="sector">The boot sector or its backup.</param>
        /// <param name="kind">The volume's kind, which decides where the label lies.</param>
        /// <param name="label">The 11 bytes of the label.</param>
        /// <returns>Whether the sector changed.</returns>
        public static bool SetLabel(Span<byte> sector, FatKind kind, ReadOnlySpan<byte> label)
        {
            int extended = kind == FatKind.Fat32 ? FAT32_EXTENDED : FAT16_EXTENDED;
            var field = sector.Slice(extended + 7, 11);
            if (sector[extended + 2] != EXTENDED_SIGNATURE || field.SequenceEqual(label[..11]))
                return false;

            label[..11].CopyTo(field);
            return true;
        }

        /// <summary>
        /// An FSInfo sector.
        /// </summary>
        public static byte[] BuildFsInfo(int sectorSize, uint freeCount, uint nextFree)
        {
            var sector = new byte[sectorSize];
            WriteUInt32(sector, 0, 0x41615252);
            WriteUInt32(sector, 484, 0x61417272);
            FatFsInfo.Write(sector, freeCount, nextFree);
            WriteUInt32(sector, 508, 0xAA550000);
            return sector;
        }

        private static string TypeName(FatKind kind)
        {
            return kind switch
            {
                FatKind.Fat12 => "FAT12   ",
                FatKind.Fat16 => "FAT16   ",
                _ => "FAT32   "
            };
        }

        private static void WriteUInt16(Span<byte> span, int offset, ushort value)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(span[offset..], value);
        }

        private static void WriteUInt32(Span<byte> span, int offset, uint value)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(span[offset..], value);
        }

        #endregion
    }
}
