using OutWit.Common.Fat.Boot;
using OutWit.Common.Fat.Devices;
using OutWit.Common.Fat.Tests.Utils;
using OutWit.Common.NUnit;

namespace OutWit.Common.Fat.Tests
{
    [TestFixture]
    public class FatFormatterTests
    {
        #region Constants

        private const long MIB = 1L << 20;

        private static readonly FatVolumeOptions LEAVE_OPEN = new() { LeaveOpen = true };

        #endregion

        #region Choice Tests

        [TestCase(1474560L, 512, FatKind.Fat12, 512)]
        [TestCase(8 * MIB, 512, FatKind.Fat12, 4096)]
        [TestCase(9 * MIB, 512, FatKind.Fat16, 1024)]
        [TestCase(512 * MIB, 512, FatKind.Fat16, 8192)]
        [TestCase(513 * MIB, 512, FatKind.Fat32, 4096)]
        [TestCase(200 * MIB, 512, FatKind.Fat16, 4096)]
        [TestCase(9 * MIB, 4096, FatKind.Fat12, 4096)]
        [TestCase(64 * MIB, 4096, FatKind.Fat16, 4096)]
        [TestCase(33L << 30, 512, FatKind.ExFat, 131072)]
        public async Task KindAndClusterSizeFollowTheSizeTest(long bytes, int sectorSize, FatKind kind, int clusterSize)
        {
            await using var device = new BlockDeviceMemory(bytes / sectorSize, sectorSize);

            var info = await FatFormatter.FormatAsync(device);

            Assert.That((info.Kind, info.ClusterSize), Is.EqualTo((kind, clusterSize)));
        }

        [Test]
        public async Task LargeFat32UsesLargerClustersTest()
        {
            await using var device = new BlockDeviceMemory((32L << 30) / 512);

            var info = await FatFormatter.FormatAsync(device);

            Assert.That((info.Kind, info.ClusterSize), Is.EqualTo((FatKind.Fat32, 16384)));
        }

        #endregion

        #region Layout Tests

        [TestCase(FatKind.Fat12)]
        [TestCase(FatKind.Fat16)]
        [TestCase(FatKind.Fat32)]
        [TestCase(FatKind.ExFat)]
        public async Task NewVolumeIsEmptyAndUsableTest(FatKind kind)
        {
            var disk = new BlockDeviceMemory(Size(kind) / 512);
            var info = await FatFormatter.FormatAsync(disk, new FatFormatOptions { Kind = kind, Label = "NEW", VolumeSerial = 0x12345678 });

            await using var volume = await FatVolume.MountAsync(disk, LEAVE_OPEN);
            long free = await volume.CountFreeClustersAsync();
            await volume.CreateDirectoryAsync("/dir");
            await volume.WriteAllBytesAsync("/dir/a file.txt", FatVolumeOracleTests.Pattern("new", 5000));

            Assert.That(volume.Info, Was.EqualTo(info));
            Assert.That(await volume.EnumerateAsync("/").Select(e => e.Name).ToListAsync(), Is.EqualTo(new[] { "dir" }));
            Assert.That(await volume.GetLabelAsync(), Is.EqualTo("NEW"));
            Assert.That(info.VolumeSerial, Is.EqualTo(0x12345678));
            Assert.That(free, Is.EqualTo(info.ClusterCount - Used(kind, info)));
            Assert.That(await volume.ReadAllBytesAsync("/DIR/A FILE.TXT"), Is.EqualTo(FatVolumeOracleTests.Pattern("new", 5000)));
            await VolumeConsistency.AssertAsync(volume);
        }

        [Test]
        public async Task OptionsShapeTheLayoutTest()
        {
            await using var device = new BlockDeviceMemory(20 * MIB / 512);

            var info = await FatFormatter.FormatAsync(device, new FatFormatOptions
            {
                Kind = FatKind.Fat16, ClusterSize = 2048, FatCount = 1, RootEntries = 100, Label = "mixed Case"
            });

            await using var volume = await FatVolume.MountAsync(device, LEAVE_OPEN);
            Assert.That((info.SectorsPerCluster, info.FatCount, info.RootDirectoryEntries), Is.EqualTo((4, 1, 112)));
            Assert.That(await volume.GetLabelAsync(), Is.EqualTo("MIXED CASE"));
        }

        [Test]
        public async Task Fat32CarriesItsBackupsTest()
        {
            await using var device = new BlockDeviceMemory(64 * MIB / 512);

            var info = await FatFormatter.FormatAsync(device, new FatFormatOptions { Kind = FatKind.Fat32, ClusterSize = 512 });

            var boot = new byte[512];
            var backup = new byte[512];
            await device.ReadAsync(0, boot);
            await device.ReadAsync(info.BackupBootSector!.Value, backup);
            var (freeCount, nextFree) = await BlankVolume.ReadFsInfoAsync(device);
            var (backupFree, _) = await BlankVolume.ReadFsInfoAsync(device, info.BackupBootSector.Value + 1);
            Assert.That(backup, Is.EqualTo(boot));
            Assert.That((freeCount, nextFree, backupFree), Is.EqualTo((info.ClusterCount - 1, 3u, info.ClusterCount - 1)));
            Assert.That(info.RootCluster, Is.EqualTo(2));
            Assert.That(info.ClusterCount, Is.GreaterThanOrEqualTo(65525));
        }

        [Test]
        public async Task ExFatBootRegionIsBackedUpTest()
        {
            await using var device = new BlockDeviceMemory(64 * MIB / 4096, 4096);

            var info = await FatFormatter.FormatAsync(device, new FatFormatOptions { Kind = FatKind.ExFat, ClusterSize = 16384 });

            var main = new byte[12 * 4096];
            var backup = new byte[12 * 4096];
            await device.ReadAsync(0, main);
            await device.ReadAsync(12, backup);
            Assert.That(backup, Is.EqualTo(main));
            Assert.That(ExFatBootChecksum.Matches(main, 4096), Is.True);
            Assert.That(main[112], Is.EqualTo((byte)((info.RootCluster!.Value - 1) * 100 / info.ClusterCount)));
            Assert.That(info.FatOffset, Is.EqualTo(24));
            Assert.That(info.ClusterHeapSector % info.SectorsPerCluster, Is.Zero);
        }

        [TestCase(256 * MIB, 512, 131072)]
        [TestCase(16 * MIB, 512, 32768)]
        [TestCase(64 * MIB, 4096, 65536)]
        public async Task ExFatHeapStartsOnAClusterBoundaryTest(long bytes, int sectorSize, int clusterSize)
        {
            await using var device = new BlockDeviceMemory(bytes / sectorSize, sectorSize);

            var info = await FatFormatter.FormatAsync(device, new FatFormatOptions { Kind = FatKind.ExFat, ClusterSize = clusterSize });

            Assert.That(info.SectorsPerCluster, Is.EqualTo(clusterSize / sectorSize));
            Assert.That(info.ClusterHeapSector % info.SectorsPerCluster, Is.Zero);
            Assert.That(info.ClusterHeapSector, Is.LessThan(info.FatOffset + info.FatSectors + info.SectorsPerCluster));
        }

        [TestCase(FatKind.Fat12, 8 * MIB, 512, 0x01)]
        [TestCase(FatKind.Fat16, 64 * MIB, 512, 0x06)]
        [TestCase(FatKind.Fat32, 300 * MIB, 4096, 0x0C)]
        [TestCase(FatKind.ExFat, 64 * MIB, 512, 0x07)]
        public async Task DiskGetsOnePartitionTest(FatKind kind, long bytes, int sectorSize, int type)
        {
            await using var disk = new BlockDeviceMemory(bytes / sectorSize, sectorSize);

            var layout = await FatFormatter.FormatDiskAsync(disk, new FatFormatOptions { Kind = kind, VolumeSerial = 0xCAFE0001 });

            long start = MIB / sectorSize;
            Assert.That(layout.PartitionTable, Is.EqualTo(PartitionTableKind.Mbr));
            Assert.That(layout.DiskSignature, Is.EqualTo(0xCAFE0001));
            Assert.That(layout.Partitions.Select(p => (p.Index, (int)p.Type, p.FirstSector, p.SectorCount, p.IsActive)),
                Is.EqualTo(new[] { (0, type, start, bytes / sectorSize - start, false) }));
            Assert.That(layout.Volumes.Single().Volume.Kind, Is.EqualTo(kind));
            await using var volume = await FatVolume.MountDiskAsync(disk, LEAVE_OPEN);
            Assert.That(await volume.EnumerateAsync().CountAsync(), Is.Zero);
        }

        [Test]
        public async Task ReformattingLeavesNothingOfTheOldVolumeTest()
        {
            var disk = await ReferenceImages.OpenAsync(ReferenceImages.Get("fat32-c1"), isReadOnly: false);

            await FatFormatter.FormatAsync(disk, new FatFormatOptions { Kind = FatKind.ExFat, Label = "AFTER" });

            await using var volume = await FatVolume.MountAsync(disk, LEAVE_OPEN);
            Assert.That(volume.Info.Kind, Is.EqualTo(FatKind.ExFat));
            Assert.That(await volume.EnumerateAsync().CountAsync(), Is.Zero);
            await VolumeConsistency.AssertAsync(volume);
        }

        [TestCase(FatKind.Fat12, 4143, 4084u)]
        [TestCase(FatKind.Fat12, 4144, 0u)]
        [TestCase(FatKind.Fat16, 4152, 0u)]
        [TestCase(FatKind.Fat16, 4153, 0u)]
        [TestCase(FatKind.Fat16, 4154, 4087u)]
        [TestCase(FatKind.Fat16, 66073, 65524u)]
        [TestCase(FatKind.Fat16, 66074, 0u)]
        [TestCase(FatKind.Fat32, 66598, 0u)]
        [TestCase(FatKind.Fat32, 66599, 65525u)]
        public async Task ClusterCountStaysWhereLinuxReadsTheKindTest(FatKind kind, long sectors, uint clusters)
        {
            await using var disk = new BlockDeviceMemory(sectors);
            var options = new FatFormatOptions { Kind = kind, ClusterSize = 512 };

            if (clusters == 0)
            {
                Assert.That(async () => await FatFormatter.FormatAsync(disk, options), Throws.ArgumentException);
                return;
            }

            var info = await FatFormatter.FormatAsync(disk, options);
            Assert.That(info.Kind, Is.EqualTo(kind));
            Assert.That(info.ClusterCount, Is.EqualTo(clusters));
        }

        [TestCase(FatKind.Fat12, 4 * MIB, 0)]
        [TestCase(FatKind.Fat16, 32 * MIB, 0)]
        [TestCase(FatKind.Fat32, 64 * MIB, 512)]
        [TestCase(FatKind.ExFat, 16 * MIB, 0)]
        public async Task OldBytesDoNotShowThroughTest(FatKind kind, long bytes, int clusterSize)
        {
            await using var disk = new BlockDeviceMemory(bytes / 512);
            var old = Enumerable.Repeat((byte)'A', (int)MIB).ToArray();
            for (long sector = 0; sector < disk.SectorCount; sector += MIB / 512)
                await disk.WriteAsync(sector, old);

            await FatFormatter.FormatAsync(disk, new FatFormatOptions { Kind = kind, ClusterSize = clusterSize == 0 ? null : clusterSize });

            await using var volume = await FatVolume.MountAsync(disk, LEAVE_OPEN);
            Assert.That(await volume.EnumerateAsync().CountAsync(), Is.Zero);
            Assert.That(await volume.GetLabelAsync(), Is.Null);
            var report = await FatChecker.CheckAsync(volume);
            Assert.That(report.Problems, Is.Empty);
            Assert.That(report.FreeClusters, Is.EqualTo(await volume.CountFreeClustersAsync()));
        }

        [Test]
        public async Task SerialComesFromTheClockTest()
        {
            var clock = new FixedClock(new DateTime(2026, 9, 16, 22, 10, 5, 330));
            await using var first = new BlockDeviceMemory(4 * MIB / 512);
            await using var second = new BlockDeviceMemory(4 * MIB / 512);

            var one = await FatFormatter.FormatAsync(first, new FatFormatOptions { Clock = clock });
            var two = await FatFormatter.FormatAsync(second, new FatFormatOptions { Clock = clock });
            clock.Now = clock.Now.AddSeconds(1);
            var three = await FatFormatter.FormatAsync(second, new FatFormatOptions { Clock = clock });

            Assert.That(one.VolumeSerial, Is.EqualTo(two.VolumeSerial).And.Not.Zero);
            Assert.That(three.VolumeSerial, Is.Not.EqualTo(one.VolumeSerial));
        }

        #endregion

        #region Refusal Tests

        [TestCase(FatKind.Fat16, 3000)]
        [TestCase(FatKind.Fat16, 256)]
        [TestCase(FatKind.Fat16, 131072)]
        [TestCase(FatKind.Fat12, 512)]
        [TestCase(FatKind.Fat32, 4096)]
        [TestCase(FatKind.Fat16, 32768)]
        public async Task LayoutTheKindCannotHaveIsRefusedTest(FatKind kind, int clusterSize)
        {
            await using var device = new BlockDeviceMemory(20 * MIB / 512);

            var error = Assert.ThrowsAsync<ArgumentException>(async () =>
                await FatFormatter.FormatAsync(device, new FatFormatOptions { Kind = kind, ClusterSize = clusterSize }));

            Assert.That(error, Is.Not.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(await FatDetector.DetectVolumeAsync(device), Is.Null, "nothing is written when the options do not fit");
        }

        [TestCase(0)]
        [TestCase(65536)]
        [TestCase(int.MaxValue)]
        public async Task RootDirectorySizesTheKindCannotHaveAreRefusedTest(int rootEntries)
        {
            await using var device = new BlockDeviceMemory(20 * MIB / 512);
            var untouched = new BlockDeviceProbe(device) { FailWrite = (_, _) => true };

            Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
                await FatFormatter.FormatAsync(untouched, new FatFormatOptions { Kind = FatKind.Fat16, RootEntries = rootEntries }),
                "a root size the boot sector cannot hold is refused before anything is written");
        }

        [TestCase(FatKind.Fat16, "TWELVE CHARS")]
        [TestCase(FatKind.Fat16, "STAR*")]
        [TestCase(FatKind.Fat16, "ÜBER")]
        [TestCase(FatKind.ExFat, "tab\there")]
        public async Task LabelTheKindCannotHoldIsRefusedTest(FatKind kind, string label)
        {
            await using var device = new BlockDeviceMemory(20 * MIB / 512);

            Assert.ThrowsAsync<ArgumentException>(async () =>
                await FatFormatter.FormatAsync(device, new FatFormatOptions { Kind = kind, Label = label }));
        }

        [TestCase(0)]
        [TestCase(3)]
        public async Task TableCountOutOfRangeIsRefusedTest(int fatCount)
        {
            await using var device = new BlockDeviceMemory(20 * MIB / 512);

            Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
                await FatFormatter.FormatAsync(device, new FatFormatOptions { FatCount = fatCount }));
        }

        [Test]
        public async Task DevicesThatCannotBeFormattedAreRefusedTest()
        {
            await using var readOnly = await ReferenceImages.OpenAsync(ReferenceImages.Get("fat16-c1"));
            await using var tiny = new BlockDeviceMemory(4);
            await using var noRoomForAPartition = new BlockDeviceMemory(2048);

            Assert.ThrowsAsync<NotSupportedException>(async () => await FatFormatter.FormatAsync(readOnly));
            Assert.ThrowsAsync<ArgumentException>(async () => await FatFormatter.FormatAsync(tiny));
            Assert.ThrowsAsync<ArgumentException>(async () => await FatFormatter.FormatDiskAsync(noRoomForAPartition));
            Assert.ThrowsAsync<ArgumentNullException>(async () => await FatFormatter.FormatAsync(null!));
        }

        [TestCase(FatKind.Fat16)]
        [TestCase(FatKind.ExFat)]
        public async Task InterruptedFormatLeavesNoVolumeTest(FatKind kind)
        {
            var disk = await ReferenceImages.OpenAsync(ReferenceImages.Get(kind == FatKind.ExFat ? "exfat-c8" : "fat16-c1"), isReadOnly: false);
            var probe = new BlockDeviceProbe(disk) { FailWrite = (sector, _) => sector > 30 };

            Assert.ThrowsAsync<IOException>(async () => await FatFormatter.FormatAsync(probe, new FatFormatOptions { Kind = kind }));

            var backupRegion = new byte[12 * 512];
            await disk.ReadAsync(12, backupRegion);
            Assert.That(await FatDetector.DetectVolumeAsync(disk), Is.Null);
            Assert.That(System.Text.Encoding.ASCII.GetString(backupRegion, 3, 8), Is.Not.EqualTo("EXFAT   "), "An old backup region would let fsck.exfat bring the old volume back.");
        }

        [Test]
        public async Task InterruptedDiskFormatLeavesNoOldVolumeTest()
        {
            var disk = await ReferenceImages.OpenAsync(ReferenceImages.Get("exfat-c8"), isReadOnly: false);
            long start = MIB / 512;
            var probe = new BlockDeviceProbe(disk) { FailWrite = (sector, _) => sector > start + 30 };

            Assert.ThrowsAsync<IOException>(async () => await FatFormatter.FormatDiskAsync(probe, new FatFormatOptions { Kind = FatKind.Fat16 }));

            var layout = await FatDetector.DetectDiskAsync(disk);
            Assert.That(layout.Volumes, Is.Empty);
            Assert.That(layout.Problems, Is.Empty);
            Assert.That(await HasExFatBackupRegionAsync(disk), Is.False, "An old backup region would let fsck.exfat bring the old volume back.");
        }

        [Test]
        public async Task DiskFormatClearsWhatAWholeDiskVolumeLeftTest()
        {
            await using var disk = await ReferenceImages.OpenAsync(ReferenceImages.Get("exfat-c8"), isReadOnly: false);
            Assert.That(await HasExFatBackupRegionAsync(disk), Is.True, "the image is a whole-disk exFAT volume with its backup region at sector 12");

            var layout = await FatFormatter.FormatDiskAsync(disk, new FatFormatOptions { Kind = FatKind.Fat16 });

            Assert.That(layout.Volumes, Has.Count.EqualTo(1));
            Assert.That(layout.Problems, Is.Empty);
            Assert.That(await HasExFatBackupRegionAsync(disk), Is.False, "An old backup region would let fsck.exfat bring the old volume back over the new table.");
        }

        #endregion

        #region Tools

        private static async Task<bool> HasExFatBackupRegionAsync(IBlockDevice disk)
        {
            var backupRegion = new byte[12 * 512];
            await disk.ReadAsync(12, backupRegion);
            return System.Text.Encoding.ASCII.GetString(backupRegion, 3, 8) == "EXFAT   ";
        }

        private static long Size(FatKind kind)
        {
            return kind switch { FatKind.Fat12 => 4 * MIB, FatKind.Fat16 => 32 * MIB, FatKind.Fat32 => 48 * MIB, _ => 16 * MIB };
        }

        /// <summary>
        /// The clusters a new volume uses: FAT32's root, or exFAT's bitmap, up-case table and root.
        /// </summary>
        private static long Used(FatKind kind, FatVolumeInfo info)
        {
            return kind switch
            {
                FatKind.Fat32 => 1,
                FatKind.ExFat => info.RootCluster!.Value - 1,
                _ => 0
            };
        }

        #endregion
    }
}
