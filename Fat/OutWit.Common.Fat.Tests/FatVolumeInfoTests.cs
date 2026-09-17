using OutWit.Common.Fat.Boot;
using OutWit.Common.Fat.Tests.Utils;
using OutWit.Common.NUnit;
using OutWit.Common.Utils;

namespace OutWit.Common.Fat.Tests
{
    [TestFixture]
    public class FatVolumeInfoTests
    {
        #region Model Tests

        [Test]
        public void ModelContractHoldsTest()
        {
            ModelBaseContract.AssertHolds(Sample());
        }

        [Test]
        public void WithReplacesOnePropertyTest()
        {
            var info = Sample();

            var changed = info.With(x => x.ClusterCount, 7u);

            Assert.That(changed.ClusterCount, Is.EqualTo(7u));
            Assert.That(info.ClusterCount, Is.EqualTo(3968u));
            Assert.That(changed, Was.Not.EqualTo(info));
            Assert.That(changed.With(x => x.ClusterCount, 3968u), Was.EqualTo(info));
        }

        [Test]
        public void WithRefusesComputedPropertyTest()
        {
            Assert.Throws<InvalidOperationException>(() => Sample().With(x => x.ClusterSize, 1));
        }

        [Test]
        public void OtherModelIsNeverEqualTest()
        {
            Assert.That(Sample().Is(new MbrPartitionEntry()), Is.False);
        }

        [Test]
        public void ClusterSizeFollowsGeometryTest()
        {
            Assert.That(Sample().ClusterSize, Is.EqualTo(16384));
        }

        [Test]
        public void ToStringNamesTheEssentialsTest()
        {
            string text = Sample().ToString();

            Assert.That(text, Does.Contain("Kind: ExFat"));
            Assert.That(text, Does.Contain("ClusterSize: 16384"));
            Assert.That(text, Does.Contain("VolumeSerial: EC4B0001"));
        }

        #endregion

        #region Tools

        private static FatVolumeInfo Sample()
        {
            return new FatVolumeInfo
            {
                Kind = FatKind.ExFat,
                SectorSize = 4096,
                SectorsPerCluster = 4,
                TotalSectors = 16384,
                FatOffset = 256,
                FatCount = 1,
                FatSectors = 4,
                ActiveFat = 0,
                IsFatMirrored = false,
                RootDirectorySector = 100,
                RootDirectoryEntries = 512,
                RootCluster = 4,
                ClusterHeapSector = 512,
                ClusterCount = 3968,
                VolumeSerial = 0xEC4B0001,
                FsInfoSector = 1,
                BackupBootSector = 12
            };
        }

        #endregion
    }
}
