using System;
using System.IO;

namespace OutWit.Common.Fat.ExFat
{
    /// <summary>
    /// The up-case table the exFAT specification recommends, in its compressed form, as
    /// Windows and mkfs.exfat write it to new volumes.
    /// </summary>
    /// <remarks>
    /// The 5836 bytes are a resource of this assembly; their checksum is the one the
    /// specification gives for the table.
    /// </remarks>
    internal static class ExFatUpcaseDefault
    {
        #region Constants

        public const uint CHECKSUM = 0xE619D30D;

        private const string RESOURCE = "OutWit.Common.Fat.ExFat.UpcaseTable.bin";

        private static readonly Lazy<byte[]> DATA = new(Load);

        #endregion

        #region Functions

        private static byte[] Load()
        {
            using var stream = typeof(ExFatUpcaseDefault).Assembly.GetManifestResourceStream(RESOURCE)
                               ?? throw new InvalidOperationException($"The resource {RESOURCE} is missing.");
            var data = new byte[stream.Length];
            stream.ReadExactly(data);

            uint checksum = ExFatChecksum.OfUpcaseTable(data);
            return checksum == CHECKSUM
                ? data
                : throw new InvalidDataException($"The built-in up-case table sums to 0x{checksum:X8}, not 0x{CHECKSUM:X8}.");
        }

        #endregion

        #region Properties

        /// <summary>
        /// The table's bytes; not to be changed.
        /// </summary>
        public static ReadOnlyMemory<byte> Data => DATA.Value;

        #endregion
    }
}
