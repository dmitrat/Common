namespace OutWit.Common.Fat.ExFat
{
    /// <summary>
    /// The layout of a 32-byte exFAT directory entry, by the entry types this library reads.
    /// </summary>
    /// <remarks>
    /// The type byte packs <c>InUse</c> (bit 7), <c>TypeCategory</c> (bit 6: secondary),
    /// <c>TypeImportance</c> (bit 5: benign) and a five-bit code. An entry whose
    /// <c>InUse</c> bit is clear is free; a type of zero ends the directory.
    /// </remarks>
    internal static class ExFatEntry
    {
        #region Constants

        public const int SIZE = 32;

        public const int TYPE = 0;

        public const byte END_OF_DIRECTORY = 0x00;

        public const byte IN_USE = 0x80;

        public const byte SECONDARY = 0x40;

        public const byte BENIGN = 0x20;

        public const byte ALLOCATION_BITMAP = 0x81;

        public const byte UPCASE_TABLE = 0x82;

        public const byte VOLUME_LABEL = 0x83;

        public const byte FILE = 0x85;

        public const byte STREAM_EXTENSION = 0xC0;

        public const byte FILE_NAME = 0xC1;

        /// <summary>A primary entry's count of the secondary entries after it (generic template).</summary>
        public const int SECONDARY_COUNT = 1;

        /// <summary>A secondary entry's flags (generic template).</summary>
        public const int SECONDARY_FLAGS = 1;

        public const byte ALLOCATION_POSSIBLE = 0x01;

        public const byte NO_FAT_CHAIN = 0x02;

        public const int MIN_FILE_SECONDARIES = 2;

        /// <summary>The most secondary entries a primary counts: the field is a byte.</summary>
        public const int MAX_SECONDARIES = 255;

        public const int SET_CHECKSUM = 2;

        public const int FILE_ATTRIBUTES = 4;

        public const int CREATE_TIMESTAMP = 8;

        public const int MODIFIED_TIMESTAMP = 12;

        public const int ACCESSED_TIMESTAMP = 16;

        public const int CREATE_10MS = 20;

        public const int MODIFIED_10MS = 21;

        public const int CREATE_UTC_OFFSET = 22;

        public const int MODIFIED_UTC_OFFSET = 23;

        public const int ACCESSED_UTC_OFFSET = 24;

        public const int NAME_LENGTH = 3;

        public const int NAME_HASH = 4;

        public const int VALID_DATA_LENGTH = 8;

        /// <summary>First cluster, in the stream extension and in every entry of the critical primary template.</summary>
        public const int FIRST_CLUSTER = 20;

        /// <summary>Data length, in the stream extension and in every entry of the critical primary template.</summary>
        public const int DATA_LENGTH = 24;

        public const int NAME = 2;

        public const int NAME_CHARS_PER_ENTRY = 15;

        public const int MAX_NAME_LENGTH = 255;

        public const int BITMAP_FLAGS = 1;

        public const int UPCASE_CHECKSUM = 4;

        public const int LABEL_LENGTH = 1;

        public const int LABEL = 2;

        public const int MAX_LABEL_LENGTH = 11;

        #endregion

        #region Functions

        public static bool IsInUse(byte type)
        {
            return (type & IN_USE) != 0;
        }

        public static bool IsSecondary(byte type)
        {
            return (type & SECONDARY) != 0;
        }

        public static bool IsBenign(byte type)
        {
            return (type & BENIGN) != 0;
        }

        #endregion
    }
}
