using System;
using System.Buffers.Binary;
using OutWit.Common.Fat.Model;

namespace OutWit.Common.Fat.Boot
{
    /// <summary>
    /// Writes a master boot record with one partition.
    /// </summary>
    /// <remarks>
    /// The CHS fields are filled for a geometry of 255 heads and 63 sectors a track, as
    /// sfdisk and Windows fill them, and set to their largest value past cylinder 1023. The
    /// record carries no boot code.
    /// </remarks>
    internal static class MasterBootRecordWriter
    {
        #region Constants

        public const byte TYPE_FAT12 = 0x01;

        public const byte TYPE_FAT16 = 0x06;

        public const byte TYPE_FAT32_LBA = 0x0C;

        public const byte TYPE_EXFAT = 0x07;

        private const int HEADS = 255;

        private const int SECTORS_PER_TRACK = 63;

        private const int MAX_CYLINDER = 1023;

        #endregion

        #region Functions

        /// <summary>
        /// A record whose first slot holds the partition and whose others are empty.
        /// </summary>
        public static byte[] Build(int sectorSize, uint diskSignature, byte type, long firstSector, long sectorCount)
        {
            var sector = new byte[sectorSize];
            var span = sector.AsSpan();
            BinaryPrimitives.WriteUInt32LittleEndian(span[MasterBootRecord.DISK_SIGNATURE_OFFSET..], diskSignature);

            var entry = span.Slice(MasterBootRecord.TABLE_OFFSET, MasterBootRecord.ENTRY_SIZE);
            entry[0] = MasterBootRecord.STATUS_INACTIVE;
            WriteChs(entry[1..], firstSector);
            entry[4] = type;
            WriteChs(entry[5..], firstSector + sectorCount - 1);
            BinaryPrimitives.WriteUInt32LittleEndian(entry[8..], (uint)firstSector);
            BinaryPrimitives.WriteUInt32LittleEndian(entry[12..], (uint)sectorCount);

            BootSignature.Write(span);
            return sector;
        }

        /// <summary>
        /// The partition type for a kind of volume.
        /// </summary>
        public static byte TypeOf(FatKind kind)
        {
            return kind switch
            {
                FatKind.Fat12 => TYPE_FAT12,
                FatKind.Fat16 => TYPE_FAT16,
                FatKind.Fat32 => TYPE_FAT32_LBA,
                _ => TYPE_EXFAT
            };
        }

        private static void WriteChs(Span<byte> at, long sector)
        {
            long cylinder = sector / (HEADS * SECTORS_PER_TRACK);
            if (cylinder > MAX_CYLINDER)
            {
                at[0] = 0xFE;
                at[1] = 0xFF;
                at[2] = 0xFF;
                return;
            }

            at[0] = (byte)(sector / SECTORS_PER_TRACK % HEADS);
            at[1] = (byte)((sector % SECTORS_PER_TRACK + 1) | ((cylinder >> 2) & 0xC0));
            at[2] = (byte)cylinder;
        }

        #endregion
    }
}
