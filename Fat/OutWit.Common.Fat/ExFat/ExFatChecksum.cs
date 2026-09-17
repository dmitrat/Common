using System;

namespace OutWit.Common.Fat.ExFat
{
    /// <summary>
    /// The rotating sums exFAT keeps over entry sets, names and the up-case table.
    /// </summary>
    /// <remarks>
    /// Each is "rotate right by one, add the next byte", in 16 bits for entry sets and
    /// names and in 32 bits for the up-case table, as the specification gives them.
    /// </remarks>
    internal static class ExFatChecksum
    {
        #region Functions

        /// <summary>
        /// The checksum of a directory entry set, which skips the two bytes it is stored in.
        /// </summary>
        public static ushort OfEntrySet(ReadOnlySpan<byte> set)
        {
            ushort sum = 0;
            for (int i = 0; i < set.Length; i++)
            {
                if (i is ExFatEntry.SET_CHECKSUM or ExFatEntry.SET_CHECKSUM + 1)
                    continue;
                sum = (ushort)(((sum & 1) != 0 ? 0x8000 : 0) + (sum >> 1) + set[i]);
            }

            return sum;
        }

        /// <summary>
        /// The hash of a name, over the bytes of its up-cased UTF-16 units.
        /// </summary>
        public static ushort OfName(ReadOnlySpan<char> upcased)
        {
            ushort sum = 0;
            foreach (char unit in upcased)
            {
                sum = (ushort)(((sum & 1) != 0 ? 0x8000 : 0) + (sum >> 1) + (unit & 0xFF));
                sum = (ushort)(((sum & 1) != 0 ? 0x8000 : 0) + (sum >> 1) + (unit >> 8));
            }

            return sum;
        }

        /// <summary>
        /// The checksum of the up-case table, as the table's directory entry records it.
        /// </summary>
        public static uint OfUpcaseTable(ReadOnlySpan<byte> table)
        {
            uint sum = 0;
            foreach (byte value in table)
                sum = ((sum & 1) != 0 ? 0x80000000 : 0) + (sum >> 1) + value;

            return sum;
        }

        #endregion
    }
}
