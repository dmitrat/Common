using OutWit.Common.Fat.ExFat;
using OutWit.Common.Fat.Tests.Utils;

namespace OutWit.Common.Fat.Tests.ExFat
{
    [TestFixture]
    public class ExFatTimestampTests
    {
        #region Decoding Tests

        [TestCase((byte)0x80, 12, 34)]
        [TestCase((byte)0x8C, 9, 34)]
        [TestCase((byte)0xEC, 17, 34)]
        [TestCase((byte)0x96, 7, 4)]
        [TestCase((byte)0xFF, 12, 49)]
        public void TimeWithOffsetIsGivenInUtcTest(byte offset, int hour, int minute)
        {
            var time = ExFatTimestamp.Decode(ExFatSetBuilder.STAMP, 0, offset);

            Assert.That(time, Is.EqualTo(new DateTime(2024, 2, 29, hour, minute, 56)));
            Assert.That(time!.Value.Kind, Is.EqualTo(DateTimeKind.Utc));
        }

        [TestCase((byte)0x00)]
        [TestCase((byte)0x0C)]
        public void TimeWithoutOffsetIsGivenAsStoredTest(byte offset)
        {
            var time = ExFatTimestamp.Decode(ExFatSetBuilder.STAMP, 0, offset);

            Assert.That(time, Is.EqualTo(new DateTime(2024, 2, 29, 12, 34, 56)));
            Assert.That(time!.Value.Kind, Is.EqualTo(DateTimeKind.Unspecified));
        }

        [TestCase((byte)0, 0)]
        [TestCase((byte)199, 1990)]
        [TestCase((byte)200, 0)]
        public void HundredthsAreAddedTest(byte hundredths, int milliseconds)
        {
            var time = ExFatTimestamp.Decode(ExFatSetBuilder.STAMP, hundredths, ExFatSetBuilder.UTC);

            Assert.That(time, Is.EqualTo(new DateTime(2024, 2, 29, 12, 34, 56).AddMilliseconds(milliseconds)));
        }

        [TestCase(0u)]
        [TestCase(0x00000000u | 0x645C)]
        [TestCase(0x585E645Cu)]
        public void ImpossibleDateIsNullTest(uint stamp)
        {
            Assert.That(ExFatTimestamp.Decode(stamp, 0, ExFatSetBuilder.UTC), Is.Null);
        }

        [Test]
        public void UtcBeforeTheFirstLocalMomentIsKeptTest()
        {
            var time = ExFatTimestamp.Decode(0x00210000, 0, 0xB8);

            Assert.That(time, Is.EqualTo(new DateTime(1979, 12, 31, 10, 0, 0)));
        }

        [TestCase((byte)0x80, 0)]
        [TestCase((byte)0x3F, 945)]
        [TestCase((byte)0x40, -960)]
        [TestCase((byte)0xFF, -15)]
        public void OffsetMinutesAreSignedTest(byte offset, int minutes)
        {
            Assert.That(ExFatTimestamp.OffsetMinutes(offset), Is.EqualTo(minutes));
        }

        #endregion

        #region Encoding Tests

        [TestCase(0, (byte)0x80)]
        [TestCase(180, (byte)0x8C)]
        [TestCase(-300, (byte)0xEC)]
        [TestCase(330, (byte)0x96)]
        [TestCase(-15, (byte)0xFF)]
        [TestCase(840, (byte)0xB8)]
        public void OffsetIsRecordedInQuartersTest(int minutes, byte expected)
        {
            var now = new DateTimeOffset(2024, 2, 29, 12, 34, 56, TimeSpan.FromMinutes(minutes));

            var (stamp, hundredths, offset) = ExFatTimestamp.Encode(now);

            Assert.That((stamp, hundredths, offset), Is.EqualTo((ExFatSetBuilder.STAMP, (byte)0, expected)));
            Assert.That(ExFatTimestamp.Decode(stamp, hundredths, offset), Is.EqualTo(now.UtcDateTime));
        }

        [Test]
        public void HundredthsCarryTheOddSecondTest()
        {
            var now = new DateTimeOffset(2024, 2, 29, 12, 34, 57, 990, TimeSpan.Zero);

            var (stamp, hundredths, _) = ExFatTimestamp.Encode(now);

            Assert.That((stamp, hundredths), Is.EqualTo((ExFatSetBuilder.STAMP, (byte)199)));
        }

        [Test]
        public void OffsetBetweenQuartersKeepsTheInstantTest()
        {
            var now = new DateTimeOffset(2024, 2, 29, 12, 34, 56, TimeSpan.FromMinutes(20));

            var (stamp, hundredths, offset) = ExFatTimestamp.Encode(now);

            Assert.That(offset, Is.EqualTo(0x81));
            Assert.That(ExFatTimestamp.Decode(stamp, hundredths, offset), Is.EqualTo(now.UtcDateTime));
        }

        [TestCase(1970)]
        [TestCase(2200)]
        public void TimesOutsideTheRangeAreClampedTest(int year)
        {
            var now = new DateTimeOffset(year, 6, 1, 0, 0, 0, TimeSpan.Zero);

            var (stamp, _, _) = ExFatTimestamp.Encode(now);

            var decoded = ExFatTimestamp.Decode(stamp, 0, 0)!.Value;
            Assert.That(decoded.Year, Is.EqualTo(year < 1980 ? 1980 : 2107));
        }

        #endregion
    }
}
