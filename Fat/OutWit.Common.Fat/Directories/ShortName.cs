using System;

namespace OutWit.Common.Fat.Directories
{
    /// <summary>
    /// The 11-byte 8.3 name of a directory entry.
    /// </summary>
    internal static class ShortName
    {
        #region Constants

        private const int BASE_LENGTH = 8;

        private const byte PADDING = 0x20;

        private const char REPLACEMENT = '_';

        #endregion

        #region Functions

        /// <summary>
        /// The name as stored, with the dot put back: <c>README.TXT</c>, <c>MAKEFILE</c>.
        /// A leading 0x05 stands for 0xE5. Separators and control characters, which a valid
        /// short name never holds, become '_' so the name is always a path component.
        /// </summary>
        public static string Decode(ReadOnlySpan<byte> name)
        {
            return Decode(name, 0);
        }

        /// <summary>
        /// The name as displayed: the base or the extension lower-cased where the entry's
        /// case flags say so, as Windows NT and Linux do.
        /// </summary>
        public static string Decode(ReadOnlySpan<byte> name, byte caseFlags)
        {
            string stem = Part(name[..BASE_LENGTH], unescape: true);
            string extension = Part(name[BASE_LENGTH..DirectorySlot.NAME_LENGTH], unescape: false);

            if ((caseFlags & DirectorySlot.LOWER_CASE_BASE) != 0)
                stem = stem.ToLowerInvariant();
            if ((caseFlags & DirectorySlot.LOWER_CASE_EXTENSION) != 0)
                extension = extension.ToLowerInvariant();

            return extension.Length == 0 ? stem : stem + "." + extension;
        }

        /// <summary>
        /// The checksum a long name carries to tie it to its short name.
        /// </summary>
        public static byte Checksum(ReadOnlySpan<byte> name)
        {
            byte sum = 0;
            for (int i = 0; i < DirectorySlot.NAME_LENGTH; i++)
                sum = (byte)(((sum & 1) << 7) + (sum >> 1) + name[i]);
            return sum;
        }

        /// <summary>
        /// Whether the name is the <c>.</c> or <c>..</c> entry of a subdirectory.
        /// </summary>
        public static bool IsDotEntry(ReadOnlySpan<byte> name)
        {
            int dots = name[0] == '.' ? (name[1] == '.' ? 2 : 1) : 0;
            return dots > 0 && name[dots..DirectorySlot.NAME_LENGTH].IndexOfAnyExcept(PADDING) < 0;
        }

        private static string Part(ReadOnlySpan<byte> bytes, bool unescape)
        {
            int length = bytes.LastIndexOfAnyExcept(PADDING) + 1;
            Span<char> chars = stackalloc char[length];
            for (int i = 0; i < length; i++)
            {
                byte value = unescape && i == 0 && bytes[0] == DirectorySlot.ESCAPED_E5 ? DirectorySlot.DELETED : bytes[i];
                char c = CodePage437.Decode(value);
                chars[i] = c < ' ' || c is '/' or '\\' ? REPLACEMENT : c;
            }

            return new string(chars);
        }

        #endregion
    }
}
