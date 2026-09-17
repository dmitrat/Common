using System;
using System.Buffers.Binary;
using OutWit.Common.Fat.Directories;
using OutWit.Common.Fat.Model;

namespace OutWit.Common.Fat.ExFat
{
    /// <summary>
    /// Makes and changes exFAT file entry sets.
    /// </summary>
    /// <remarks>
    /// A set is changed in memory and sealed with its checksum before it is written; the
    /// callers write it whole. The name hash is the caller's to give, since it depends on the
    /// volume's up-case table.
    /// </remarks>
    internal static class ExFatEntryEncoder
    {
        #region Constants

        private const int HEADER_ENTRIES = 2;

        #endregion

        #region Functions

        /// <summary>
        /// The file and stream entries of a new entry whose times are all <paramref name="now"/>;
        /// <see cref="Rename"/> gives it a name.
        /// </summary>
        public static byte[] Create(FatAttributes attributes, DateTimeOffset now, uint firstCluster, long dataLength, bool isContiguous)
        {
            var header = new byte[HEADER_ENTRIES * ExFatEntry.SIZE];
            header[ExFatEntry.TYPE] = ExFatEntry.FILE;
            BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(ExFatEntry.FILE_ATTRIBUTES), (ushort)attributes);
            var (stamp, hundredths, offset) = ExFatTimestamp.Encode(now);
            WriteTime(header, ExFatEntry.CREATE_TIMESTAMP, stamp, offset, ExFatEntry.CREATE_UTC_OFFSET);
            header[ExFatEntry.CREATE_10MS] = hundredths;
            SetModified(header, now);

            header[ExFatEntry.SIZE + ExFatEntry.TYPE] = ExFatEntry.STREAM_EXTENSION;
            SetStream(header, firstCluster, dataLength, dataLength, isContiguous);
            return header;
        }

        /// <summary>
        /// The set with another name: its file and stream entries, and any benign entries after
        /// its name, are kept.
        /// </summary>
        /// <param name="set">A whole set, or a file and a stream entry alone, with no name.</param>
        /// <param name="name">The name, already checked.</param>
        /// <param name="nameHash">The name's hash on this volume.</param>
        /// <exception cref="ArgumentException">The set would pass 255 secondary entries.</exception>
        public static byte[] Rename(ReadOnlySpan<byte> set, string name, ushort nameHash)
        {
            int oldNames = Math.Min(NameEntries(set[ExFatEntry.SIZE + ExFatEntry.NAME_LENGTH]), set.Length / ExFatEntry.SIZE - HEADER_ENTRIES);
            var rest = set[((HEADER_ENTRIES + oldNames) * ExFatEntry.SIZE)..];
            int names = NameEntries(name.Length);
            int secondaries = HEADER_ENTRIES - 1 + names + rest.Length / ExFatEntry.SIZE;
            if (secondaries > ExFatEntry.MAX_SECONDARIES)
                throw new ArgumentException($"The name '{name}' and the entry's other secondary entries would take {secondaries}; a set holds at most {ExFatEntry.MAX_SECONDARIES}.", nameof(name));

            var renamed = new byte[(HEADER_ENTRIES + names) * ExFatEntry.SIZE + rest.Length];

            set[..(HEADER_ENTRIES * ExFatEntry.SIZE)].CopyTo(renamed);
            renamed[ExFatEntry.SECONDARY_COUNT] = (byte)(renamed.Length / ExFatEntry.SIZE - 1);
            var stream = renamed.AsSpan(ExFatEntry.SIZE, ExFatEntry.SIZE);
            stream[ExFatEntry.NAME_LENGTH] = (byte)name.Length;
            BinaryPrimitives.WriteUInt16LittleEndian(stream[ExFatEntry.NAME_HASH..], nameHash);

            for (int i = 0; i < names; i++)
            {
                var entry = renamed.AsSpan((HEADER_ENTRIES + i) * ExFatEntry.SIZE, ExFatEntry.SIZE);
                entry[ExFatEntry.TYPE] = ExFatEntry.FILE_NAME;
                int from = i * ExFatEntry.NAME_CHARS_PER_ENTRY;
                int count = Math.Min(ExFatEntry.NAME_CHARS_PER_ENTRY, name.Length - from);
                for (int c = 0; c < count; c++)
                    BinaryPrimitives.WriteUInt16LittleEndian(entry[(ExFatEntry.NAME + 2 * c)..], name[from + c]);
            }

            rest.CopyTo(renamed.AsSpan((HEADER_ENTRIES + names) * ExFatEntry.SIZE));
            Seal(renamed);
            return renamed;
        }

        /// <summary>
        /// Records where an entry's data lies and how much of it is written.
        /// </summary>
        public static void SetStream(Span<byte> set, uint firstCluster, long dataLength, long validLength, bool isContiguous)
        {
            var stream = set.Slice(ExFatEntry.SIZE, ExFatEntry.SIZE);
            stream[ExFatEntry.SECONDARY_FLAGS] = (byte)(ExFatEntry.ALLOCATION_POSSIBLE | (isContiguous && firstCluster != 0 ? ExFatEntry.NO_FAT_CHAIN : 0));
            BinaryPrimitives.WriteUInt64LittleEndian(stream[ExFatEntry.VALID_DATA_LENGTH..], (ulong)validLength);
            BinaryPrimitives.WriteUInt32LittleEndian(stream[ExFatEntry.FIRST_CLUSTER..], firstCluster);
            BinaryPrimitives.WriteUInt64LittleEndian(stream[ExFatEntry.DATA_LENGTH..], (ulong)dataLength);
        }

        /// <summary>
        /// Records a write: the modification and access times.
        /// </summary>
        public static void SetModified(Span<byte> set, DateTimeOffset now)
        {
            var (stamp, hundredths, offset) = ExFatTimestamp.Encode(now);
            WriteTime(set, ExFatEntry.MODIFIED_TIMESTAMP, stamp, offset, ExFatEntry.MODIFIED_UTC_OFFSET);
            set[ExFatEntry.MODIFIED_10MS] = hundredths;
            WriteTime(set, ExFatEntry.ACCESSED_TIMESTAMP, stamp, offset, ExFatEntry.ACCESSED_UTC_OFFSET);
        }

        /// <summary>
        /// Sets attribute bits, keeping the others.
        /// </summary>
        public static void AddAttributes(Span<byte> set, FatAttributes attributes)
        {
            ushort value = BinaryPrimitives.ReadUInt16LittleEndian(set[ExFatEntry.FILE_ATTRIBUTES..]);
            BinaryPrimitives.WriteUInt16LittleEndian(set[ExFatEntry.FILE_ATTRIBUTES..], (ushort)(value | (ushort)attributes));
        }

        /// <summary>
        /// Replaces the attribute bits a caller may set — read-only, hidden, system and
        /// archive — keeping the others.
        /// </summary>
        public static void SetAttributes(Span<byte> set, FatAttributes attributes)
        {
            ushort value = BinaryPrimitives.ReadUInt16LittleEndian(set[ExFatEntry.FILE_ATTRIBUTES..]);
            value = (ushort)(value & ~(ushort)DirectorySlot.SETTABLE_ATTRIBUTES | (ushort)(attributes & DirectorySlot.SETTABLE_ATTRIBUTES));
            BinaryPrimitives.WriteUInt16LittleEndian(set[ExFatEntry.FILE_ATTRIBUTES..], value);
        }

        /// <summary>
        /// Makes a slot a volume label entry: in use, with the label's characters, or with
        /// none for no label.
        /// </summary>
        /// <param name="entry">The slot.</param>
        /// <param name="label">At most 11 UTF-16 units, or <c>null</c>.</param>
        public static void WriteLabel(Span<byte> entry, string? label)
        {
            entry[..ExFatEntry.SIZE].Clear();
            entry[ExFatEntry.TYPE] = ExFatEntry.VOLUME_LABEL;
            if (label == null)
                return;

            entry[ExFatEntry.LABEL_LENGTH] = (byte)label.Length;
            for (int i = 0; i < label.Length; i++)
                BinaryPrimitives.WriteUInt16LittleEndian(entry[(ExFatEntry.LABEL + 2 * i)..], label[i]);
        }

        /// <summary>
        /// Stores the set's checksum.
        /// </summary>
        public static void Seal(Span<byte> set)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(set[ExFatEntry.SET_CHECKSUM..], ExFatChecksum.OfEntrySet(set));
        }

        /// <summary>
        /// How many name entries a name of this many UTF-16 units takes.
        /// </summary>
        public static int NameEntries(int nameLength)
        {
            return (nameLength + ExFatEntry.NAME_CHARS_PER_ENTRY - 1) / ExFatEntry.NAME_CHARS_PER_ENTRY;
        }

        private static void WriteTime(Span<byte> set, int at, uint stamp, byte offset, int offsetAt)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(set[at..], stamp);
            set[offsetAt] = offset;
        }

        #endregion
    }
}
