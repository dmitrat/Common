using OutWit.Common.Fat.Model;
using OutWit.Common.Fat.Tests.Utils;
using OutWit.Common.NUnit;
using OutWit.Common.Utils;

namespace OutWit.Common.Fat.Tests.Model
{
    [TestFixture]
    public class FatVolumeLocationTests
    {
        #region Model Tests

        [Test]
        public void ModelContractHoldsTest()
        {
            ModelBaseContract.AssertHolds(Sample());
        }

        [Test]
        public void WholeDiskLocationsCompareWithoutPartitionTest()
        {
            var first = Sample().With(x => x.Partition, null);
            var second = Sample().With(x => x.Partition, null);

            Assert.That(first, Was.EqualTo(second));
            Assert.That(first, Was.Not.EqualTo(Sample()));
            Assert.That(Sample(), Was.Not.EqualTo(first));
            Assert.That(first.Clone().Partition, Is.Null);
        }

        [Test]
        public void ToStringIncludesVolumeTest()
        {
            Assert.That(Sample().ToString(), Does.Contain("FirstSector: 2048").And.Contain("Kind: Fat16"));
        }

        #endregion

        #region Tools

        private static FatVolumeLocation Sample()
        {
            return new FatVolumeLocation
            {
                Partition = new MbrPartitionEntry { Index = 1, IsActive = true, Type = 0x06, FirstSector = 2048, SectorCount = 129024 },
                FirstSector = 2048,
                SectorCount = 129024,
                Volume = new FatVolumeInfo { Kind = FatKind.Fat16, SectorSize = 512, SectorsPerCluster = 16, TotalSectors = 129024, ClusterCount = 8057 }
            };
        }

        #endregion
    }
}
