using System;

namespace OutWit.Common.Fat.Directories
{
    /// <summary>
    /// FAT's packed dates and times.
    /// </summary>
    /// <remarks>
    /// A date is <c>year-1980 &lt;&lt; 9 | month &lt;&lt; 5 | day</c>, a time
    /// <c>hour &lt;&lt; 11 | minute &lt;&lt; 5 | second / 2</c>; the creation time adds
    /// hundredths of a second, 0 to 199. There is no time zone.
    /// </remarks>
    internal static class FatTimestamp
    {
        #region Constants

        private const int BASE_YEAR = 1980;

        private const int MAX_HUNDREDTHS = 199;

        private const int LAST_YEAR = 2107;

        #endregion

        #region Functions

        /// <summary>
        /// Decodes a date and time, or returns <c>null</c> for a zero or impossible date.
        /// An impossible time leaves the date at midnight.
        /// </summary>
        public static DateTime? Decode(ushort date, ushort time, byte hundredths = 0)
        {
            var day = DecodeDate(date);
            if (day == null)
                return null;

            int hour = time >> 11;
            int minute = (time >> 5) & 0x3F;
            int second = (time & 0x1F) * 2;
            if (hour > 23 || minute > 59 || second > 59)
                return day;

            var value = day.Value.Add(new TimeSpan(hour, minute, second));
            return hundredths <= MAX_HUNDREDTHS ? value.AddMilliseconds(hundredths * 10) : value;
        }

        /// <summary>
        /// Decodes a date alone, or returns <c>null</c> for a zero or impossible date.
        /// </summary>
        public static DateTime? DecodeDate(ushort date)
        {
            int year = BASE_YEAR + (date >> 9);
            int month = (date >> 5) & 0x0F;
            int day = date & 0x1F;

            if (month is < 1 or > 12 || day < 1 || day > DateTime.DaysInMonth(year, month))
                return null;

            return new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Unspecified);
        }

        /// <summary>
        /// Packs a time. Times before 1980 become its first moment and times after 2107 its
        /// last, the range FAT can hold; the seconds go down to the even second, and the
        /// remainder into <paramref name="hundredths"/>.
        /// </summary>
        public static (ushort Date, ushort Time) Encode(DateTime value, out byte hundredths)
        {
            if (value.Year < BASE_YEAR)
                value = new DateTime(BASE_YEAR, 1, 1);
            else if (value.Year > LAST_YEAR)
                value = new DateTime(LAST_YEAR, 12, 31, 23, 59, 59, 990);

            var date = (ushort)((value.Year - BASE_YEAR) << 9 | value.Month << 5 | value.Day);
            var time = (ushort)(value.Hour << 11 | value.Minute << 5 | value.Second / 2);
            hundredths = (byte)(value.Second % 2 * 100 + value.Millisecond / 10);
            return (date, time);
        }

        #endregion
    }
}
