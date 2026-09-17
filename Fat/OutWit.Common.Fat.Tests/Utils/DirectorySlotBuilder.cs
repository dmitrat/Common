using System.Buffers.Binary;
using OutWit.Common.Fat.Model;

namespace OutWit.Common.Fat.Tests.Utils
{
    /// <summary>
    /// Writes directory slots from the specification's offsets, with a checksum of its
    /// own, so parser tests do not check the parser against itself.
    /// </summary>
    internal static class DirectorySlotBuilder
    {
        #region Constants

        public const int SLOT = 32;

        private static readonly int[] CHAR_OFFSETS = { 1, 3, 5, 7, 9, 14, 16, 18, 20, 22, 24, 28, 30 };

        #endregion

        #region Functions

        /// <summary>
        /// A short entry. <paramref name="name"/> is the 11 on-disk characters, padded.
        /// </summary>
        public static byte[] Short(string name, FatAttributes attributes = FatAttributes.Archive, uint firstCluster = 0,
            uint size = 0, byte caseFlags = 0, ushort modifiedDate = 0x585D, ushort modifiedTime = 0x645C)
        {
            if (name.Length != 11)
                throw new ArgumentException("A short name has 11 characters.", nameof(name));

            var slot = new byte[SLOT];
            for (int i = 0; i < 11; i++)
                slot[i] = (byte)name[i];
            slot[11] = (byte)attributes;
            slot[12] = caseFlags;
            BinaryPrimitives.WriteUInt16LittleEndian(slot.AsSpan(20), (ushort)(firstCluster >> 16));
            BinaryPrimitives.WriteUInt16LittleEndian(slot.AsSpan(22), modifiedTime);
            BinaryPrimitives.WriteUInt16LittleEndian(slot.AsSpan(24), modifiedDate);
            BinaryPrimitives.WriteUInt16LittleEndian(slot.AsSpan(26), (ushort)firstCluster);
            BinaryPrimitives.WriteUInt32LittleEndian(slot.AsSpan(28), size);
            return slot;
        }

        /// <summary>
        /// The long-name slots for a short entry, in on-disk order: highest ordinal first.
        /// </summary>
        public static List<byte[]> Long(string longName, byte[] shortSlot)
        {
            byte checksum = Checksum(shortSlot);
            int count = (longName.Length + 12) / 13;
            var slots = new List<byte[]>();

            for (int ordinal = count; ordinal >= 1; ordinal--)
            {
                var slot = new byte[SLOT];
                slot[0] = (byte)(ordinal == count ? ordinal | 0x40 : ordinal);
                slot[11] = 0x0F;
                slot[13] = checksum;

                for (int i = 0; i < 13; i++)
                {
                    int position = (ordinal - 1) * 13 + i;
                    ushort unit = position < longName.Length ? longName[position] : position == longName.Length ? (ushort)0 : (ushort)0xFFFF;
                    BinaryPrimitives.WriteUInt16LittleEndian(slot.AsSpan(CHAR_OFFSETS[i]), unit);
                }

                slots.Add(slot);
            }

            return slots;
        }

        /// <summary>
        /// Concatenates slots into a directory cluster, padded with zeros.
        /// </summary>
        public static byte[] Directory(int length, IEnumerable<byte[]> slots)
        {
            var directory = new byte[length];
            int offset = 0;
            foreach (var slot in slots)
            {
                slot.CopyTo(directory, offset);
                offset += SLOT;
            }

            return directory;
        }

        /// <summary>
        /// The long-name checksum: rotate the sum right by one, then add the next byte.
        /// </summary>
        public static byte Checksum(ReadOnlySpan<byte> shortName)
        {
            int sum = 0;
            for (int i = 0; i < 11; i++)
                sum = ((sum >> 1) | ((sum & 1) << 7)) + shortName[i] & 0xFF;
            return (byte)sum;
        }

        #endregion
    }
}
