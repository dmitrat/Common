using System;

namespace OutWit.Common.Fat.Boot
{
    /// <summary>
    /// The 0x55 0xAA pair that ends a boot sector and a master boot record alike.
    /// </summary>
    internal static class BootSignature
    {
        #region Constants

        public const int OFFSET = 510;

        public const int MIN_SECTOR_SIZE = 512;

        private const byte FIRST = 0x55;

        private const byte SECOND = 0xAA;

        #endregion

        #region Functions

        public static bool IsPresent(ReadOnlySpan<byte> sector)
        {
            return sector.Length >= MIN_SECTOR_SIZE && sector[OFFSET] == FIRST && sector[OFFSET + 1] == SECOND;
        }

        public static void Write(Span<byte> sector)
        {
            sector[OFFSET] = FIRST;
            sector[OFFSET + 1] = SECOND;
        }

        #endregion
    }
}
