using OutWit.Common.Fat.Directories;
using OutWit.Common.Fat.Exceptions;
using OutWit.Common.Fat.Model;
using OutWit.Common.Fat.Tests.Utils;
using OutWit.Common.Fat.Utils;
using OutWit.Common.Fat.Volumes;

namespace OutWit.Common.Fat.Tests.Directories
{
    [TestFixture]
    public class FatDirectoryTests
    {
        #region Limit Tests

        [Test]
        public async Task DirectoryWithoutEndStopsAtItsLimitTest()
        {
            var clusters = Enumerable.Range(2, 5000).Select(c => (uint)c).ToArray();
            var directory = await CreateAsync(new FatTableImage(FatKind.Fat16, 6000, 24).Chain(clusters));

            int listed = 0;
            var error = Assert.ThrowsAsync<FatException>(async () =>
            {
                await foreach (var _ in directory.EnumerateAsync(CancellationToken.None))
                    listed++;
            });

            Assert.That(error!.Kind, Is.EqualTo(FatErrorKind.Corrupt));
            Assert.That(error.Message, Does.Contain("65536 entries"));
            Assert.That(listed, Is.EqualTo(65536));
        }

        [Test]
        public async Task FullDirectoryWithoutEndIsWholeTest()
        {
            var clusters = Enumerable.Range(2, FatDirectoryVfat.MAX_BYTES / FatTableImage.SECTOR_SIZE).Select(c => (uint)c).ToArray();
            var directory = await CreateAsync(new FatTableImage(FatKind.Fat16, 6000, 24).Chain(clusters));

            int listed = 0;
            await foreach (var _ in directory.EnumerateAsync(CancellationToken.None))
                listed++;

            Assert.That(listed, Is.EqualTo(65536));
        }

        [Test]
        public async Task DirectoryChainLoopIsCaughtAtOnceTest()
        {
            var directory = await CreateAsync(new FatTableImage(FatKind.Fat16, 6000, 24).Set(2, 3).Set(3, 2));

            int listed = 0;
            var error = Assert.ThrowsAsync<FatException>(async () =>
            {
                await foreach (var _ in directory.EnumerateAsync(CancellationToken.None))
                    listed++;
            });

            // The first cluster is listed before the chain is followed past it.
            Assert.That(error!.Message, Does.Contain("loops"));
            Assert.That(listed, Is.EqualTo(16));
        }

        [Test]
        public async Task SubdirectoryWithoutClustersIsCorruptTest()
        {
            var image = new FatTableImage(FatKind.Fat16, 100, 1);
            var core = new FatVolumeCore(await image.CreateDeviceAsync(), image.Info);
            var directory = Open(core, new FatDirectoryEntry { Path = "/sub", Attributes = FatAttributes.Directory });

            var error = Assert.ThrowsAsync<FatException>(async () => await directory.FindAsync("x", CancellationToken.None));

            Assert.That(error!.Message, Does.Contain("/sub has no clusters"));
        }

        #endregion

        #region Cost Tests

        [TestCase(FatKind.Fat16)]
        [TestCase(FatKind.Fat32)]
        public async Task SmallDirectoryCostsOneSectorTest(FatKind kind)
        {
            var (probe, core, names) = await TestVolumes.CoreAsync(kind, clusters: kind == FatKind.Fat32 ? 300 : 4200, rootEntries: 512);
            var root = names.Open(names.Root);
            probe.Clear();

            Assert.That(await root.FindAsync("missing.txt", CancellationToken.None), Is.Null);

            Assert.That(probe.SectorsRead, Is.EqualTo(1));
        }

        [Test]
        public async Task LargeDirectoryIsReadInGrowingStepsTest()
        {
            var image = new FatTableImage(FatKind.Fat16, 6000, 24).Chain(Enumerable.Range(2, 400).Select(c => (uint)c).ToArray());
            var device = await image.CreateDeviceAsync();
            await device.WriteAsync(image.Info.ClusterHeapSector, Enumerable.Repeat((byte)'A', 400 * FatTableImage.SECTOR_SIZE).ToArray());
            var probe = new BlockDeviceProbe(device);
            var directory = Open(new FatVolumeCore(probe, image.Info),
                new FatDirectoryEntry { Path = "/big", Attributes = FatAttributes.Directory, FirstCluster = 2 });

            int listed = 0;
            await foreach (var _ in directory.EnumerateAsync(CancellationToken.None))
                listed++;

            var counts = probe.Of(BlockDeviceOperation.Read)
                .Where(request => request.Sector >= image.Info.ClusterHeapSector)
                .Select(request => request.Count);
            Assert.That(listed, Is.EqualTo(400 * 16));
            Assert.That(counts, Is.EqualTo(new[] { 1, 2, 4, 8, 16, 32, 64, 128, 128, 17 }));
        }

        #endregion

        #region Growth Tests

        [Test]
        public async Task FailedChainWriteGivesBackTheDirectoryClustersTest()
        {
            // A FAT16 volume whose table spans twenty sectors. The subdirectory is cluster 2, and
            // a file holds the next four thousand, so the search for the directory's next cluster
            // runs through sixteen table sectors and pushes the first out of the window; the link
            // from cluster 2 has to read it back, and that read fails.
            var probe = new BlockDeviceProbe(await BlankVolume.CreateAsync(FatKind.Fat16, 5000, rootEntries: 16));
            await using var volume = await FatVolume.MountAsync(probe, TestVolumes.Options(null));
            await volume.CreateDirectoryAsync("/d");
            await volume.WriteAllBytesAsync("/big.bin", new byte[4000 * 512]);
            for (int i = 0; i < 14; i++)
                await volume.WriteAllBytesAsync($"/d/F{i:00}.TXT", Array.Empty<byte>());
            long free = await volume.CountFreeClustersAsync();

            for (uint k = 1; k <= 8; k++)
                await volume.Core.Table.GetAsync(256 * k + 2, CancellationToken.None);
            int reads = 0;
            probe.FailRead = (sector, _) => sector == volume.Info.FatOffset && ++reads == 2;
            Assert.ThrowsAsync<IOException>(async () => await volume.WriteAllBytesAsync("/d/F14.TXT", Array.Empty<byte>()));
            probe.FailRead = null;

            Assert.That(await volume.ExistsAsync("/d/F14.TXT"), Is.False);
            Assert.That(await volume.EnumerateAsync("/d").CountAsync(), Is.EqualTo(14));
            Assert.That(await volume.CountFreeClustersAsync(), Is.EqualTo(free), "the cluster taken for the directory is given back");
            await VolumeConsistency.AssertAsync(volume);
        }

        #endregion

        #region Tools

        /// <summary>
        /// A directory at cluster 2 whose clusters are full of file entries and hold no end marker.
        /// </summary>
        private static async Task<FatDirectory> CreateAsync(FatTableImage image)
        {
            var device = await image.CreateDeviceAsync();
            var filler = Enumerable.Repeat((byte)'A', (int)image.Info.ClusterCount * FatTableImage.SECTOR_SIZE).ToArray();
            await device.WriteAsync(image.Info.ClusterHeapSector, filler);

            var core = new FatVolumeCore(device, image.Info);
            return Open(core, new FatDirectoryEntry { Path = "/big", Attributes = FatAttributes.Directory, FirstCluster = 2 });
        }

        /// <summary>
        /// A subdirectory, which lies in a directory of its own.
        /// </summary>
        private static FatDirectory Open(FatVolumeCore core, FatDirectoryEntry entry)
        {
            return core.OpenDirectory(new DirectoryItem(entry, 0, 0, 0));
        }

        #endregion
    }
}
