using System;
using System.Buffers.Binary;

namespace OutWit.Common.Fat.Directories
{
    /// <summary>
    /// Turns the slots of one directory, in order, into entries.
    /// </summary>
    internal sealed class DirectoryParser
    {
        #region Constants

        private const FatAttributes KIND_BITS = FatAttributes.Directory | FatAttributes.VolumeLabel;

        private const byte ATTRIBUTE_BITS = 0x3F;

        #endregion

        #region Fields

        private readonly LongNameAssembler m_longName = new();

        private readonly string m_directoryPath;

        private readonly bool m_hasHighCluster;

        private readonly uint m_directoryCluster;

        private long m_index = -1;

        #endregion

        #region Constructors

        /// <param name="directoryPath">The path of the directory, for the entries' paths.</param>
        /// <param name="hasHighCluster">Whether the high half of the first cluster counts; FAT32 only.</param>
        /// <param name="directoryCluster">The directory's first cluster; zero for the fixed root.</param>
        public DirectoryParser(string directoryPath, bool hasHighCluster, uint directoryCluster = 0)
        {
            m_directoryPath = directoryPath;
            m_hasHighCluster = hasHighCluster;
            m_directoryCluster = directoryCluster;
        }

        #endregion

        #region Functions

        /// <summary>
        /// Reads the next slot.
        /// </summary>
        /// <param name="slot">32 bytes.</param>
        /// <param name="item">The entry and its place, when the slot is one.</param>
        /// <param name="label">The label, when the slot is the volume label.</param>
        public DirectorySlotKind Parse(ReadOnlySpan<byte> slot, out DirectoryItem? item, out string? label)
        {
            item = null;
            label = null;
            m_index++;

            byte first = slot[DirectorySlot.NAME];
            if (first == DirectorySlot.END_OF_DIRECTORY)
            {
                m_longName.Reset();
                return DirectorySlotKind.End;
            }

            byte attributes = slot[DirectorySlot.ATTRIBUTES];
            if (first == DirectorySlot.DELETED)
                return Skip();

            if ((attributes & DirectorySlot.LONG_NAME_MASK) == DirectorySlot.LONG_NAME_ATTRIBUTES)
            {
                m_longName.Add(slot, m_index);
                return DirectorySlotKind.Skipped;
            }

            var name = slot[..DirectorySlot.NAME_LENGTH];
            var kind = (FatAttributes)attributes & KIND_BITS;

            if (kind == FatAttributes.VolumeLabel)
            {
                m_longName.Reset();
                label = CodePage437.Decode(name).TrimEnd(' ');
                return DirectorySlotKind.Label;
            }

            if (kind == KIND_BITS || first == ' ' || ShortName.IsDotEntry(name))
                return Skip();

            string? longName = m_longName.Complete(name, m_index, out long firstSlot);
            string displayName = longName ?? ShortName.Decode(name, slot[DirectorySlot.CASE_FLAGS]);
            bool isDirectory = kind == FatAttributes.Directory;

            var entry = new FatDirectoryEntry
            {
                Path = FatPath.Combine(m_directoryPath, displayName),
                Name = displayName,
                ShortName = ShortName.Decode(name),
                Attributes = (FatAttributes)(attributes & ATTRIBUTE_BITS),
                Length = isDirectory ? 0 : BinaryPrimitives.ReadUInt32LittleEndian(slot[DirectorySlot.FILE_SIZE..]),
                FirstCluster = ReadFirstCluster(slot),
                Created = FatTimestamp.Decode(ReadUInt16(slot, DirectorySlot.CREATED_DATE), ReadUInt16(slot, DirectorySlot.CREATED_TIME),
                    slot[DirectorySlot.CREATED_HUNDREDTHS]),
                Modified = FatTimestamp.Decode(ReadUInt16(slot, DirectorySlot.MODIFIED_DATE), ReadUInt16(slot, DirectorySlot.MODIFIED_TIME)),
                Accessed = FatTimestamp.DecodeDate(ReadUInt16(slot, DirectorySlot.ACCESSED_DATE))
            };

            item = new DirectoryItem(entry, m_directoryCluster, firstSlot, m_index);
            return DirectorySlotKind.Entry;
        }

        private DirectorySlotKind Skip()
        {
            m_longName.Reset();
            return DirectorySlotKind.Skipped;
        }

        private uint ReadFirstCluster(ReadOnlySpan<byte> slot)
        {
            uint low = ReadUInt16(slot, DirectorySlot.FIRST_CLUSTER_LOW);
            return m_hasHighCluster ? (uint)ReadUInt16(slot, DirectorySlot.FIRST_CLUSTER_HIGH) << 16 | low : low;
        }

        private static ushort ReadUInt16(ReadOnlySpan<byte> slot, int offset)
        {
            return BinaryPrimitives.ReadUInt16LittleEndian(slot[offset..]);
        }

        #endregion

        #region Properties

        /// <summary>
        /// How many long names were thrown away so far; each entry they belonged to, if any,
        /// was read by its short name.
        /// </summary>
        public int BrokenLongNames => m_longName.Discarded;

        #endregion
    }
}
