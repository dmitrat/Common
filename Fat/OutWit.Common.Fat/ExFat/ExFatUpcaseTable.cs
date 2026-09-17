using System;
using System.Buffers.Binary;

namespace OutWit.Common.Fat.ExFat
{
    /// <summary>
    /// A volume's up-case table: how exFAT compares names without regard to case.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The table is a list of UTF-16 units, the upper case of character 0, 1, 2 and so on.
    /// A run of characters that are their own upper case may be written as 0xFFFF and the
    /// run's length; characters past the end of the table are their own upper case. That
    /// is how the specification describes it and how Linux reads it.
    /// </para>
    /// <para>
    /// Every UTF-16 unit is mapped on its own, surrogates included, so names outside the
    /// Basic Multilingual Plane compare as their units do.
    /// </para>
    /// </remarks>
    internal sealed class ExFatUpcaseTable
    {
        #region Constants

        private const int CHARACTERS = 0x10000;

        private const ushort IDENTITY_RUN = 0xFFFF;

        #endregion

        #region Fields

        private readonly char[] m_upper;

        #endregion

        #region Constructors

        private ExFatUpcaseTable(char[] upper)
        {
            m_upper = upper;
        }

        #endregion

        #region Functions

        /// <summary>
        /// Reads a table as its directory entry's data describes it.
        /// </summary>
        /// <param name="data">The table's bytes; a trailing odd byte is ignored.</param>
        public static ExFatUpcaseTable Parse(ReadOnlySpan<byte> data)
        {
            var upper = new char[CHARACTERS];
            for (int i = 0; i < CHARACTERS; i++)
                upper[i] = (char)i;

            int index = 0;
            bool isRunLength = false;
            for (int offset = 0; offset + 1 < data.Length && index < CHARACTERS; offset += 2)
            {
                ushort unit = BinaryPrimitives.ReadUInt16LittleEndian(data[offset..]);
                if (isRunLength)
                {
                    index += unit;
                    isRunLength = false;
                }
                else if (unit == index)
                {
                    index++;
                }
                else if (unit == IDENTITY_RUN)
                {
                    isRunLength = true;
                }
                else
                {
                    upper[index++] = (char)unit;
                }
            }

            return new ExFatUpcaseTable(upper);
        }

        /// <summary>
        /// The upper case of a UTF-16 unit.
        /// </summary>
        public char ToUpper(char unit)
        {
            return m_upper[unit];
        }

        /// <summary>
        /// Whether two names are the same name on this volume.
        /// </summary>
        public bool NamesEqual(ReadOnlySpan<char> left, ReadOnlySpan<char> right)
        {
            if (left.Length != right.Length)
                return false;

            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i] && m_upper[left[i]] != m_upper[right[i]])
                    return false;
            }

            return true;
        }

        /// <summary>
        /// The hash a stream extension records for a name.
        /// </summary>
        public ushort HashOf(ReadOnlySpan<char> name)
        {
            Span<char> upcased = name.Length <= ExFatEntry.MAX_NAME_LENGTH ? stackalloc char[name.Length] : new char[name.Length];
            for (int i = 0; i < name.Length; i++)
                upcased[i] = m_upper[name[i]];

            return ExFatChecksum.OfName(upcased);
        }

        #endregion
    }
}
