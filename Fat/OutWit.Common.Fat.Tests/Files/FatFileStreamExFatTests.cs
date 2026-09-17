using OutWit.Common.Fat.Allocation;
using OutWit.Common.Fat.Devices;
using OutWit.Common.Fat.Tests.Utils;

namespace OutWit.Common.Fat.Tests.Files
{
    /// <summary>
    /// What writing a file does on exFAT that it does not on FAT: clusters without a table
    /// chain, and a valid length apart from the length.
    /// </summary>
    [TestFixture]
    public class FatFileStreamExFatTests
    {
        #region Constants

        private const int CLUSTER = 512;

        /// <summary>The first free cluster of a blank volume of 512-byte clusters.</summary>
        private const uint FIRST_FREE = 16;

        #endregion

        #region Chain Tests

        [Test]
        public async Task FileWrittenInOnePieceHasNoChainTest()
        {
            var (_, volume) = await TestVolumes.BlankAsync(FatKind.ExFat);
            await using var _ = volume;

            await volume.WriteAllBytesAsync("/a.bin", Pattern(5 * CLUSTER));

            var item = (await volume.GetItemAsync("/a.bin", CancellationToken.None))!;
            Assert.That(item.IsContiguous, Is.True);
            Assert.That(await volume.GetClusterRunsAsync("/a.bin", CancellationToken.None), Is.EqualTo(new[] { new ClusterRun(0, FIRST_FREE, 5) }));
            Assert.That((await volume.Core.Table.GetAsync(FIRST_FREE, CancellationToken.None)).Kind, Is.EqualTo(FatTableEntryKind.Free));
        }

        [Test]
        public async Task FileThatCannotGrowInPlaceGetsAChainTest()
        {
            var (disk, volume) = await TestVolumes.BlankAsync(FatKind.ExFat);
            await volume.WriteAllBytesAsync("/a.bin", Pattern(2 * CLUSTER));
            await volume.WriteAllBytesAsync("/b.bin", Pattern(CLUSTER));

            await using (var stream = await volume.OpenAsync("/a.bin", FileMode.Append, FileAccess.Write))
                await stream.WriteAsync(Pattern(CLUSTER + 1));
            await volume.DisposeAsync();

            await using var reopened = await TestVolumes.RemountAsync(disk);
            var item = (await reopened.GetItemAsync("/a.bin", CancellationToken.None))!;
            Assert.That(item.IsContiguous, Is.False);
            Assert.That(await reopened.GetClusterRunsAsync("/a.bin", CancellationToken.None),
                Is.EqualTo(new[] { new ClusterRun(0, FIRST_FREE, 2), new ClusterRun(2, FIRST_FREE + 3, 2) }));
            Assert.That(await reopened.ReadAllBytesAsync("/a.bin"), Is.EqualTo(Pattern(2 * CLUSTER).Concat(Pattern(CLUSTER + 1))));
            await VolumeConsistency.AssertAsync(reopened);
        }

        [Test]
        public async Task ShrinkingAContiguousFileLeavesTheTableAloneTest()
        {
            var (disk, volume) = await TestVolumes.BlankAsync(FatKind.ExFat);
            await volume.WriteAllBytesAsync("/a.bin", Pattern(5 * CLUSTER));
            await volume.DisposeAsync();
            var probe = new BlockDeviceProbe(disk);
            await using var mounted = await FatVolume.MountAsync(probe, TestVolumes.Options(null));

            await using (var stream = await mounted.OpenAsync("/a.bin", FileMode.Open, FileAccess.Write))
                await stream.SetLengthAsync(CLUSTER + 3);
            await mounted.FlushAsync();

            var info = mounted.Info;
            Assert.That(probe.Of(BlockDeviceOperation.Write).Where(r => r.Sector >= info.FatOffset && r.Sector < info.FatOffset + info.FatSectors), Is.Empty);
            Assert.That(await mounted.GetClusterRunsAsync("/a.bin", CancellationToken.None), Is.EqualTo(new[] { new ClusterRun(0, FIRST_FREE, 2) }));
            await VolumeConsistency.AssertAsync(mounted);
        }

        [Test]
        public async Task EmptiedFileHasNoClustersAndNoFlagTest()
        {
            var (_, volume) = await TestVolumes.BlankAsync(FatKind.ExFat);
            await using var _ = volume;
            await volume.WriteAllBytesAsync("/a.bin", Pattern(3 * CLUSTER));

            await volume.WriteAllBytesAsync("/a.bin", Array.Empty<byte>());

            var item = (await volume.GetItemAsync("/a.bin", CancellationToken.None))!;
            Assert.That((item.Entry.FirstCluster, item.DataLength, item.ValidLength, item.IsContiguous), Is.EqualTo((0u, 0L, 0L, false)));
            await VolumeConsistency.AssertAsync(volume);
        }

        [Test]
        public async Task FailedChainWriteGivesBackTheNewClustersTest()
        {
            var probe = new BlockDeviceProbe(await BlankVolumeExFat.CreateAsync(1500));
            await using var volume = await FatVolume.MountAsync(probe, TestVolumes.Options(null));
            await volume.WriteAllBytesAsync("/a.bin", Pattern(2 * CLUSTER));
            await volume.WriteAllBytesAsync("/b.bin", Pattern(CLUSTER));
            long free = await volume.CountFreeClustersAsync();

            // b.bin sits right after a.bin, so a.bin cannot grow in place and needs a chain. Eight
            // later table sectors push the first out of the window; writing the chain has to read
            // it back, and that read fails.
            for (uint k = 1; k <= 8; k++)
                await volume.Core.Table.GetAsync(128 * k + 2, CancellationToken.None);
            probe.FailRead = (sector, _) => sector == volume.Info.FatOffset;
            await using (var a = await volume.OpenAsync("/a.bin", FileMode.Append, FileAccess.Write))
            {
                Assert.ThrowsAsync<IOException>(async () => await a.WriteAsync(Pattern(CLUSTER)));
                probe.FailRead = null;
            }

            Assert.That((await volume.GetEntryAsync("/a.bin"))!.Length, Is.EqualTo(2 * CLUSTER));
            Assert.That(await volume.CountFreeClustersAsync(), Is.EqualTo(free), "the clusters taken for the chain are given back");
            await VolumeConsistency.AssertAsync(volume);
        }

        #endregion

        #region Valid Length Tests

        [Test]
        public async Task LongerLengthIsNotWrittenTest()
        {
            var (disk, volume) = await TestVolumes.BlankAsync(FatKind.ExFat);
            await volume.WriteAllBytesAsync("/a.bin", Pattern(100));
            await volume.DisposeAsync();
            var probe = new BlockDeviceProbe(disk);
            var mounted = await FatVolume.MountAsync(probe, TestVolumes.Options(null));

            await using (var stream = await mounted.OpenAsync("/a.bin", FileMode.Open, FileAccess.Write))
                await stream.SetLengthAsync(3 * CLUSTER);
            await mounted.DisposeAsync();

            long firstData = mounted.Info.ClusterHeapSector + (FIRST_FREE - 2);
            Assert.That(probe.Of(BlockDeviceOperation.Write).Where(r => r.Sector >= firstData), Is.Empty);
            await using var reopened = await TestVolumes.RemountAsync(disk);
            var item = (await reopened.GetItemAsync("/a.bin", CancellationToken.None))!;
            Assert.That((item.DataLength, item.ValidLength), Is.EqualTo((3L * CLUSTER, 100L)));
            Assert.That(await reopened.ReadAllBytesAsync("/a.bin"), Is.EqualTo(Pattern(100).Concat(new byte[3 * CLUSTER - 100])));
        }

        [Test]
        public async Task WritePastTheValidLengthZeroesTheGapTest()
        {
            var (disk, volume) = await TestVolumes.BlankAsync(FatKind.ExFat);
            await using var _ = volume;
            await using (var stream = await volume.OpenAsync("/a.bin", FileMode.CreateNew, FileAccess.ReadWrite))
            {
                await stream.WriteAsync(Pattern(100));
                await stream.SetLengthAsync(3 * CLUSTER);
                await stream.FlushAsync();

                var junk = new byte[3 * CLUSTER];
                await disk.ReadAsync(volume.Core.ClusterSector(FIRST_FREE), junk);
                junk.AsSpan(100).Fill(0xAA);
                await disk.WriteAsync(volume.Core.ClusterSector(FIRST_FREE), junk);

                stream.Position = 2 * CLUSTER + 5;
                await stream.WriteAsync(Pattern(10));
            }

            var item = (await volume.GetItemAsync("/a.bin", CancellationToken.None))!;
            var expected = new byte[3 * CLUSTER];
            Pattern(100).CopyTo(expected, 0);
            Pattern(10).CopyTo(expected, 2 * CLUSTER + 5);
            Assert.That(item.ValidLength, Is.EqualTo(2 * CLUSTER + 15));
            Assert.That(await volume.ReadAllBytesAsync("/a.bin"), Is.EqualTo(expected));
            var stored = new byte[CLUSTER];
            await disk.ReadAsync(volume.Core.ClusterSector(FIRST_FREE + 2), stored);
            Assert.That(stored.AsSpan(0, 5).ToArray(), Is.All.Zero, "the gap is written");
            Assert.That(stored.AsSpan(15).ToArray(), Is.All.EqualTo(0xAA), "past the valid length nothing is");
        }

        [Test]
        public async Task ShorterLengthCutsTheValidLengthTest()
        {
            var (_, volume) = await TestVolumes.BlankAsync(FatKind.ExFat);
            await using var _ = volume;
            await using (var stream = await volume.OpenAsync("/a.bin", FileMode.CreateNew, FileAccess.Write))
            {
                await stream.WriteAsync(Pattern(1000));
                await stream.SetLengthAsync(600);
                await stream.SetLengthAsync(900);
            }

            var item = (await volume.GetItemAsync("/a.bin", CancellationToken.None))!;
            Assert.That((item.DataLength, item.ValidLength), Is.EqualTo((900L, 600L)));
            Assert.That(await volume.ReadAllBytesAsync("/a.bin"), Is.EqualTo(Pattern(600).Concat(new byte[300])));
        }

        #endregion

        #region Size Tests

        [Test]
        public async Task FileMayPassFourGiBTest()
        {
            var disk = await BlankVolumeExFat.CreateAsync(clusters: 131072, sectorsPerCluster: 128);
            long length = 5L << 30;
            await using (var volume = await FatVolume.MountAsync(disk, TestVolumes.Options(null)))
            {
                await using var stream = await volume.OpenAsync("/big.bin", FileMode.CreateNew, FileAccess.ReadWrite);
                await stream.WriteAsync(new byte[] { 1, 2, 3 });
                await stream.SetLengthAsync(length);
            }

            await using var reopened = await TestVolumes.RemountAsync(disk);
            var entry = (await reopened.GetEntryAsync("/big.bin"))!;
            await using var read = await reopened.OpenReadAsync("/big.bin");
            var head = new byte[4];
            await read.ReadExactlyAsync(head);
            read.Position = length - 2;
            var tail = new byte[3];
            int last = await read.ReadAsync(tail);
            Assert.That(entry.Length, Is.EqualTo(length));
            Assert.That(head, Is.EqualTo(new byte[] { 1, 2, 3, 0 }));
            Assert.That((last, tail[0], tail[1]), Is.EqualTo((2, (byte)0, (byte)0)));
            Assert.That(await reopened.GetClusterRunsAsync("/big.bin", CancellationToken.None), Has.Count.EqualTo(1));
        }

        [Test]
        public async Task FileLongerThanTheVolumeIsTooLargeTest()
        {
            var (_, volume) = await TestVolumes.BlankAsync(FatKind.ExFat);
            await using var _ = volume;
            await using var stream = await volume.OpenAsync("/a.bin", FileMode.CreateNew, FileAccess.Write);

            var error = Assert.ThrowsAsync<FatException>(async () => await stream.SetLengthAsync(301L * CLUSTER));

            Assert.That(error!.Kind, Is.EqualTo(FatErrorKind.TooLarge));
        }

        #endregion

        #region Tools

        private static byte[] Pattern(int size)
        {
            return FatVolumeOracleTests.Pattern("exf", size);
        }

        #endregion
    }
}
