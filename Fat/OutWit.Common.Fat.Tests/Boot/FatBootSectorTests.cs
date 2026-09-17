using OutWit.Common.Fat.Boot;
using OutWit.Common.Fat.Tests.Utils;
using OutWit.Common.NUnit;

namespace OutWit.Common.Fat.Tests.Boot
{
    [TestFixture]
    public class FatBootSectorTests
    {
        #region Kind Tests

        [TestCase(1, false, FatKind.Fat12)]
        [TestCase(4084, false, FatKind.Fat12)]
        [TestCase(4086, false, FatKind.Fat16)]
        [TestCase(65525, false, FatKind.Fat16)]
        [TestCase(65525, true, FatKind.Fat32)]
        [TestCase(1000, true, FatKind.Fat32)]
        [TestCase(0x0FFFFFF5, true, FatKind.Fat32)]
        public void KindFollowsLayoutAndClusterCountTest(long clusters, bool fat32Layout, FatKind kind)
        {
            var info = Describe(FatBootSectorBuilder.ForClusters(clusters, fat32Layout));

            Assert.That(info.Kind, Is.EqualTo(kind));
            Assert.That(info.ClusterCount, Is.EqualTo(clusters));
        }

        [TestCase(12, FatKind.Fat12)]
        [TestCase(16, FatKind.Fat16)]
        public void DisputedCountFollowsTableWidthTest(int entryBits, FatKind kind)
        {
            var info = Describe(FatBootSectorBuilder.ForClusters(4085, false, entryBits: entryBits));

            Assert.That(info.Kind, Is.EqualTo(kind));
            Assert.That(info.ClusterCount, Is.EqualTo(4085));
        }

        [Test]
        public void SixteenBitTableHoldsItsLargestCountTest()
        {
            var info = Describe(FatBootSectorBuilder.ForClusters(65525, false, entryBits: 16));

            Assert.That(info.Kind, Is.EqualTo(FatKind.Fat16));
            Assert.That(info.ClusterCount, Is.EqualTo(65525));
        }

        [Test]
        public void TooManyClustersForSixteenBitLayoutIsCorruptTest()
        {
            AssertCorrupt(FatBootSectorBuilder.ForClusters(65526, fat32Layout: false), "more than Fat16");
        }

        [Test]
        public void TooManyClustersForFat32IsCorruptTest()
        {
            AssertCorrupt(FatBootSectorBuilder.ForClusters(0x0FFFFFF6, fat32Layout: true), "more than Fat32");
        }

        #endregion

        #region Layout Tests

        [Test]
        public void Fat16LayoutIsDescribedTest()
        {
            var builder = FatBootSectorBuilder.ForClusters(10000, false, sectorsPerCluster: 4);
            builder.ReservedSectors = 4;
            builder.FatSectors = 40;
            builder.RootEntries = 224;
            builder.TotalSectors = builder.DataStart + 10000 * 4 + 3;

            var info = Describe(builder);

            Assert.That(info, Was.EqualTo(new FatVolumeInfo
            {
                Kind = FatKind.Fat16,
                SectorSize = 512,
                SectorsPerCluster = 4,
                TotalSectors = 98 + 40003,
                FatOffset = 4,
                FatCount = 2,
                FatSectors = 40,
                ActiveFat = 0,
                IsFatMirrored = true,
                RootDirectorySector = 84,
                RootDirectoryEntries = 224,
                RootCluster = null,
                ClusterHeapSector = 84 + 14,
                ClusterCount = 10000,
                VolumeSerial = 0xCAFEF00D,
                FsInfoSector = null,
                BackupBootSector = null
            }));
            Assert.That(info.ClusterSize, Is.EqualTo(2048));
        }

        [Test]
        public void Fat32LayoutIsDescribedTest()
        {
            var builder = FatBootSectorBuilder.ForClusters(70000, true, sectorsPerCluster: 8, bytesPerSector: 4096);
            builder.RootCluster = 5;

            var info = Describe(builder, 4096);

            Assert.That(info.Kind, Is.EqualTo(FatKind.Fat32));
            Assert.That(info.ClusterSize, Is.EqualTo(32768));
            Assert.That(info.RootCluster, Is.EqualTo(5));
            Assert.That(info.RootDirectorySector, Is.Null);
            Assert.That(info.RootDirectoryEntries, Is.Null);
            Assert.That(info.FsInfoSector, Is.EqualTo(1));
            Assert.That(info.BackupBootSector, Is.EqualTo(6));
            Assert.That(info.ClusterHeapSector, Is.EqualTo(32 + 2 * builder.FatSectors));
        }

        [Test]
        public void UnmirroredFat32NamesItsActiveTableTest()
        {
            var builder = FatBootSectorBuilder.ForClusters(70000, true);
            builder.ExtendedFlags = 0x0081;

            var info = Describe(builder);

            Assert.That(info.IsFatMirrored, Is.False);
            Assert.That(info.ActiveFat, Is.EqualTo(1));
        }

        [Test]
        public void MirroredFat32IgnoresActiveTableBitsTest()
        {
            var builder = FatBootSectorBuilder.ForClusters(70000, true);
            builder.ExtendedFlags = 0x0003;

            var info = Describe(builder);

            Assert.That(info.IsFatMirrored, Is.True);
            Assert.That(info.ActiveFat, Is.Zero);
        }

        [Test]
        public void AbsentFsInfoAndBackupAreNullTest()
        {
            var builder = FatBootSectorBuilder.ForClusters(70000, true);
            builder.FsInfoSector = 0xFFFF;
            builder.BackupBootSector = 0xFFFF;

            var info = Describe(builder);

            Assert.That(info.FsInfoSector, Is.Null);
            Assert.That(info.BackupBootSector, Is.Null);
        }

        [TestCase(32, 1)]
        [TestCase(1, null)]
        public void ZeroFsInfoMeansSectorOneTest(int reservedSectors, int? fsInfo)
        {
            var builder = FatBootSectorBuilder.ForClusters(70000, true);
            builder.ReservedSectors = reservedSectors;
            builder.TotalSectors = builder.DataStart + 70000;
            builder.FsInfoSector = 0;
            builder.BackupBootSector = 0;

            var info = Describe(builder);

            Assert.That(info.FsInfoSector, Is.EqualTo(fsInfo));
            Assert.That(info.BackupBootSector, Is.Null);
        }

        [Test]
        public void HiddenSectorsAreReadTest()
        {
            var builder = FatBootSectorBuilder.ForClusters(5000, false);
            builder.HiddenSectors = 0x12345678;

            Assert.That(FatBootSector.TryParse(builder.Build())!.HiddenSectors, Is.EqualTo(0x12345678));
        }

        [Test]
        public void MissingExtendedSignatureMeansNoSerialTest()
        {
            var builder = FatBootSectorBuilder.ForClusters(5000, false);
            builder.HasSerial = false;

            Assert.That(Describe(builder).VolumeSerial, Is.Zero);
        }

        [Test]
        public void SmallTotalFieldIsPreferredTest()
        {
            var builder = FatBootSectorBuilder.ForClusters(3000, false);
            var sector = builder.Build();
            BitConverter.GetBytes(0xFFFFFFFFu).CopyTo(sector, 32);

            var info = FatBootSector.TryParse(sector)!.Describe(512, long.MaxValue);

            Assert.That(info.TotalSectors, Is.EqualTo(builder.TotalSectors));
        }

        #endregion

        #region Recognition Tests

        [TestCase(nameof(FatBootSectorBuilder.HasSignature), 0)]
        [TestCase(nameof(FatBootSectorBuilder.Jump), 0x00)]
        [TestCase(nameof(FatBootSectorBuilder.Jump), 0x33)]
        [TestCase(nameof(FatBootSectorBuilder.BytesPerSector), 0)]
        [TestCase(nameof(FatBootSectorBuilder.BytesPerSector), 500)]
        [TestCase(nameof(FatBootSectorBuilder.SectorsPerCluster), 0)]
        [TestCase(nameof(FatBootSectorBuilder.SectorsPerCluster), 3)]
        [TestCase(nameof(FatBootSectorBuilder.ReservedSectors), 0)]
        [TestCase(nameof(FatBootSectorBuilder.FatCount), 0)]
        [TestCase(nameof(FatBootSectorBuilder.Media), 0x00)]
        [TestCase(nameof(FatBootSectorBuilder.Media), 0xF1)]
        [TestCase(nameof(FatBootSectorBuilder.TotalSectors), 0)]
        public void ImplausibleParametersAreNotFatTest(string field, int value)
        {
            var builder = FatBootSectorBuilder.ForClusters(5000, false);
            var property = typeof(FatBootSectorBuilder).GetProperty(field)!;
            property.SetValue(builder, property.PropertyType == typeof(bool) ? value != 0 : Convert.ChangeType(value, property.PropertyType));

            Assert.That(FatBootSector.TryParse(builder.Build()), Is.Null);
        }

        [TestCase(0xEB)]
        [TestCase(0xE9)]
        [TestCase(0xE8)]
        public void AcceptedJumpsAreFatTest(int jump)
        {
            var builder = FatBootSectorBuilder.ForClusters(5000, false);
            builder.Jump = (byte)jump;

            Assert.That(FatBootSector.TryParse(builder.Build()), Is.Not.Null);
        }

        [Test]
        public void ShortBufferIsNotFatTest()
        {
            Assert.That(FatBootSector.TryParse(new byte[100]), Is.Null);
        }

        #endregion

        #region Validation Tests

        [Test]
        public void SectorSizeMismatchIsReportedTest()
        {
            var sector = FatBootSectorBuilder.ForClusters(5000, false, bytesPerSector: 4096).Build();

            var error = Assert.Throws<FatException>(() => FatBootSector.TryParse(sector)!.Describe(512, long.MaxValue));

            Assert.That(error!.Kind, Is.EqualTo(FatErrorKind.SectorSizeMismatch));
        }

        [TestCase(false, 14, FatKind.Fat16, 14 * 512 * 8 / 16 - 2)]
        [TestCase(true, 14, FatKind.Fat32, 14 * 512 * 8 / 32 - 2)]
        public void ClustersPastTheTableAreLeftOutTest(bool fat32Layout, int fatSectors, FatKind kind, int addressable)
        {
            var builder = FatBootSectorBuilder.ForClusters(5000, fat32Layout);
            builder.FatSectors = fatSectors;
            builder.TotalSectors = builder.DataStart + 5000;

            var info = Describe(builder);

            Assert.That(info.Kind, Is.EqualTo(kind));
            Assert.That(info.ClusterCount, Is.EqualTo(addressable));
            Assert.That(info.TotalSectors, Is.EqualTo(builder.TotalSectors));
        }

        [Test]
        public void CappedCountStillLimitsFat16Test()
        {
            var builder = FatBootSectorBuilder.ForClusters(70000, false);
            builder.FatSectors = 257;
            builder.TotalSectors = builder.DataStart + 70000;

            AssertCorrupt(builder, "more than Fat16");
        }

        [Test]
        public void TwelveBitTableNeedsOnlyTwelveBitsTest()
        {
            var builder = FatBootSectorBuilder.ForClusters(4000, false);
            builder.FatSectors = (4002 * 3 / 2 + 511) / 512;
            builder.TotalSectors = builder.DataStart + 4000;

            Assert.That(Describe(builder).Kind, Is.EqualTo(FatKind.Fat12));
        }

        [Test]
        public void DataAreaPastEndIsCorruptTest()
        {
            var builder = FatBootSectorBuilder.ForClusters(10, false);
            builder.TotalSectors = builder.DataStart;

            AssertCorrupt(builder, "past the end");
        }

        [Test]
        public void MissingTableSizeIsCorruptTest()
        {
            var builder = FatBootSectorBuilder.ForClusters(70000, true);
            builder.FatSectors = 0;

            AssertCorrupt(builder, "no size");
        }

        [Test]
        public void Fat32WithFixedRootIsCorruptTest()
        {
            var builder = FatBootSectorBuilder.ForClusters(70000, true);
            builder.RootEntries = 512;

            AssertCorrupt(builder, "fixed root");
        }

        [Test]
        public void Fat16WithoutRootIsCorruptTest()
        {
            var builder = FatBootSectorBuilder.ForClusters(5000, false);
            builder.RootEntries = 0;

            AssertCorrupt(builder, "no root");
        }

        [TestCase(0u)]
        [TestCase(1u)]
        [TestCase(70002u)]
        public void RootClusterOutsideHeapIsCorruptTest(uint cluster)
        {
            var builder = FatBootSectorBuilder.ForClusters(70000, true);
            builder.RootCluster = cluster;

            AssertCorrupt(builder, "root directory starts");
        }

        [Test]
        public void ActiveTableBeyondCountIsCorruptTest()
        {
            var builder = FatBootSectorBuilder.ForClusters(70000, true);
            builder.ExtendedFlags = 0x0082;

            AssertCorrupt(builder, "marked active");
        }

        [Test]
        public void FsInfoOutsideReservedAreaIsCorruptTest()
        {
            var builder = FatBootSectorBuilder.ForClusters(70000, true);
            builder.FsInfoSector = 32;

            AssertCorrupt(builder, "FSInfo");
        }

        [Test]
        public void VolumeLargerThanDeviceIsCorruptTest()
        {
            var builder = FatBootSectorBuilder.ForClusters(5000, false);
            var boot = FatBootSector.TryParse(builder.Build())!;

            var error = Assert.Throws<FatException>(() => boot.Describe(512, builder.TotalSectors - 1));

            Assert.That(error!.Kind, Is.EqualTo(FatErrorKind.Corrupt));
            Assert.DoesNotThrow(() => boot.Describe(512, builder.TotalSectors));
        }

        [Test]
        public void Fat32VersionAboveZeroIsUnsupportedTest()
        {
            var builder = FatBootSectorBuilder.ForClusters(70000, true);
            builder.Version = 0x0100;

            var error = Assert.Throws<FatException>(() => Describe(builder));

            Assert.That(error!.Kind, Is.EqualTo(FatErrorKind.Unsupported));
        }

        #endregion

        #region Tools

        private static FatVolumeInfo Describe(FatBootSectorBuilder builder, int deviceSectorSize = 512)
        {
            var boot = FatBootSector.TryParse(builder.Build());
            Assert.That(boot, Is.Not.Null, "the builder made something that is not a FAT boot sector");
            return boot!.Describe(deviceSectorSize, long.MaxValue);
        }

        private static void AssertCorrupt(FatBootSectorBuilder builder, string fragment)
        {
            var error = Assert.Throws<FatException>(() => Describe(builder));
            Assert.That(error!.Kind, Is.EqualTo(FatErrorKind.Corrupt));
            Assert.That(error.Message, Does.Contain(fragment).IgnoreCase);
        }

        #endregion
    }
}
