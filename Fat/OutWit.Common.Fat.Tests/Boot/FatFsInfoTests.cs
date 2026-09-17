using OutWit.Common.Fat.Boot;
using OutWit.Common.Fat.Tests.Utils;

namespace OutWit.Common.Fat.Tests.Boot
{
    [TestFixture]
    public class FatFsInfoTests
    {
        #region Read Tests

        [Test]
        public void HintsAreReadTest()
        {
            var sector = BlankVolume.FsInfo(512, 1234, 56);

            Assert.That(FatFsInfo.IsValid(sector), Is.True);
            Assert.That(FatFsInfo.ReadFreeCount(sector), Is.EqualTo(1234));
            Assert.That(FatFsInfo.ReadNextFree(sector), Is.EqualTo(56));
        }

        [TestCase(0)]
        [TestCase(484)]
        [TestCase(508)]
        [TestCase(511)]
        public void EachSignatureIsRequiredTest(int offset)
        {
            var sector = BlankVolume.FsInfo(512, 1, 2);
            sector[offset] ^= 0xFF;

            Assert.That(FatFsInfo.IsValid(sector), Is.False);
        }

        [Test]
        public void ShortBufferIsNotFsInfoTest()
        {
            Assert.That(FatFsInfo.IsValid(BlankVolume.FsInfo(512, 1, 2).AsSpan(0, 511)), Is.False);
        }

        #endregion

        #region Write Tests

        [Test]
        public void WriteChangesOnlyTheHintsTest()
        {
            var sector = BlankVolume.FsInfo(4096, 1, 2);
            new System.Random(7).NextBytes(sector.AsSpan(4, 480));
            new System.Random(8).NextBytes(sector.AsSpan(512));
            var before = sector.ToArray();

            FatFsInfo.Write(sector, 77, 88);

            Assert.That(FatFsInfo.ReadFreeCount(sector), Is.EqualTo(77));
            Assert.That(FatFsInfo.ReadNextFree(sector), Is.EqualTo(88));
            Assert.That(sector.AsSpan(0, 488).SequenceEqual(before.AsSpan(0, 488)), Is.True);
            Assert.That(sector.AsSpan(496).SequenceEqual(before.AsSpan(496)), Is.True);
        }

        #endregion
    }
}
