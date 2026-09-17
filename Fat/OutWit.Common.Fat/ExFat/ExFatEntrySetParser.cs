using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using OutWit.Common.Fat.Directories;

namespace OutWit.Common.Fat.ExFat
{
    /// <summary>
    /// Turns the entries of one exFAT directory, in order, into files, directories, the
    /// label and the root's critical entries.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A file is a set: a file entry, a stream extension, the name entries, and perhaps
    /// benign entries after them, as many as the file entry counts, with a checksum over all
    /// of them. A set that is cut short, whose checksum does not match, or whose stream or
    /// name entries are missing is corrupt; one with a critical secondary entry this library
    /// does not know is unsupported. Neither stops the parser: the problem is queued for
    /// <see cref="TakeProblem"/>, the set is passed over, and an entry that cut it short is
    /// read as what it is. The caller decides what a problem means.
    /// </para>
    /// <para>
    /// Free entries, secondary entries outside a set, and primary entries of other types are
    /// passed over, benign primaries together with their secondaries, as Linux does.
    /// </para>
    /// </remarks>
    internal sealed class ExFatEntrySetParser
    {
        #region Constants

        private const FatAttributes ATTRIBUTE_BITS = FatAttributes.ReadOnly | FatAttributes.Hidden | FatAttributes.System
                                                     | FatAttributes.Directory | FatAttributes.Archive;

        #endregion

        #region Fields

        private readonly string m_directoryPath;

        private readonly uint m_directoryCluster;

        private readonly DirectoryItem? m_directory;

        private readonly byte[] m_set = new byte[(ExFatEntry.MAX_SECONDARIES + 1) * ExFatEntry.SIZE];

        private readonly Queue<FatException> m_problems = new();

        private long m_index = -1;

        private long m_setStart;

        private int m_setLength;

        private int m_collected;

        private bool m_isFileSet;

        #endregion

        #region Constructors

        /// <param name="directoryPath">The path of the directory, for the entries' paths.</param>
        /// <param name="directoryCluster">The directory's first cluster.</param>
        /// <param name="directory">The directory, for the entries' <see cref="DirectoryItem.Parent"/>.</param>
        /// <param name="firstSlot">The position in the directory of the first entry to be read.</param>
        public ExFatEntrySetParser(string directoryPath, uint directoryCluster, DirectoryItem? directory = null, long firstSlot = 0)
        {
            m_directoryPath = directoryPath;
            m_directoryCluster = directoryCluster;
            m_directory = directory;
            m_index = firstSlot - 1;
        }

        #endregion

        #region Functions

        /// <summary>
        /// Reads the next entry.
        /// </summary>
        /// <param name="slot">32 bytes.</param>
        /// <param name="item">The file or directory, when the entry completes one.</param>
        /// <param name="label">The label, when the entry is a volume label with characters.</param>
        public DirectorySlotKind Parse(ReadOnlySpan<byte> slot, out DirectoryItem? item, out string? label)
        {
            item = null;
            label = null;
            m_index++;
            byte type = slot[ExFatEntry.TYPE];

            if (m_setLength > 0)
            {
                if (ExFatEntry.IsInUse(type) && ExFatEntry.IsSecondary(type))
                    return Continue(slot, out item);

                if (m_isFileSet)
                    Report(Corrupt($"ends after {m_collected} of its {m_setLength} entries"));
                m_setLength = 0;
            }

            if (type == ExFatEntry.END_OF_DIRECTORY)
                return DirectorySlotKind.End;
            if (!ExFatEntry.IsInUse(type) || ExFatEntry.IsSecondary(type))
                return DirectorySlotKind.Skipped;

            switch (type)
            {
                case ExFatEntry.FILE:
                    int secondaries = slot[ExFatEntry.SECONDARY_COUNT];
                    m_setStart = m_index;
                    if (secondaries < ExFatEntry.MIN_FILE_SECONDARIES)
                        Report(Corrupt($"counts {secondaries} secondary entries"));
                    else
                        Start(slot, secondaries, isFileSet: true);
                    return DirectorySlotKind.Skipped;

                case ExFatEntry.VOLUME_LABEL:
                    int length = Math.Min((int)slot[ExFatEntry.LABEL_LENGTH], ExFatEntry.MAX_LABEL_LENGTH);
                    if (length == 0)
                        return DirectorySlotKind.Skipped;
                    label = DecodeUnits(slot.Slice(ExFatEntry.LABEL, length * 2));
                    return DirectorySlotKind.Label;

                case ExFatEntry.ALLOCATION_BITMAP:
                case ExFatEntry.UPCASE_TABLE:
                    Critical = new ExFatRootEntry(type,
                        type == ExFatEntry.ALLOCATION_BITMAP ? slot[ExFatEntry.BITMAP_FLAGS] : (byte)0,
                        BinaryPrimitives.ReadUInt32LittleEndian(slot[ExFatEntry.FIRST_CLUSTER..]),
                        BinaryPrimitives.ReadUInt64LittleEndian(slot[ExFatEntry.DATA_LENGTH..]),
                        type == ExFatEntry.UPCASE_TABLE ? BinaryPrimitives.ReadUInt32LittleEndian(slot[ExFatEntry.UPCASE_CHECKSUM..]) : 0,
                        m_index);
                    return DirectorySlotKind.Critical;

                default:
                    if (ExFatEntry.IsBenign(type) && slot[ExFatEntry.SECONDARY_COUNT] > 0)
                    {
                        m_setStart = m_index;
                        Start(slot, slot[ExFatEntry.SECONDARY_COUNT], isFileSet: false);
                    }
                    return DirectorySlotKind.Skipped;
            }
        }

        /// <summary>
        /// Says the directory has ended: a file set still being read is cut short.
        /// </summary>
        public void Finish()
        {
            if (m_setLength > 0 && m_isFileSet)
                Report(Corrupt($"is cut short by the end of the directory after {m_collected} of its {m_setLength} entries"));
            m_setLength = 0;
        }

        /// <summary>
        /// The oldest problem not yet taken, or <c>null</c>.
        /// </summary>
        public FatException? TakeProblem()
        {
            return m_problems.Count > 0 ? m_problems.Dequeue() : null;
        }

        private void Start(ReadOnlySpan<byte> slot, int secondaries, bool isFileSet)
        {
            m_setLength = secondaries + 1;
            m_collected = 1;
            m_isFileSet = isFileSet;
            slot[..ExFatEntry.SIZE].CopyTo(m_set);
        }

        private DirectorySlotKind Continue(ReadOnlySpan<byte> slot, out DirectoryItem? item)
        {
            item = null;
            if (m_isFileSet)
                slot[..ExFatEntry.SIZE].CopyTo(m_set.AsSpan(m_collected * ExFatEntry.SIZE));
            if (++m_collected < m_setLength)
                return DirectorySlotKind.Skipped;

            int length = m_setLength;
            m_setLength = 0;
            if (!m_isFileSet)
                return DirectorySlotKind.Skipped;

            item = Describe(m_set.AsSpan(0, length * ExFatEntry.SIZE));
            return item != null ? DirectorySlotKind.Entry : DirectorySlotKind.Skipped;
        }

        /// <summary>
        /// The entry a whole set describes, or <c>null</c> with a problem reported.
        /// </summary>
        private DirectoryItem? Describe(ReadOnlySpan<byte> set)
        {
            if (ExFatChecksum.OfEntrySet(set) != BinaryPrimitives.ReadUInt16LittleEndian(set[ExFatEntry.SET_CHECKSUM..]))
                return Report(Corrupt("does not match its checksum"));

            var stream = set.Slice(ExFatEntry.SIZE, ExFatEntry.SIZE);
            if (stream[ExFatEntry.TYPE] != ExFatEntry.STREAM_EXTENSION)
                return Report(Corrupt("has no stream extension where it belongs"));

            int nameLength = stream[ExFatEntry.NAME_LENGTH];
            int nameEntries = (nameLength + ExFatEntry.NAME_CHARS_PER_ENTRY - 1) / ExFatEntry.NAME_CHARS_PER_ENTRY;
            int count = set.Length / ExFatEntry.SIZE;
            if (nameLength == 0 || 2 + nameEntries > count)
                return Report(Corrupt($"has no room for a name of {nameLength} characters"));

            var units = new char[nameEntries * ExFatEntry.NAME_CHARS_PER_ENTRY];
            for (int i = 0; i < count - 2; i++)
            {
                var entry = set.Slice((i + 2) * ExFatEntry.SIZE, ExFatEntry.SIZE);
                byte type = entry[ExFatEntry.TYPE];
                if (i < nameEntries != (type == ExFatEntry.FILE_NAME))
                    return Report(Corrupt($"has {nameEntries} name entries expected but entry {i + 2} is of type 0x{type:X2}"));
                if (i >= nameEntries && !ExFatEntry.IsBenign(type))
                    return Report(new FatException(FatErrorKind.Unsupported,
                        $"The entry set at slot {m_setStart} of {m_directoryPath} has a critical entry of type 0x{type:X2}, which this library does not know."));
                if (i < nameEntries)
                    DecodeUnits(entry.Slice(ExFatEntry.NAME, ExFatEntry.NAME_CHARS_PER_ENTRY * 2), units.AsSpan(i * ExFatEntry.NAME_CHARS_PER_ENTRY));
            }

            var name = units.AsSpan(0, nameLength);
            if (name is "." or "..")
                return Report(Corrupt($"is named '{name.ToString()}'"));

            ulong validLength = BinaryPrimitives.ReadUInt64LittleEndian(stream[ExFatEntry.VALID_DATA_LENGTH..]);
            ulong dataLength = BinaryPrimitives.ReadUInt64LittleEndian(stream[ExFatEntry.DATA_LENGTH..]);
            uint firstCluster = BinaryPrimitives.ReadUInt32LittleEndian(stream[ExFatEntry.FIRST_CLUSTER..]);
            if (dataLength > long.MaxValue || validLength > dataLength)
                return Report(Corrupt($"records {validLength} valid bytes of {dataLength}"));
            if (firstCluster == 0 && dataLength != 0)
                return Report(Corrupt($"records {dataLength} bytes and no cluster"));

            string safeName = SafeName(name);
            var attributes = (FatAttributes)BinaryPrimitives.ReadUInt16LittleEndian(set[ExFatEntry.FILE_ATTRIBUTES..]) & ATTRIBUTE_BITS;
            bool isDirectory = (attributes & FatAttributes.Directory) != 0;
            var entryModel = new FatDirectoryEntry
            {
                Path = FatPath.Combine(m_directoryPath, safeName),
                Name = safeName,
                Attributes = attributes,
                Length = isDirectory ? 0 : (long)dataLength,
                FirstCluster = firstCluster,
                Created = ExFatTimestamp.Decode(ReadUInt32(set, ExFatEntry.CREATE_TIMESTAMP), set[ExFatEntry.CREATE_10MS], set[ExFatEntry.CREATE_UTC_OFFSET]),
                Modified = ExFatTimestamp.Decode(ReadUInt32(set, ExFatEntry.MODIFIED_TIMESTAMP), set[ExFatEntry.MODIFIED_10MS], set[ExFatEntry.MODIFIED_UTC_OFFSET]),
                Accessed = ExFatTimestamp.Decode(ReadUInt32(set, ExFatEntry.ACCESSED_TIMESTAMP), 0, set[ExFatEntry.ACCESSED_UTC_OFFSET])
            };

            return new DirectoryItem(entryModel, m_directoryCluster, m_setStart, m_setStart + count - 1)
            {
                IsContiguous = (stream[ExFatEntry.SECONDARY_FLAGS] & ExFatEntry.NO_FAT_CHAIN) != 0,
                DataLength = (long)dataLength,
                ValidLength = (long)validLength,
                Parent = m_directory,
                NameHash = BinaryPrimitives.ReadUInt16LittleEndian(stream[ExFatEntry.NAME_HASH..]),
                StoredName = name.SequenceEqual(safeName) ? null : name.ToString()
            };
        }

        /// <summary>
        /// The name as a path component: separators and control characters become '_', as
        /// they do in FAT's short names.
        /// </summary>
        private static string SafeName(ReadOnlySpan<char> units)
        {
            var name = units.ToArray();
            for (int i = 0; i < name.Length; i++)
            {
                if (name[i] < ' ' || name[i] is '/' or '\\')
                    name[i] = '_';
            }

            return new string(name);
        }

        private DirectoryItem? Report(FatException problem)
        {
            m_problems.Enqueue(problem);
            return null;
        }

        private FatException Corrupt(string problem)
        {
            return new FatException(FatErrorKind.Corrupt, $"The entry set at slot {m_setStart} of {m_directoryPath} {problem}.");
        }

        private static string DecodeUnits(ReadOnlySpan<byte> bytes)
        {
            var units = new char[bytes.Length / 2];
            DecodeUnits(bytes, units);
            return new string(units);
        }

        private static void DecodeUnits(ReadOnlySpan<byte> bytes, Span<char> units)
        {
            for (int i = 0; i + 1 < bytes.Length; i += 2)
                units[i / 2] = (char)BinaryPrimitives.ReadUInt16LittleEndian(bytes[i..]);
        }

        private static uint ReadUInt32(ReadOnlySpan<byte> set, int offset)
        {
            return BinaryPrimitives.ReadUInt32LittleEndian(set[offset..]);
        }

        #endregion

        #region Properties

        /// <summary>
        /// The last allocation bitmap or up-case table entry read.
        /// </summary>
        public ExFatRootEntry Critical { get; private set; }

        #endregion
    }
}
