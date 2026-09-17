using System;
using OutWit.Common.Fat.Directories;

namespace OutWit.Common.Fat.ExFat
{
    /// <summary>
    /// exFAT's timestamps: FAT's date and time in one 32-bit value, hundredths of a second
    /// for two of them, and an offset from UTC for each.
    /// </summary>
    /// <remarks>
    /// The offset byte holds a validity bit and a signed count of fifteen-minute steps. A
    /// time whose offset is valid is converted to UTC, as Windows and Linux read it; one
    /// whose offset is not is local time of an unknown zone, as on FAT, and is returned as
    /// it is stored.
    /// </remarks>
    internal static class ExFatTimestamp
    {
        #region Constants

        private const byte OFFSET_VALID = 0x80;

        private const byte OFFSET_BITS = 0x7F;

        private const byte OFFSET_SIGN = 0x40;

        private const int MINUTES_PER_STEP = 15;

        private const int FIRST_YEAR = 1980;

        private const int LAST_YEAR = 2107;

        #endregion

        #region Functions

        /// <summary>
        /// Decodes a timestamp, or returns <c>null</c> for a zero or impossible date.
        /// </summary>
        /// <param name="stamp">The date in the high 16 bits, the time in the low 16.</param>
        /// <param name="hundredths">Hundredths of a second past the even second, 0 to 199; zero where there are none.</param>
        /// <param name="utcOffset">The offset byte.</param>
        public static DateTime? Decode(uint stamp, byte hundredths, byte utcOffset)
        {
            var local = FatTimestamp.Decode((ushort)(stamp >> 16), (ushort)stamp, hundredths);
            if (local == null || (utcOffset & OFFSET_VALID) == 0)
                return local;

            var utc = local.Value.AddMinutes(-OffsetMinutes(utcOffset));
            return DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        }

        /// <summary>
        /// Encodes a moment as local time with its offset from UTC, valid.
        /// </summary>
        /// <remarks>
        /// The offset is recorded in whole fifteen-minute steps, towards zero, between
        /// -16:00 and +15:45; the local time recorded is the moment at that offset, so the
        /// moment itself is kept. Moments outside 1980 to 2107 are clamped, as on FAT.
        /// </remarks>
        /// <returns>The timestamp, the hundredths past its even second, and the offset byte.</returns>
        public static (uint Stamp, byte Hundredths, byte UtcOffset) Encode(DateTimeOffset moment)
        {
            int steps = Math.Clamp((int)(moment.Offset.Ticks / TimeSpan.TicksPerMinute / MINUTES_PER_STEP), -(OFFSET_SIGN), OFFSET_SIGN - 1);
            var utc = moment.UtcDateTime;
            var local = utc.Year > LAST_YEAR || utc.Year < FIRST_YEAR ? utc : utc.AddMinutes(steps * MINUTES_PER_STEP);
            var (date, time) = FatTimestamp.Encode(local, out byte hundredths);
            return ((uint)date << 16 | time, hundredths, (byte)(OFFSET_VALID | (steps & OFFSET_BITS)));
        }

        /// <summary>
        /// The offset from UTC in minutes that an offset byte records, whether or not it is valid.
        /// </summary>
        public static int OffsetMinutes(byte utcOffset)
        {
            int steps = utcOffset & OFFSET_BITS;
            if ((steps & OFFSET_SIGN) != 0)
                steps -= OFFSET_BITS + 1;

            return steps * MINUTES_PER_STEP;
        }

        #endregion
    }
}
