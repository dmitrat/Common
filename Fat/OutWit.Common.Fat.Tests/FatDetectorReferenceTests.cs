using System.Security.Cryptography;
using OutWit.Common.Fat.Devices;
using OutWit.Common.Fat.Exceptions;
using OutWit.Common.Fat.Model;
using OutWit.Common.Fat.Tests.Utils;

namespace OutWit.Common.Fat.Tests
{
    /// <summary>
    /// Detection against images made and described by the Linux tools.
    /// </summary>
    [TestFixture]
    public class FatDetectorReferenceTests
    {
        #region Constants

        private static readonly IEnumerable<string> IMAGES = ReferenceImages.Names;

        private static readonly IEnumerable<string> PARTITIONED_IMAGES = ReferenceImages.PartitionedNames;

        private static readonly IEnumerable<string> WHOLE_DISK_IMAGES = ReferenceImages.WholeDiskNames;

        #endregion

        #region Manifest Tests

        [Test]
        public void ManifestCoversEveryKindAndLayoutTest()
        {
            var images = ReferenceImages.Manifest.Images;
            var volumes = images.SelectMany(image => image.Volumes.Select(volume => (image, volume))).ToList();

            Assert.That(ReferenceImages.Manifest.Format, Is.EqualTo(1));
            Assert.That(volumes.Select(v => v.volume.Kind).Distinct(), Is.EquivalentTo(Enum.GetValues<FatKind>()));
            foreach (var kind in Enum.GetValues<FatKind>())
            {
                var ofKind = volumes.Where(v => v.volume.Kind == kind).ToList();
                Assert.That(ofKind.Any(v => v.image.PartitionTable == "mbr"), Is.True, $"{kind} on an MBR partition");
                Assert.That(ofKind.Any(v => v.image.PartitionTable == "none"), Is.True, $"{kind} on a whole disk");
            }
            Assert.That(volumes.Any(v => v.volume.SectorSize == 4096 && v.image.PartitionTable == "mbr"), Is.True);
        }

        [TestCaseSource(nameof(IMAGES))]
        public async Task ImageMatchesManifestHashTest(string name)
        {
            var image = ReferenceImages.Get(name);
            await using var device = await ReferenceImages.OpenAsync(image);
            using var sha = SHA256.Create();
            await using var hashing = new CryptoStream(Stream.Null, sha, CryptoStreamMode.Write);

            await device.SaveAsync(hashing);
            await hashing.FlushFinalBlockAsync();

            Assert.That(device.SectorCount, Is.EqualTo(image.SectorCount));
            Assert.That(Convert.ToHexStringLower(sha.Hash!), Is.EqualTo(image.Sha256));
        }

        #endregion

        #region Detection Tests

        [TestCaseSource(nameof(IMAGES))]
        public async Task DiskLayoutMatchesOracleTest(string name)
        {
            var image = ReferenceImages.Get(name);
            await using var disk = await ReferenceImages.OpenAsync(image);

            var layout = await FatDetector.DetectDiskAsync(disk);

            var expectedTable = image.PartitionTable == "mbr" ? PartitionTableKind.Mbr : PartitionTableKind.None;
            Assert.That(layout.PartitionTable, Is.EqualTo(expectedTable));
            Assert.That(layout.DiskSignature, Is.EqualTo(image.DiskSignature));
            Assert.That(layout.Partitions.Select(p => (p.Index, p.Type, p.IsActive, p.FirstSector, p.SectorCount)),
                Is.EqualTo(image.Partitions.Select(p => (p.Index, p.Type, p.Active, p.FirstSector, p.SectorCount))));
            Assert.That(layout.Volumes, Has.Count.EqualTo(image.Volumes.Count));
            Assert.That(layout.Problems, Is.Empty);

            for (int i = 0; i < image.Volumes.Count; i++)
            {
                var expected = image.Volumes[i];
                var actual = layout.Volumes[i];
                Assert.That(actual.Partition?.Index, Is.EqualTo(expected.PartitionIndex));
                Assert.That(actual.FirstSector, Is.EqualTo(expected.FirstSector));
                Assert.That(actual.SectorCount, Is.EqualTo(expected.SectorCount));
                AssertVolume(actual.Volume, expected);
            }
        }

        [TestCaseSource(nameof(PARTITIONED_IMAGES))]
        public async Task PartitionWindowDescribesSameVolumeTest(string name)
        {
            var image = ReferenceImages.Get(name);
            await using var disk = await ReferenceImages.OpenAsync(image);
            var expected = image.Volumes.Single();

            await using var window = new BlockDevicePartition(disk, expected.FirstSector, expected.SectorCount);
            var volume = await FatDetector.DetectVolumeAsync(window);

            Assert.That(volume, Is.Not.Null);
            AssertVolume(volume!, expected);
            Assert.That(await FatDetector.DetectVolumeAsync(disk), Is.Null, "sector zero holds the partition table");
        }

        [TestCaseSource(nameof(IMAGES))]
        public async Task DetectionReadsOnlyBootSectorsTest(string name)
        {
            var image = ReferenceImages.Get(name);
            var probe = new BlockDeviceProbe(await ReferenceImages.OpenAsync(image));
            await using var cached = new BlockDeviceCached(probe, capacity: 64);

            await FatDetector.DetectDiskAsync(cached);

            int requestsPerVolume = image.Volumes[0].Kind == FatKind.ExFat ? 2 : 1;
            long sectorsPerVolume = image.Volumes[0].Kind == FatKind.ExFat ? 12 : 1;
            int tableRequests = image.Partitions.Count > 0 ? 1 : 0;
            Assert.That(probe.Of(BlockDeviceOperation.Read).Count(), Is.EqualTo(tableRequests + requestsPerVolume));
            Assert.That(probe.SectorsRead, Is.EqualTo(tableRequests + sectorsPerVolume));
            Assert.That(probe.Of(BlockDeviceOperation.Write), Is.Empty);
        }

        /// <remarks>
        /// Whole-disk images only: a partition table read in the wrong sector size simply
        /// points somewhere else, and nothing in it can tell.
        /// </remarks>
        [TestCaseSource(nameof(WHOLE_DISK_IMAGES))]
        public async Task WrongSectorSizeIsReportedTest(string name)
        {
            var image = ReferenceImages.Get(name);
            await using var device = await ReferenceImages.OpenAsync(image);
            var buffer = new MemoryStream();
            await device.SaveAsync(buffer);
            int otherSize = image.SectorSize == 512 ? 4096 : 512;
            buffer.Position = 0;
            await using var misread = new BlockDeviceStream(buffer, otherSize, isReadOnly: true);

            var error = Assert.ThrowsAsync<FatException>(async () => await FatDetector.DetectDiskAsync(misread));

            Assert.That(error!.Kind, Is.EqualTo(FatErrorKind.SectorSizeMismatch));
        }

        #endregion

        #region Tools

        private static void AssertVolume(FatVolumeInfo actual, ReferenceVolume expected)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(actual.Kind, Is.EqualTo(expected.Kind), "kind");
                Assert.That(actual.SectorSize, Is.EqualTo(expected.SectorSize), "sector size");
                Assert.That(actual.SectorsPerCluster, Is.EqualTo(expected.SectorsPerCluster), "sectors per cluster");
                Assert.That(actual.TotalSectors, Is.EqualTo(expected.TotalSectors), "total sectors");
                Assert.That(actual.FatOffset, Is.EqualTo(expected.FatOffset), "FAT offset");
                Assert.That(actual.FatSectors, Is.EqualTo(expected.FatSectors), "FAT sectors");
                Assert.That(actual.RootDirectorySector, Is.EqualTo(expected.RootDirectorySector), "root directory sector");
                Assert.That(actual.RootDirectoryEntries, Is.EqualTo(expected.RootDirectoryEntries), "root directory entries");
                Assert.That(actual.RootCluster, Is.EqualTo(expected.RootCluster), "root cluster");
                Assert.That(actual.ClusterHeapSector, Is.EqualTo(expected.ClusterHeapSector), "cluster heap");
                Assert.That(actual.ClusterCount, Is.EqualTo(expected.ClusterCount), "cluster count");
                Assert.That(actual.VolumeSerial, Is.EqualTo(expected.VolumeSerial), "serial");
                if (expected.FatCount.HasValue)
                    Assert.That(actual.FatCount, Is.EqualTo(expected.FatCount), "FAT count");
            }
        }

        #endregion
    }
}
