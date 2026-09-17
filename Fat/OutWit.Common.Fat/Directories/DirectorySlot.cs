namespace OutWit.Common.Fat.Directories
{
    /// <summary>
    /// The layout of a 32-byte directory entry, short and long.
    /// </summary>
    internal static class DirectorySlot
    {
        #region Constants

        public const int SIZE = 32;

        public const int NAME = 0;

        public const int NAME_LENGTH = 11;

        public const int ATTRIBUTES = 11;

        public const int CASE_FLAGS = 12;

        public const int CREATED_HUNDREDTHS = 13;

        public const int CREATED_TIME = 14;

        public const int CREATED_DATE = 16;

        public const int ACCESSED_DATE = 18;

        public const int FIRST_CLUSTER_HIGH = 20;

        public const int MODIFIED_TIME = 22;

        public const int MODIFIED_DATE = 24;

        public const int FIRST_CLUSTER_LOW = 26;

        public const int FILE_SIZE = 28;

        public const int LONG_ORDINAL = 0;

        public const int LONG_CHECKSUM = 13;

        public const byte END_OF_DIRECTORY = 0x00;

        public const byte DELETED = 0xE5;

        public const byte ESCAPED_E5 = 0x05;

        public const byte LAST_LONG_SLOT = 0x40;

        public const byte LONG_NAME_ATTRIBUTES = 0x0F;

        public const byte LONG_NAME_MASK = 0x3F;

        public const byte LOWER_CASE_BASE = 0x08;

        public const byte LOWER_CASE_EXTENSION = 0x10;

        public const int LONG_NAME_CHARS_PER_SLOT = 13;

        public const int MAX_LONG_NAME_SLOTS = 20;

        public const int MAX_LONG_NAME_LENGTH = 255;

        /// <summary>
        /// The attributes a caller may set on an entry; the others tell what the entry is.
        /// </summary>
        public const FatAttributes SETTABLE_ATTRIBUTES = FatAttributes.ReadOnly | FatAttributes.Hidden | FatAttributes.System | FatAttributes.Archive;

        /// <summary>
        /// Where the 13 UTF-16 units of a long-name slot lie.
        /// </summary>
        public static readonly int[] LONG_NAME_OFFSETS = { 1, 3, 5, 7, 9, 14, 16, 18, 20, 22, 24, 28, 30 };

        #endregion
    }
}
