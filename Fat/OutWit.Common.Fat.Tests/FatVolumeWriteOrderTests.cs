using OutWit.Common.Fat.Devices;
using OutWit.Common.Fat.Directories;
using OutWit.Common.Fat.Tests.Utils;

namespace OutWit.Common.Fat.Tests
{
    /// <summary>
    /// The order changes reach the device in, and what a failure part of the way leaves:
    /// never an entry that counts on clusters the table or the bitmap does not hold for it.
    /// </summary>
    [TestFixture]
    public class FatVolumeWriteOrderTests
    {
        #region Growth Tests

        [TestCase(FatKind.Fat16)]
        [TestCase(FatKind.Fat32)]
        [TestCase(FatKind.ExFat)]
        public async Task DirectoryThatFailsToGrowIsLeftAsItWasTest(FatKind kind)
        {
            var (probe, volume) = await MountAsync(kind);
            await volume.CreateDirectoryAsync("/sub");
            var before = await volume.GetClusterRunsAsync("/sub", CancellationToken.None);
            int files = 0;
            uint next = before[^1].LastCluster + 1;
            probe.FailWrite = (sector, _) => sector == volume.Core.ClusterSector(next);

            var error = Assert.ThrowsAsync<IOException>(async () =>
            {
                for (; files < 20; files++)
                    await (await volume.CreateAsync($"/sub/file {files:D2}.txt")).DisposeAsync();
            });
            probe.FailWrite = null;
            await volume.FlushAsync();

            Assert.That(error, Is.Not.Null);
            Assert.That(await volume.GetClusterRunsAsync("/sub", CancellationToken.None), Is.EqualTo(before));
            Assert.That(await volume.EnumerateAsync("/sub").CountAsync(), Is.EqualTo(files));
            await VolumeConsistency.AssertAsync(volume);
            await (await volume.CreateAsync("/sub/after the failure.txt")).DisposeAsync();
            await VolumeConsistency.AssertAsync(volume);
        }

        [TestCase(FatKind.Fat16)]
        [TestCase(FatKind.ExFat)]
        public async Task NewDirectoryIsMarkedUsedBeforeItsEntryTest(FatKind kind)
        {
            var (probe, volume) = await MountAsync(kind);
            await volume.CreateDirectoryAsync("/first");
            probe.Clear();

            await volume.CreateDirectoryAsync("/second");

            var item = (await volume.GetItemAsync("/second", CancellationToken.None))!;
            long entrySector = await EntrySectorAsync(volume, item);
            var writes = probe.Of(BlockDeviceOperation.Write).ToList();
            int entry = writes.FindIndex(write => write.Sector <= entrySector && entrySector < write.Sector + write.Count);
            int allocation = writes.FindIndex(write => IsAllocationSector(volume, write.Sector));
            Assert.That(allocation, Is.GreaterThanOrEqualTo(0).And.LessThan(entry));
        }

        #endregion

        #region Shrink Tests

        [Test]
        public async Task ShorterFileEndsItsChainAfterItsEntryTest()
        {
            var (probe, volume) = await MountAsync(FatKind.Fat16);
            await volume.WriteAllBytesAsync("/a.bin", new byte[3 * 512]);
            var item = (await volume.GetItemAsync("/a.bin", CancellationToken.None))!;
            long entrySector = await EntrySectorAsync(volume, item);
            probe.Clear();

            await using (var stream = await volume.OpenAsync("/a.bin", FileMode.Open, FileAccess.Write))
                await stream.SetLengthAsync(512);

            var writes = probe.Of(BlockDeviceOperation.Write).ToList();
            int entry = writes.FindIndex(write => write.Sector == entrySector);
            Assert.That(writes.FindIndex(write => IsAllocationSector(volume, write.Sector)), Is.GreaterThan(entry));
            Assert.That(await volume.GetClusterRunsAsync("/a.bin", CancellationToken.None), Has.Count.EqualTo(1));
            await VolumeConsistency.AssertAsync(volume);
        }

        [TestCase(FatKind.Fat16)]
        [TestCase(FatKind.ExFat)]
        public async Task ReleaseThatFailsCanRunAgainTest(FatKind kind)
        {
            var (probe, volume) = await MountAsync(kind, clusters: 5000);
            long free = await volume.CountFreeClustersAsync();
            var content = new byte[3000 * 512];
            content.AsSpan().Fill(1);
            await volume.WriteAllBytesAsync("/big.bin", content);
            int failures = 1;
            probe.FailWrite = (sector, _) => IsAllocationSector(volume, sector) && failures-- > 0;

            var stream = await volume.OpenAsync("/big.bin", FileMode.Open, FileAccess.Write);
            await stream.SetLengthAsync(0);
            Assert.ThrowsAsync<IOException>(async () => await stream.FlushAsync());
            await stream.DisposeAsync();

            Assert.That(failures, Is.LessThanOrEqualTo(0), "a write of the release failed once");
            Assert.That(volume.Core.Allocator.FreeCount, Is.EqualTo(free));
            Assert.That(await volume.CountFreeClustersAsync(), Is.EqualTo(free));
            await VolumeConsistency.AssertAsync(volume);
        }

        #endregion

        #region Move Tests

        [Test]
        public async Task DirectoriesWithoutClustersMoveIntoEachOtherTest()
        {
            var (_, volume) = await MountAsync(FatKind.ExFat);
            var root = volume.Core.OpenDirectory(volume.Names.Root);
            var writer = FatDirectoryWriter.Open(volume.Core, root);
            foreach (string name in new[] { "E", "F" })
                await writer.AddAsync(name, writer.NewTemplate(FatAttributes.Directory, 0, 0, false), null, CancellationToken.None);

            await volume.MoveAsync("/E", "/F/E");

            Assert.That(await volume.EnumerateAsync("/", recursive: true).Select(e => e.Path).ToListAsync(), Is.EqualTo(new[] { "/F", "/F/E" }));
            await VolumeConsistency.AssertAsync(volume);
        }

        #endregion

        #region Delete Tests

        [TestCase(FatKind.Fat16)]
        [TestCase(FatKind.ExFat)]
        public async Task DeleteStoppedByAnOpenFileWritesOutWhatItDeletedTest(FatKind kind)
        {
            var (probe, volume) = await MountAsync(kind);
            await volume.CreateDirectoryAsync("/t");
            await volume.WriteAllBytesAsync("/t/a.txt", new byte[600]);
            await volume.CreateDirectoryAsync("/t/sub");
            await volume.WriteAllBytesAsync("/t/sub/b.txt", new byte[600]);
            await volume.WriteAllBytesAsync("/t/z.txt", new byte[600]);

            FatException? stopped;
            await using (await volume.OpenReadAsync("/t/sub/b.txt"))
                stopped = Assert.ThrowsAsync<FatException>(async () => await volume.DeleteAsync("/t", recursive: true));

            Assert.That(stopped!.Kind, Is.EqualTo(FatErrorKind.InUse));
            Assert.That(await volume.ExistsAsync("/t/a.txt"), Is.False, "what was deleted before the open file stays deleted");
            Assert.That(await volume.ExistsAsync("/t/sub/b.txt"), Is.True);

            // As a fresh process sees the device now: the clusters of what was deleted are free there too.
            await using (var fresh = await TestVolumes.RemountAsync(probe))
                Assert.That((await FatChecker.CheckAsync(fresh)).LostClusters, Is.Zero, "the release reached the device with the deletion");

            await volume.DeleteAsync("/t", recursive: true);
            await VolumeConsistency.AssertAsync(volume);
            await volume.DisposeAsync();
        }

        #endregion

        #region Tools

        private static async Task<(BlockDeviceProbe Probe, FatVolume Volume)> MountAsync(FatKind kind, long clusters = 0)
        {
            var disk = kind == FatKind.ExFat
                ? await BlankVolumeExFat.CreateAsync(clusters == 0 ? 300 : clusters)
                : await BlankVolume.CreateAsync(kind, clusters == 0 ? (kind == FatKind.Fat16 ? 4200 : 300) : clusters, rootEntries: 32);
            var probe = new BlockDeviceProbe(disk);
            return (probe, await FatVolume.MountAsync(probe, TestVolumes.Options(null)));
        }

        /// <summary>
        /// Whether a sector holds a table or, on exFAT, the bitmap.
        /// </summary>
        private static bool IsAllocationSector(FatVolume volume, long sector)
        {
            var info = volume.Info;
            if (sector >= info.FatOffset && sector < info.FatOffset + info.FatCount * info.FatSectors)
                return true;
            return info.Kind == FatKind.ExFat && sector == volume.Core.ClusterSector(2);
        }

        private static async Task<long> EntrySectorAsync(FatVolume volume, DirectoryItem item)
        {
            var parent = volume.Core.OpenDirectory(item.Parent ?? volume.Names.Root);
            return await parent.GetSlotOffsetAsync(item.LastSlot, CancellationToken.None) / volume.Info.SectorSize;
        }

        #endregion
    }
}
