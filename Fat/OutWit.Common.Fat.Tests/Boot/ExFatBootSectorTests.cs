using OutWit.Common.Fat.Boot;
using OutWit.Common.Fat.Tests.Utils;
using OutWit.Common.NUnit;

namespace OutWit.Common.Fat.Tests.Boot
{
    [TestFixture]
    public class ExFatBootSectorTests
    {
        #region Layout Tests

        [Test]
        public void ConsistentRegionIsDescribedTest()
        {
            var info = Describe(new ExFatBootRegionBuilder());

            Assert.That(info, Was.EqualTo(new FatVolumeInfo
            {
                Kind = FatKind.ExFat,
                SectorSize = 512,
                SectorsPerCluster = 8,
                TotalSectors = 32768,
                FatOffset = 2048,
                FatCount = 1,
                FatSectors = 30,
                ActiveFat = 0,
                IsFatMirrored = false,
                RootDirectorySector = null,
                RootDirectoryEntries = null,
                RootCluster = 5,
                ClusterHeapSector = 4096,
                ClusterCount = 3584,
                VolumeSerial = 0x0BADCAFE,
                FsInfoSector = null,
                BackupBootSector = 12
            }));
        }

        [Test]
        public void LargeSectorsAndSecondTableAreDescribedTest()
        {
            var builder = new ExFatBootRegionBuilder
            {
                SectorShift = 12,
                ClusterShift = 2,
                VolumeLength = 16384,
                FatOffset = 64,
                FatLength = 4,
                ClusterHeapOffset = 512,
                ClusterCount = 3968,
                RootCluster = 4,
                FatCount = 2,
                VolumeFlags = 0x0001
            };

            var info = Describe(builder, 4096);

            Assert.That(info.SectorSize, Is.EqualTo(4096));
            Assert.That(info.ClusterSize, Is.EqualTo(16384));
            Assert.That(info.FatCount, Is.EqualTo(2));
            Assert.That(info.ActiveFat, Is.EqualTo(1));
        }

        [Test]
        public void ChangedFlagsAndUsageKeepChecksumValidTest()
        {
            var builder = new ExFatBootRegionBuilder { VolumeFlags = 0x0006, PercentInUse = 42 };

            Assert.DoesNotThrow(() => Describe(builder));
        }

        #endregion

        #region Recognition Tests

        [Test]
        public void LegacyParameterBlockIsNotExFatTest()
        {
            var region = new ExFatBootRegionBuilder { MustBeZeroViolation = true }.Build();

            Assert.That(ExFatBootSector.TryParse(region.AsSpan(0, 512)), Is.Null);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(3)]
        [TestCase(7)]
        [TestCase(510)]
        public void DamagedMarkerIsNotExFatTest(int offset)
        {
            var region = new ExFatBootRegionBuilder().Build();
            region[offset] ^= 0x01;

            Assert.That(ExFatBootSector.TryParse(region.AsSpan(0, 512)), Is.Null);
        }

        [Test]
        public void FatBootSectorIsNotExFatTest()
        {
            var sector = FatBootSectorBuilder.ForClusters(5000, false).Build();

            Assert.That(ExFatBootSector.TryParse(sector), Is.Null);
        }

        #endregion

        #region Validation Tests

        [TestCase(100)]
        [TestCase(1000)]
        [TestCase(512 * 11 + 3)]
        public void AlteredRegionFailsChecksumTest(int offset)
        {
            var region = new ExFatBootRegionBuilder().Build();
            region[offset] ^= 0x10;

            var error = Assert.Throws<FatException>(() => ExFatBootSector.TryParse(region)!.Describe(region, long.MaxValue));

            Assert.That(error!.Kind, Is.EqualTo(FatErrorKind.Corrupt));
            Assert.That(error.Message, Does.Contain("checksum"));
        }

        [Test]
        public void SectorSizeMismatchIsReportedTest()
        {
            var boot = ExFatBootSector.TryParse(new ExFatBootRegionBuilder().Build())!;

            var error = Assert.Throws<FatException>(() => boot.CheckSectorSize(4096));

            Assert.That(error!.Kind, Is.EqualTo(FatErrorKind.SectorSizeMismatch));
        }

        [TestCase(8)]
        [TestCase(13)]
        public void SectorShiftOutOfRangeIsCorruptTest(int shift)
        {
            var region = new ExFatBootRegionBuilder().Build();
            region[108] = (byte)shift;

            var error = Assert.Throws<FatException>(() => ExFatBootSector.TryParse(region)!.CheckSectorSize(512));

            Assert.That(error!.Kind, Is.EqualTo(FatErrorKind.Corrupt));
        }

        [Test]
        public void MajorRevisionTwoIsUnsupportedTest()
        {
            var error = Assert.Throws<FatException>(() => Describe(new ExFatBootRegionBuilder { Revision = 0x0200 }));

            Assert.That(error!.Kind, Is.EqualTo(FatErrorKind.Unsupported));
        }

        [TestCase(nameof(ExFatBootRegionBuilder.ClusterShift), 17, "32 MiB")]
        [TestCase(nameof(ExFatBootRegionBuilder.FatCount), 0, "one or two")]
        [TestCase(nameof(ExFatBootRegionBuilder.FatCount), 3, "one or two")]
        [TestCase(nameof(ExFatBootRegionBuilder.VolumeLength), 2047, "1 MiB")]
        [TestCase(nameof(ExFatBootRegionBuilder.FatOffset), 23, "boot regions")]
        [TestCase(nameof(ExFatBootRegionBuilder.FatLength), 2049, "overlap")]
        [TestCase(nameof(ExFatBootRegionBuilder.ClusterHeapOffset), 32768, "past the end")]
        [TestCase(nameof(ExFatBootRegionBuilder.ClusterCount), 0, "clusters")]
        [TestCase(nameof(ExFatBootRegionBuilder.ClusterCount), 3585, "clusters")]
        [TestCase(nameof(ExFatBootRegionBuilder.FatLength), 28, "cannot hold")]
        [TestCase(nameof(ExFatBootRegionBuilder.RootCluster), 1, "root directory")]
        [TestCase(nameof(ExFatBootRegionBuilder.RootCluster), 3586, "root directory")]
        [TestCase(nameof(ExFatBootRegionBuilder.VolumeFlags), 1, "marked active")]
        public void InconsistentParameterIsCorruptTest(string field, long value, string fragment)
        {
            var builder = new ExFatBootRegionBuilder();
            var property = typeof(ExFatBootRegionBuilder).GetProperty(field)!;
            property.SetValue(builder, Convert.ChangeType(value, property.PropertyType));

            var error = Assert.Throws<FatException>(() => Describe(builder));

            Assert.That(error!.Kind, Is.EqualTo(FatErrorKind.Corrupt));
            Assert.That(error.Message, Does.Contain(fragment));
        }

        [Test]
        public void VolumeLargerThanDeviceIsCorruptTest()
        {
            var region = new ExFatBootRegionBuilder().Build();
            var boot = ExFatBootSector.TryParse(region)!;

            Assert.Throws<FatException>(() => boot.Describe(region, 32767));
            Assert.DoesNotThrow(() => boot.Describe(region, 32768));
        }

        #endregion

        #region Tools

        private static FatVolumeInfo Describe(ExFatBootRegionBuilder builder, int sectorSize = 512)
        {
            var region = builder.Build();
            var boot = ExFatBootSector.TryParse(region.AsSpan(0, sectorSize));
            Assert.That(boot, Is.Not.Null, "the builder made something that is not an exFAT boot sector");
            return boot!.Describe(region, long.MaxValue);
        }

        #endregion
    }
}
