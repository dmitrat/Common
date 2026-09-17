using System.Buffers.Binary;
using OutWit.Common.Fat.Model;

namespace OutWit.Common.Fat.Tests.Utils
{
    /// <summary>
    /// Writes exFAT directory entries from the specification's offsets, with sums of its own.
    /// </summary>
    /// <remarks>
    /// The defaults describe a sealed set for an empty archive file named <c>file.txt</c>,
    /// last written on 29/02/2024 at 12:34:56 UTC.
    /// </remarks>
    internal sealed class ExFatSetBuilder
    {
        #region Constants

        public const int ENTRY = 32;

        /// <summary>29/02/2024 12:34:56 as a timestamp.</summary>
        public const uint STAMP = 0x585D645C;

        public const byte UTC = 0x80;

        #endregion

        #region Functions

        public byte[] Build()
        {
            int nameEntries = (Name.Length + 14) / 15;
            int secondaries = 1 + nameEntries + Extra.Count;
            var set = new byte[(1 + secondaries) * ENTRY];

            set[0] = 0x85;
            set[1] = (byte)(SecondaryCount ?? secondaries);
            BinaryPrimitives.WriteUInt16LittleEndian(set.AsSpan(4), (ushort)Attributes);
            BinaryPrimitives.WriteUInt32LittleEndian(set.AsSpan(8), Created);
            BinaryPrimitives.WriteUInt32LittleEndian(set.AsSpan(12), Modified);
            BinaryPrimitives.WriteUInt32LittleEndian(set.AsSpan(16), Accessed);
            set[20] = Created10Ms;
            set[21] = Modified10Ms;
            set[22] = CreatedOffset;
            set[23] = ModifiedOffset;
            set[24] = AccessedOffset;

            var stream = set.AsSpan(ENTRY, ENTRY);
            stream[0] = StreamType;
            stream[1] = (byte)(0x01 | (IsContiguous ? 0x02 : 0));
            stream[3] = (byte)(NameLength ?? Name.Length);
            BinaryPrimitives.WriteUInt16LittleEndian(stream[4..], NameHash);
            BinaryPrimitives.WriteUInt64LittleEndian(stream[8..], ValidLength ?? DataLength);
            BinaryPrimitives.WriteUInt32LittleEndian(stream[20..], FirstCluster);
            BinaryPrimitives.WriteUInt64LittleEndian(stream[24..], DataLength);

            for (int i = 0; i < nameEntries; i++)
            {
                var entry = set.AsSpan((2 + i) * ENTRY, ENTRY);
                entry[0] = 0xC1;
                for (int c = 0; c < 15 && i * 15 + c < Name.Length; c++)
                    BinaryPrimitives.WriteUInt16LittleEndian(entry[(2 + 2 * c)..], Name[i * 15 + c]);
            }

            for (int i = 0; i < Extra.Count; i++)
                Extra[i].CopyTo(set.AsSpan((2 + nameEntries + i) * ENTRY));

            if (IsSealed)
                BinaryPrimitives.WriteUInt16LittleEndian(set.AsSpan(2), SetChecksum(set));
            return set;
        }

        public static byte[] Label(string text)
        {
            var entry = new byte[ENTRY];
            entry[0] = 0x83;
            entry[1] = (byte)text.Length;
            for (int i = 0; i < text.Length; i++)
                BinaryPrimitives.WriteUInt16LittleEndian(entry.AsSpan(2 + 2 * i), text[i]);
            return entry;
        }

        public static byte[] Bitmap(uint firstCluster, ulong length, byte flags = 0)
        {
            var entry = new byte[ENTRY];
            entry[0] = 0x81;
            entry[1] = flags;
            BinaryPrimitives.WriteUInt32LittleEndian(entry.AsSpan(20), firstCluster);
            BinaryPrimitives.WriteUInt64LittleEndian(entry.AsSpan(24), length);
            return entry;
        }

        public static byte[] Upcase(uint firstCluster, byte[] table)
        {
            var entry = new byte[ENTRY];
            entry[0] = 0x82;
            BinaryPrimitives.WriteUInt32LittleEndian(entry.AsSpan(4), UpcaseChecksum(table));
            BinaryPrimitives.WriteUInt32LittleEndian(entry.AsSpan(20), firstCluster);
            BinaryPrimitives.WriteUInt64LittleEndian(entry.AsSpan(24), (ulong)table.Length);
            return entry;
        }

        /// <summary>
        /// An entry of a type and nothing else, such as a free or benign one.
        /// </summary>
        public static byte[] Entry(byte type, byte second = 0)
        {
            var entry = new byte[ENTRY];
            entry[0] = type;
            entry[1] = second;
            return entry;
        }

        /// <summary>
        /// An up-case table in the compressed form: characters below 'a' are their own upper
        /// case, then the given upper cases from 'a' on.
        /// </summary>
        public static byte[] UpcaseTable(string fromLowerA)
        {
            var table = new byte[4 + fromLowerA.Length * 2];
            BinaryPrimitives.WriteUInt16LittleEndian(table, 0xFFFF);
            BinaryPrimitives.WriteUInt16LittleEndian(table.AsSpan(2), 'a');
            for (int i = 0; i < fromLowerA.Length; i++)
                BinaryPrimitives.WriteUInt16LittleEndian(table.AsSpan(4 + 2 * i), fromLowerA[i]);
            return table;
        }

        public static byte[] Join(params byte[][] parts)
        {
            return parts.SelectMany(part => part).ToArray();
        }

        public static ushort SetChecksum(byte[] set)
        {
            ushort sum = 0;
            for (int i = 0; i < set.Length; i++)
            {
                if (i is 2 or 3)
                    continue;
                sum = (ushort)((sum >> 1) | (sum << 15));
                sum += set[i];
            }
            return sum;
        }

        public static uint UpcaseChecksum(byte[] table)
        {
            uint sum = 0;
            foreach (byte value in table)
                sum = (sum >> 1 | sum << 31) + value;
            return sum;
        }

        #endregion

        #region Properties

        public string Name { get; set; } = "file.txt";

        public int? NameLength { get; set; }

        public ushort NameHash { get; set; }

        public FatAttributes Attributes { get; set; } = FatAttributes.Archive;

        public uint FirstCluster { get; set; }

        public ulong DataLength { get; set; }

        public ulong? ValidLength { get; set; }

        public bool IsContiguous { get; set; }

        public byte StreamType { get; set; } = 0xC0;

        public int? SecondaryCount { get; set; }

        public List<byte[]> Extra { get; } = new();

        public bool IsSealed { get; set; } = true;

        public uint Created { get; set; } = STAMP;

        public uint Modified { get; set; } = STAMP;

        public uint Accessed { get; set; } = STAMP;

        public byte Created10Ms { get; set; }

        public byte Modified10Ms { get; set; }

        public byte CreatedOffset { get; set; } = UTC;

        public byte ModifiedOffset { get; set; } = UTC;

        public byte AccessedOffset { get; set; } = UTC;

        #endregion
    }
}
