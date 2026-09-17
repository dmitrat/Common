using OutWit.Common.Fat.Devices;
using OutWit.Common.Fat.Exceptions;
using OutWit.Common.Fat.Model;
using OutWit.Common.Fat.Tests.Utils;

namespace OutWit.Common.Fat.Tests
{
    [TestFixture]
    public class FatDetectorTests
    {
        #region Whole Disk Tests

        [Test]
        public async Task BlankDiskHoldsNothingTest()
        {
            await using var disk = new BlockDeviceMemory(1000);

            var layout = await FatDetector.DetectDiskAsync(disk);

            Assert.That(layout.PartitionTable, Is.EqualTo(PartitionTableKind.None));
            Assert.That(layout.DiskSignature, Is.Null);
            Assert.That(layout.Partitions, Is.Empty);
            Assert.That(layout.Volumes, Is.Empty);
            Assert.That(await FatDetector.DetectVolumeAsync(disk), Is.Null);
        }

        [Test]
        public async Task EmptyDeviceHoldsNothingAndIsNotReadTest()
        {
            var probe = new BlockDeviceProbe(new BlockDeviceMemory(0));

            var layout = await FatDetector.DetectDiskAsync(probe);
            var volume = await FatDetector.DetectVolumeAsync(probe);

            Assert.That(layout.Volumes, Is.Empty);
            Assert.That(volume, Is.Null);
            Assert.That(probe.Requests, Is.Empty);
        }

        [Test]
        public async Task WholeDiskVolumeIsFoundTest()
        {
            var builder = FatBootSectorBuilder.ForClusters(5000, false);
            await using var disk = new BlockDeviceMemory(builder.TotalSectors + 10);
            await disk.WriteAsync(0, builder.Build());

            var layout = await FatDetector.DetectDiskAsync(disk);

            Assert.That(layout.PartitionTable, Is.EqualTo(PartitionTableKind.None));
            Assert.That(layout.Volumes, Has.Count.EqualTo(1));
            var location = layout.Volumes[0];
            Assert.That(location.Partition, Is.Null);
            Assert.That(location.FirstSector, Is.Zero);
            Assert.That(location.SectorCount, Is.EqualTo(builder.TotalSectors + 10));
            Assert.That(location.Volume.Kind, Is.EqualTo(FatKind.Fat16));
        }

        [Test]
        public async Task BootSectorWinsOverWellFormedTableTest()
        {
            var sector = FatBootSectorBuilder.ForClusters(5000, false).Build();
            var table = MbrBuilder.Build(0, new MbrSlot(0, 0x0C, 100, 100));
            table.AsSpan(440, 72).CopyTo(sector.AsSpan(440));
            await using var disk = new BlockDeviceMemory(10000);
            await disk.WriteAsync(0, sector);

            var layout = await FatDetector.DetectDiskAsync(disk);

            Assert.That(layout.PartitionTable, Is.EqualTo(PartitionTableKind.None));
            Assert.That(layout.Volumes.Single().FirstSector, Is.Zero);
        }

        [Test]
        public async Task CopiedParametersInFrontOfTableAreSkippedTest()
        {
            var fat = FatBootSectorBuilder.ForClusters(5000, false);
            fat.HiddenSectors = 63;
            var sector = fat.Build();
            MbrBuilder.Build(0x5EC7, new MbrSlot(0, 0x06, 63, (uint)fat.TotalSectors, 0x80)).AsSpan(440, 72).CopyTo(sector.AsSpan(440));
            await using var disk = new BlockDeviceMemory(63 + fat.TotalSectors);
            await disk.WriteAsync(0, sector);
            await disk.WriteAsync(63, fat.Build());

            var layout = await FatDetector.DetectDiskAsync(disk);

            Assert.That(layout.PartitionTable, Is.EqualTo(PartitionTableKind.Mbr));
            Assert.That(layout.DiskSignature, Is.EqualTo(0x5EC7));
            Assert.That(layout.Volumes.Single().FirstSector, Is.EqualTo(63));
        }

        [Test]
        public async Task HiddenSectorsWithoutMatchingSlotKeepWholeDiskTest()
        {
            var fat = FatBootSectorBuilder.ForClusters(5000, false);
            fat.HiddenSectors = 63;
            var sector = fat.Build();
            MbrBuilder.Build(0, new MbrSlot(0, 0x06, 64, 100)).AsSpan(440, 72).CopyTo(sector.AsSpan(440));
            await using var disk = new BlockDeviceMemory(fat.TotalSectors);
            await disk.WriteAsync(0, sector);

            var layout = await FatDetector.DetectDiskAsync(disk);

            Assert.That(layout.PartitionTable, Is.EqualTo(PartitionTableKind.None));
            Assert.That(layout.Volumes.Single().FirstSector, Is.Zero);
        }

        #endregion

        #region Partition Tests

        [Test]
        public async Task EveryPrimaryPartitionIsProbedTest()
        {
            var fat = FatBootSectorBuilder.ForClusters(3000, false);
            var exFat = new ExFatBootRegionBuilder();
            var sector = MbrBuilder.Build(0xA1B2C3D4,
                new MbrSlot(0, 0x83, 100, 50),
                new MbrSlot(1, 0x0C, 200, (uint)fat.TotalSectors, 0x80),
                new MbrSlot(2, 0x05, 5000, 100),
                new MbrSlot(3, 0x0B, 10000, (uint)exFat.VolumeLength));
            await using var disk = new BlockDeviceMemory(10000 + (long)exFat.VolumeLength);
            await disk.WriteAsync(0, sector);
            await disk.WriteAsync(200, fat.Build());
            await disk.WriteAsync(10000, exFat.Build());

            var layout = await FatDetector.DetectDiskAsync(disk);

            Assert.That(layout.PartitionTable, Is.EqualTo(PartitionTableKind.Mbr));
            Assert.That(layout.DiskSignature, Is.EqualTo(0xA1B2C3D4));
            Assert.That(layout.Partitions.Select(p => p.Index), Is.EqualTo(new[] { 0, 1, 2, 3 }));
            Assert.That(layout.Volumes.Select(v => (v.Partition!.Index, v.FirstSector, v.Volume.Kind)), Is.EqualTo(new[]
            {
                (1, 200L, FatKind.Fat12),
                (3, 10000L, FatKind.ExFat)
            }));
            Assert.That(layout.Volumes[0].Partition!.IsActive, Is.True);
            Assert.That(layout.Volumes[1].SectorCount, Is.EqualTo(exFat.VolumeLength));
        }

        [Test]
        public async Task GuidPartitionTableIsUnsupportedTest()
        {
            await using var disk = new BlockDeviceMemory(1000);
            await disk.WriteAsync(0, MbrBuilder.Build(0, new MbrSlot(0, 0xEE, 1, 999)));

            var error = Assert.ThrowsAsync<FatException>(async () => await FatDetector.DetectDiskAsync(disk));

            Assert.That(error!.Kind, Is.EqualTo(FatErrorKind.Unsupported));
        }

        [TestCase(900u, 101u)]
        [TestCase(0u, 10u)]
        public async Task PartitionOutsideDiskIsReportedTest(uint firstSector, uint sectorCount)
        {
            await using var disk = new BlockDeviceMemory(1000);
            await disk.WriteAsync(0, MbrBuilder.Build(0, new MbrSlot(2, 0x0C, firstSector, sectorCount)));

            var layout = await FatDetector.DetectDiskAsync(disk);

            Assert.That(layout.Volumes, Is.Empty);
            var problem = layout.Problems.Single();
            Assert.That(problem.Partition.Index, Is.EqualTo(2));
            Assert.That(problem.Kind, Is.EqualTo(FatErrorKind.Corrupt));
            Assert.That(problem.Message, Does.Contain("does not fit"));
        }

        [Test]
        public async Task ZeroLengthSlotIsListedButNotProbedTest()
        {
            var probe = new BlockDeviceProbe(new BlockDeviceMemory(1000));
            await probe.WriteAsync(0, MbrBuilder.Build(0, new MbrSlot(0, 0x07, 0, 0)));
            probe.Clear();

            var layout = await FatDetector.DetectDiskAsync(probe);

            Assert.That(layout.Partitions, Has.Count.EqualTo(1));
            Assert.That(layout.Volumes, Is.Empty);
            Assert.That(layout.Problems, Is.Empty);
            Assert.That(probe.SectorsRead, Is.EqualTo(1));
        }

        [Test]
        public async Task ExtendedPartitionIsListedButNotFollowedTest()
        {
            await using var disk = new BlockDeviceMemory(100);
            await disk.WriteAsync(0, MbrBuilder.Build(0, new MbrSlot(0, 0x0F, 50, 5000)));

            var layout = await FatDetector.DetectDiskAsync(disk);

            Assert.That(layout.Partitions.Single().IsExtended, Is.True);
            Assert.That(layout.Volumes, Is.Empty);
            Assert.That(layout.Problems, Is.Empty);
        }

        [Test]
        public async Task VolumeLargerThanItsPartitionIsReportedTest()
        {
            var fat = FatBootSectorBuilder.ForClusters(3000, false);
            await using var disk = new BlockDeviceMemory(10000);
            await disk.WriteAsync(0, MbrBuilder.Build(0, new MbrSlot(0, 0x01, 100, (uint)fat.TotalSectors - 1)));
            await disk.WriteAsync(100, fat.Build());

            var layout = await FatDetector.DetectDiskAsync(disk);

            Assert.That(layout.Volumes, Is.Empty);
            Assert.That(layout.Problems.Single().Kind, Is.EqualTo(FatErrorKind.Corrupt));
        }

        [Test]
        public async Task BadPartitionsDoNotHideGoodOneTest()
        {
            var good = FatBootSectorBuilder.ForClusters(3000, false);
            var broken = FatBootSectorBuilder.ForClusters(3000, false);
            broken.RootEntries = 0;
            await using var disk = new BlockDeviceMemory(40000);
            await disk.WriteAsync(0, MbrBuilder.Build(0,
                new MbrSlot(0, 0x07, 0, 0),
                new MbrSlot(1, 0x01, 10000, (uint)good.TotalSectors),
                new MbrSlot(2, 0x01, 20000, 5000),
                new MbrSlot(3, 0x0C, 35000, 10000)));
            await disk.WriteAsync(10000, good.Build());
            await disk.WriteAsync(20000, broken.Build());

            var layout = await FatDetector.DetectDiskAsync(disk);

            Assert.That(layout.Volumes.Single().Partition!.Index, Is.EqualTo(1));
            Assert.That(layout.Problems.Select(p => (p.Partition.Index, p.Kind)), Is.EqualTo(new[]
            {
                (2, FatErrorKind.Corrupt),
                (3, FatErrorKind.Corrupt)
            }));
        }

        [Test]
        public async Task PartitionWithOtherSectorSizeIsReportedTest()
        {
            var alien = FatBootSectorBuilder.ForClusters(3000, false, bytesPerSector: 4096);
            await using var disk = new BlockDeviceMemory(40000);
            await disk.WriteAsync(0, MbrBuilder.Build(0, new MbrSlot(0, 0x0C, 100, 30000)));
            await disk.WriteAsync(100, alien.Build().AsMemory(0, 512));

            var layout = await FatDetector.DetectDiskAsync(disk);

            Assert.That(layout.Problems.Single().Kind, Is.EqualTo(FatErrorKind.SectorSizeMismatch));
        }

        [Test]
        public async Task TableOnLargeSectorsCountsInThemTest()
        {
            var fat = FatBootSectorBuilder.ForClusters(70000, true, bytesPerSector: 4096);
            await using var disk = new BlockDeviceMemory(256 + fat.TotalSectors, 4096);
            await disk.WriteAsync(0, MbrBuilder.BuildForSectorSize(4096, 0, new MbrSlot(0, 0x0C, 256, (uint)fat.TotalSectors)));
            await disk.WriteAsync(256, fat.Build());

            var layout = await FatDetector.DetectDiskAsync(disk);

            var location = layout.Volumes.Single();
            Assert.That(location.FirstSector, Is.EqualTo(256));
            Assert.That(location.Volume.Kind, Is.EqualTo(FatKind.Fat32));
            Assert.That(location.Volume.SectorSize, Is.EqualTo(4096));
        }

        #endregion

        #region Volume Tests

        [Test]
        public async Task PartitionTableIsNotAVolumeTest()
        {
            await using var disk = new BlockDeviceMemory(1000);
            await disk.WriteAsync(0, MbrBuilder.Build(0, new MbrSlot(0, 0x0C, 100, 100)));

            Assert.That(await FatDetector.DetectVolumeAsync(disk), Is.Null);
        }

        [Test]
        public async Task VolumeOfOtherSectorSizeIsReportedTest()
        {
            var sector = FatBootSectorBuilder.ForClusters(5000, false, bytesPerSector: 4096).Build();
            await using var disk = new BlockDeviceMemory(100000);
            await disk.WriteAsync(0, sector.AsMemory(0, 512));

            var error = Assert.ThrowsAsync<FatException>(async () => await FatDetector.DetectVolumeAsync(disk));

            Assert.That(error!.Kind, Is.EqualTo(FatErrorKind.SectorSizeMismatch));
        }

        [Test]
        public async Task ExFatOfOtherSectorSizeIsReportedBeforeReadingRegionTest()
        {
            var region = new ExFatBootRegionBuilder().Build();
            var probe = new BlockDeviceProbe(new BlockDeviceMemory(64, 4096));
            await probe.WriteAsync(0, region.AsMemory(0, 4096));
            probe.Clear();

            var error = Assert.ThrowsAsync<FatException>(async () => await FatDetector.DetectVolumeAsync(probe));

            Assert.That(error!.Kind, Is.EqualTo(FatErrorKind.SectorSizeMismatch));
            Assert.That(probe.SectorsRead, Is.EqualTo(1));
        }

        [Test]
        public async Task ExFatReadsItsBootRegionInTwoRequestsTest()
        {
            var region = new ExFatBootRegionBuilder().Build();
            var probe = new BlockDeviceProbe(new BlockDeviceMemory(32768));
            await probe.WriteAsync(0, region);
            probe.Clear();

            var volume = await FatDetector.DetectVolumeAsync(probe);

            Assert.That(volume!.Kind, Is.EqualTo(FatKind.ExFat));
            Assert.That(probe.Requests, Is.EqualTo(new[]
            {
                new BlockDeviceRequest(BlockDeviceOperation.Read, 0, 1),
                new BlockDeviceRequest(BlockDeviceOperation.Read, 1, 11)
            }));
        }

        [Test]
        public async Task ExFatOnTooSmallDeviceIsCorruptTest()
        {
            var region = new ExFatBootRegionBuilder().Build();
            await using var disk = new BlockDeviceMemory(11);
            await disk.WriteAsync(0, region.AsMemory(0, 11 * 512));

            var error = Assert.ThrowsAsync<FatException>(async () => await FatDetector.DetectVolumeAsync(disk));

            Assert.That(error!.Kind, Is.EqualTo(FatErrorKind.Corrupt));
        }

        [Test]
        public void NullDeviceIsRejectedTest()
        {
            Assert.ThrowsAsync<ArgumentNullException>(async () => await FatDetector.DetectVolumeAsync(null!));
            Assert.ThrowsAsync<ArgumentNullException>(async () => await FatDetector.DetectDiskAsync(null!));
        }

        #endregion
    }
}
