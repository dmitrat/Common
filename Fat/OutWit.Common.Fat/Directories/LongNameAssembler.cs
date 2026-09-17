using System;
using System.Buffers.Binary;

namespace OutWit.Common.Fat.Directories
{
    /// <summary>
    /// Collects the long-name slots in front of a short entry and checks that they
    /// belong to it.
    /// </summary>
    /// <remarks>
    /// Slots come last-first: the one flagged 0x40 carries the highest ordinal, and the
    /// ordinals must then count down to 1 with the same checksum throughout. The name is
    /// accepted only if the short entry that follows matches the checksum, and only if it
    /// can be a path component. Anything else — a gap, a stray slot, a deleted entry in
    /// between, a name like <c>..</c> — discards the collected name, and the short name is
    /// used instead, as Windows and Linux do.
    /// </remarks>
    internal sealed class LongNameAssembler
    {
        #region Constants

        private const ushort TERMINATOR = 0x0000;

        #endregion

        #region Fields

        private readonly char[] m_chars = new char[DirectorySlot.MAX_LONG_NAME_SLOTS * DirectorySlot.LONG_NAME_CHARS_PER_SLOT];

        private int m_slots;

        private int m_expected;

        private byte m_checksum;

        private long m_firstSlot;

        private bool m_isStray;

        #endregion

        #region Functions

        /// <summary>
        /// Takes a long-name slot.
        /// </summary>
        /// <param name="slot">The slot.</param>
        /// <param name="index">Its position in the directory.</param>
        public void Add(ReadOnlySpan<byte> slot, long index)
        {
            byte ordinal = slot[DirectorySlot.LONG_ORDINAL];
            int number = ordinal & ~DirectorySlot.LAST_LONG_SLOT;

            if ((ordinal & DirectorySlot.LAST_LONG_SLOT) != 0)
            {
                Reset();
                if (number is < 1 or > DirectorySlot.MAX_LONG_NAME_SLOTS)
                {
                    Discarded++;
                    m_isStray = true;
                    return;
                }

                m_slots = number;
                m_checksum = slot[DirectorySlot.LONG_CHECKSUM];
                m_firstSlot = index;
            }
            else if (number != m_expected || number == 0 || slot[DirectorySlot.LONG_CHECKSUM] != m_checksum)
            {
                bool isCounted = m_slots > 0 || m_isStray;
                Reset();
                Discarded += isCounted ? 0 : 1;
                m_isStray = true;
                return;
            }

            int start = (number - 1) * DirectorySlot.LONG_NAME_CHARS_PER_SLOT;
            for (int i = 0; i < DirectorySlot.LONG_NAME_CHARS_PER_SLOT; i++)
                m_chars[start + i] = (char)BinaryPrimitives.ReadUInt16LittleEndian(slot[DirectorySlot.LONG_NAME_OFFSETS[i]..]);

            m_expected = number - 1;
        }

        /// <summary>
        /// Ends the collection at a short entry.
        /// </summary>
        /// <param name="shortName">The short entry's 11 name bytes.</param>
        /// <param name="shortSlot">The short entry's position in the directory.</param>
        /// <param name="firstSlot">Receives where the name starts: the first long-name slot, or the short entry.</param>
        /// <returns>The long name if the slots were complete and belong to this entry; otherwise <c>null</c>.</returns>
        public string? Complete(ReadOnlySpan<byte> shortName, long shortSlot, out long firstSlot)
        {
            firstSlot = shortSlot;
            long start = m_firstSlot;
            bool hasSlots = m_slots > 0;
            bool isWhole = hasSlots && m_expected == 0 && ShortName.Checksum(shortName) == m_checksum;
            int length = m_slots * DirectorySlot.LONG_NAME_CHARS_PER_SLOT;
            Clear();

            if (!isWhole)
            {
                Discarded += hasSlots ? 1 : 0;
                return null;
            }

            int end = Array.IndexOf(m_chars, (char)TERMINATOR, 0, length);
            if (end >= 0)
                length = end;

            var name = length is 0 or > DirectorySlot.MAX_LONG_NAME_LENGTH ? null : new string(m_chars, 0, length);
            if (name == null || !IsUsable(name))
            {
                Discarded++;
                return null;
            }

            firstSlot = start;
            return name;
        }

        /// <summary>
        /// Drops whatever has been collected, counting it as discarded.
        /// </summary>
        public void Reset()
        {
            if (m_slots > 0)
                Discarded++;
            Clear();
        }

        private void Clear()
        {
            m_slots = 0;
            m_expected = 0;
            m_isStray = false;
        }

        /// <summary>
        /// Whether a long name can be a path component: not <c>.</c> or <c>..</c>, and free
        /// of separators and control characters. Linux refuses such names as well.
        /// </summary>
        private static bool IsUsable(string name)
        {
            if (name is "." or "..")
                return false;

            foreach (char c in name)
            {
                if (c < ' ' || c is '/' or '\\')
                    return false;
            }

            return true;
        }

        #endregion

        #region Properties

        /// <summary>
        /// How many long names were thrown away: cut short, stray, not matching their short
        /// entry, or not usable as a path component. The slots of one broken name count once.
        /// </summary>
        public int Discarded { get; private set; }

        #endregion
    }
}
