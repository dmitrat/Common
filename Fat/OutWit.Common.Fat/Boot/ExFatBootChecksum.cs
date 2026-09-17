using System;
using System.Buffers.Binary;

namespace OutWit.Common.Fat.Boot
{
    /// <summary>
    /// The checksum over sectors 0 to 10 of an exFAT boot region, which sector 11 repeats.
    /// </summary>
    internal static class ExFatBootChecksum
    {
        #region Constants

        public const int CHECKED_SECTORS = 11;

        private const int VOLUME_FLAGS_OFFSET = 106;

        private const int PERCENT_IN_USE_OFFSET = 112;

        #endregion

        #region Functions

        /// <summary>
        /// Computes the checksum. The volume flags and the percentage in use are skipped:
        /// they change without the checksum being rewritten.
        /// </summary>
        /// <param name="sectors">Sectors 0 to 10.</param>
        public static uint Compute(ReadOnlySpan<byte> sectors)
        {
            uint checksum = 0;
            for (int i = 0; i < sectors.Length; i++)
            {
                if (i is VOLUME_FLAGS_OFFSET or VOLUME_FLAGS_OFFSET + 1 or PERCENT_IN_USE_OFFSET)
                    continue;

                checksum = ((checksum & 1) != 0 ? 0x80000000u : 0u) + (checksum >> 1) + sectors[i];
            }

            return checksum;
        }

        /// <summary>
        /// Tells whether sector 11 of a boot region holds the checksum of sectors 0 to 10
        /// in every one of its four-byte slots.
        /// </summary>
        /// <param name="region">Sectors 0 to 11.</param>
        /// <param name="sectorSize">The sector size in bytes.</param>
        public static bool Matches(ReadOnlySpan<byte> region, int sectorSize)
        {
            uint expected = Compute(region[..(CHECKED_SECTORS * sectorSize)]);
            var stored = region.Slice(CHECKED_SECTORS * sectorSize, sectorSize);

            for (int offset = 0; offset < sectorSize; offset += sizeof(uint))
            {
                if (BinaryPrimitives.ReadUInt32LittleEndian(stored[offset..]) != expected)
                    return false;
            }

            return true;
        }

        #endregion
    }
}
