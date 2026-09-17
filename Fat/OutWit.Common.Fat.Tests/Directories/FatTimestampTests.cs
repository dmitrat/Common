using OutWit.Common.Fat.Directories;

namespace OutWit.Common.Fat.Tests.Directories
{
    [TestFixture]
    public class FatTimestampTests
    {
        #region Decoding Tests

        [Test]
        public void PackedDateAndTimeAreDecodedTest()
        {
            var value = FatTimestamp.Decode(Date(2024, 2, 29), Time(12, 34, 56));

            Assert.That(value, Is.EqualTo(new DateTime(2024, 2, 29, 12, 34, 56)));
            Assert.That(value!.Value.Kind, Is.EqualTo(DateTimeKind.Unspecified));
        }

        [TestCase(0, 0)]
        [TestCase(99, 990)]
        [TestCase(150, 1500)]
        [TestCase(199, 1990)]
        [TestCase(200, 0)]
        public void HundredthsAreAddedTest(int hundredths, int milliseconds)
        {
            var value = FatTimestamp.Decode(Date(2001, 1, 1), Time(0, 0, 10), (byte)hundredths);

            Assert.That(value, Is.EqualTo(new DateTime(2001, 1, 1, 0, 0, 10).AddMilliseconds(milliseconds)));
        }

        [Test]
        public void RangeLimitsAreDecodedTest()
        {
            Assert.That(FatTimestamp.Decode(Date(1980, 1, 1), 0), Is.EqualTo(new DateTime(1980, 1, 1)));
            Assert.That(FatTimestamp.Decode(Date(2107, 12, 31), Time(23, 59, 58)), Is.EqualTo(new DateTime(2107, 12, 31, 23, 59, 58)));
        }

        #endregion

        #region Invalid Value Tests

        [Test]
        public void ZeroDateIsAbsentTest()
        {
            Assert.That(FatTimestamp.Decode(0, Time(10, 0, 0)), Is.Null);
            Assert.That(FatTimestamp.DecodeDate(0), Is.Null);
        }

        [TestCase(2024, 0, 1)]
        [TestCase(2024, 13, 1)]
        [TestCase(2023, 2, 29)]
        [TestCase(2024, 4, 31)]
        [TestCase(2024, 1, 0)]
        public void ImpossibleDateIsAbsentTest(int year, int month, int day)
        {
            Assert.That(FatTimestamp.Decode(Date(year, month, day), 0), Is.Null);
        }

        [TestCase(24, 0, 0)]
        [TestCase(10, 60, 0)]
        [TestCase(10, 0, 60)]
        public void ImpossibleTimeLeavesTheDateTest(int hour, int minute, int second)
        {
            var time = (ushort)(hour << 11 | minute << 5 | second / 2);

            Assert.That(FatTimestamp.Decode(Date(2020, 5, 5), time), Is.EqualTo(new DateTime(2020, 5, 5)));
        }

        #endregion

        #region Encoding Tests

        [Test]
        public void EncodedTimeDecodesToItselfTest()
        {
            var value = new DateTime(2026, 9, 16, 21, 7, 43, 250);

            var (date, time) = FatTimestamp.Encode(value, out byte hundredths);

            Assert.That((date, time, hundredths), Is.EqualTo((Date(2026, 9, 16), Time(21, 7, 42), (byte)125)));
            Assert.That(FatTimestamp.Decode(date, time, hundredths), Is.EqualTo(new DateTime(2026, 9, 16, 21, 7, 43, 250)));
        }

        [Test]
        public void EncodingKeepsTwoSecondsWithoutHundredthsTest()
        {
            var (date, time) = FatTimestamp.Encode(new DateTime(2000, 2, 29, 23, 59, 59, 999), out _);

            Assert.That(FatTimestamp.Decode(date, time), Is.EqualTo(new DateTime(2000, 2, 29, 23, 59, 58)));
        }

        [Test]
        public void TimesOutsideTheRangeAreClampedTest()
        {
            var (early, earlyTime) = FatTimestamp.Encode(new DateTime(1970, 6, 1, 12, 0, 0), out byte earlyHundredths);
            var (late, lateTime) = FatTimestamp.Encode(new DateTime(2200, 1, 1), out byte lateHundredths);

            Assert.That(FatTimestamp.Decode(early, earlyTime, earlyHundredths), Is.EqualTo(new DateTime(1980, 1, 1)));
            Assert.That(FatTimestamp.Decode(late, lateTime, lateHundredths), Is.EqualTo(new DateTime(2107, 12, 31, 23, 59, 59, 990)));
        }

        #endregion

        #region Tools

        private static ushort Date(int year, int month, int day)
        {
            return (ushort)((year - 1980) << 9 | month << 5 | day);
        }

        private static ushort Time(int hour, int minute, int second)
        {
            return (ushort)(hour << 11 | minute << 5 | second / 2);
        }

        #endregion
    }
}
