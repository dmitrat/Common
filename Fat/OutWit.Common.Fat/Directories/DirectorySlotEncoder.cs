using System;
using System.Buffers.Binary;
using OutWit.Common.Fat.Model;

namespace OutWit.Common.Fat.Directories
{
    /// <summary>
    /// Writes directory slots: short entries, their fields, and long-name slots.
    /// </summary>
    internal static class DirectorySlotEncoder
    {
        #region Constants

        private const byte LONG_NAME_TYPE = 0;

        private const ushort PADDING = 0xFFFF;

        #endregion

        #region Functions

        /// <summary>
        /// Writes a whole short entry.
        /// </summary>
        public static void WriteShort(Span<byte> slot, ReadOnlySpan<byte> shortName, byte caseFlags, FatAttributes attributes,
            uint firstCluster, uint size, DateTime created, DateTime modified)
        {
            slot[..DirectorySlot.SIZE].Clear();
            shortName[..DirectorySlot.NAME_LENGTH].CopyTo(slot);
            slot[DirectorySlot.ATTRIBUTES] = (byte)attributes;
            slot[DirectorySlot.CASE_FLAGS] = caseFlags;

            var (date, time) = FatTimestamp.Encode(created, out byte hundredths);
            slot[DirectorySlot.CREATED_HUNDREDTHS] = hundredths;
            WriteUInt16(slot, DirectorySlot.CREATED_TIME, time);
            WriteUInt16(slot, DirectorySlot.CREATED_DATE, date);

            SetModified(slot, modified);
            SetFirstCluster(slot, firstCluster);
            SetSize(slot, size);
        }

        /// <summary>
        /// Stores the first cluster, both halves. On FAT12 and FAT16 the high half of a
        /// valid cluster number is zero anyway.
        /// </summary>
        public static void SetFirstCluster(Span<byte> slot, uint cluster)
        {
            WriteUInt16(slot, DirectorySlot.FIRST_CLUSTER_HIGH, (ushort)(cluster >> 16));
            WriteUInt16(slot, DirectorySlot.FIRST_CLUSTER_LOW, (ushort)cluster);
        }

        public static void SetSize(Span<byte> slot, uint size)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(slot[DirectorySlot.FILE_SIZE..], size);
        }

        /// <summary>
        /// Stores the time of the last write, and its date as the last access.
        /// </summary>
        public static void SetModified(Span<byte> slot, DateTime modified)
        {
            var (date, time) = FatTimestamp.Encode(modified, out _);
            WriteUInt16(slot, DirectorySlot.MODIFIED_TIME, time);
            WriteUInt16(slot, DirectorySlot.MODIFIED_DATE, date);
            WriteUInt16(slot, DirectorySlot.ACCESSED_DATE, date);
        }

        /// <summary>
        /// Replaces the attribute bits a caller may set — read-only, hidden, system and
        /// archive — keeping the others.
        /// </summary>
        public static void SetAttributes(Span<byte> slot, FatAttributes attributes)
        {
            byte settable = (byte)DirectorySlot.SETTABLE_ATTRIBUTES;
            slot[DirectorySlot.ATTRIBUTES] = (byte)(slot[DirectorySlot.ATTRIBUTES] & ~settable | (byte)attributes & settable);
        }

        /// <summary>
        /// Replaces the name and case flags of a short entry, keeping everything else.
        /// </summary>
        public static void Rename(Span<byte> slot, ReadOnlySpan<byte> shortName, byte caseFlags)
        {
            shortName[..DirectorySlot.NAME_LENGTH].CopyTo(slot);
            slot[DirectorySlot.CASE_FLAGS] = caseFlags;
        }

        /// <summary>
        /// The number of long-name slots a name takes.
        /// </summary>
        public static int LongSlotCount(string name)
        {
            return (name.Length + DirectorySlot.LONG_NAME_CHARS_PER_SLOT - 1) / DirectorySlot.LONG_NAME_CHARS_PER_SLOT;
        }

        /// <summary>
        /// Writes one long-name slot.
        /// </summary>
        /// <param name="slot">The slot.</param>
        /// <param name="name">The whole long name.</param>
        /// <param name="ordinal">The slot's number, from 1 for the part that starts the name.</param>
        /// <param name="checksum">The checksum of the short name the slot belongs to.</param>
        public static void WriteLong(Span<byte> slot, string name, int ordinal, byte checksum)
        {
            slot[..DirectorySlot.SIZE].Clear();
            bool isLast = ordinal == LongSlotCount(name);
            slot[DirectorySlot.LONG_ORDINAL] = (byte)(isLast ? ordinal | DirectorySlot.LAST_LONG_SLOT : ordinal);
            slot[DirectorySlot.ATTRIBUTES] = DirectorySlot.LONG_NAME_ATTRIBUTES;
            slot[DirectorySlot.CASE_FLAGS] = LONG_NAME_TYPE;
            slot[DirectorySlot.LONG_CHECKSUM] = checksum;

            int start = (ordinal - 1) * DirectorySlot.LONG_NAME_CHARS_PER_SLOT;
            for (int i = 0; i < DirectorySlot.LONG_NAME_CHARS_PER_SLOT; i++)
            {
                int position = start + i;
                ushort unit = position < name.Length ? name[position] : position == name.Length ? (ushort)0 : PADDING;
                WriteUInt16(slot, DirectorySlot.LONG_NAME_OFFSETS[i], unit);
            }
        }

        private static void WriteUInt16(Span<byte> slot, int offset, ushort value)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(slot[offset..], value);
        }

        #endregion
    }
}
