using OutWit.Common.Fat.Boot;
using OutWit.Common.Fat.Tests.Utils;

namespace OutWit.Common.Fat.Tests.Boot
{
    [TestFixture]
    public class MbrPartitionEntryTests
    {
        #region Model Tests

        [Test]
        public void ModelContractHoldsTest()
        {
            ModelBaseContract.AssertHolds(new MbrPartitionEntry { Index = 1, IsActive = true, Type = 0x0C, FirstSector = 2048, SectorCount = 4096 });
        }

        [Test]
        public void ToStringShowsTypeInHexTest()
        {
            var entry = new MbrPartitionEntry { Index = 1, Type = 0x0C, FirstSector = 2048, SectorCount = 4096 };

            Assert.That(entry.ToString(), Is.EqualTo("Index: 1, Type: 0C, FirstSector: 2048, SectorCount: 4096"));
        }

        #endregion

        #region Type Tests

        [TestCase(0x05, true)]
        [TestCase(0x0F, true)]
        [TestCase(0x85, true)]
        [TestCase(0x0C, false)]
        [TestCase(0x07, false)]
        [TestCase(0xEE, false)]
        public void ExtendedTypesAreRecognisedTest(int type, bool isExtended)
        {
            var entry = new MbrPartitionEntry { Type = (byte)type };

            Assert.That(entry.IsExtended, Is.EqualTo(isExtended));
        }

        #endregion
    }
}
