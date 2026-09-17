using OutWit.Common.Fat.Allocation;
using OutWit.Common.Fat.Devices;
using OutWit.Common.Fat.ExFat;
using OutWit.Common.Fat.Tests.Utils;

namespace OutWit.Common.Fat.Tests.ExFat
{
    [TestFixture]
    public class ExFatAllocatorTests
    {
        #region Constants

        /// <summary>The first free cluster of a blank volume of 512-byte clusters.</summary>
        private const uint FIRST_FREE = 16;

        #endregion

        #region Allocation Tests

        [Test]
        public async Task ClustersWithoutAChainLeaveTheTableAloneTest()
        {
            var (probe, core, _) = await TestVolumes.CoreAsync(FatKind.ExFat, 300);
            probe.Clear();

            var runs = await core.Allocator.AllocateAsync(0, 5, 0, false, CancellationToken.None);
            await core.Allocator.FlushAsync(CancellationToken.None);

            Assert.That(runs, Is.EqualTo(new[] { new ClusterRun(0, FIRST_FREE, 5) }));
            Assert.That(TableWrites(probe, core), Is.Empty);
            Assert.That(await UsedAsync(core, FIRST_FREE, FIRST_FREE + 4), Is.All.True);
        }

        [Test]
        public async Task ChainedClustersAreLinkedTest()
        {
            var (_, core, _) = await TestVolumes.CoreAsync(FatKind.ExFat, 300);
            var first = await core.Allocator.AllocateAsync(0, 2, 0, true, CancellationToken.None);

            var more = await core.Allocator.AllocateAsync(first[0].LastCluster, 2, 2, true, CancellationToken.None);

            Assert.That(more, Is.EqualTo(new[] { new ClusterRun(2, FIRST_FREE + 2, 2) }));
            Assert.That(await new ClusterChain(core.Table, FIRST_FREE).ReadAllAsync(CancellationToken.None),
                Is.EqualTo(new[] { new ClusterRun(0, FIRST_FREE, 4) }));
        }

        [Test]
        public async Task ReleasedClustersAreFreedInTheBitmapOnlyTest()
        {
            var (probe, core, _) = await TestVolumes.CoreAsync(FatKind.ExFat, 300);
            var runs = await core.Allocator.AllocateAsync(0, 3, 0, true, CancellationToken.None);
            await core.Allocator.FlushAsync(CancellationToken.None);
            probe.Clear();

            await core.Allocator.ReleaseAsync(runs, CancellationToken.None);
            await core.Allocator.FlushAsync(CancellationToken.None);

            Assert.That(TableWrites(probe, core), Is.Empty);
            Assert.That(await UsedAsync(core, FIRST_FREE, FIRST_FREE + 2), Is.All.False);
            Assert.That((await core.Table.GetAsync(FIRST_FREE, CancellationToken.None)).Next, Is.EqualTo(FIRST_FREE + 1));
        }

        [Test]
        public async Task LinkWritesAChainThroughRunsTest()
        {
            var (_, core, _) = await TestVolumes.CoreAsync(FatKind.ExFat, 300);
            var runs = new List<ClusterRun> { new(0, 40, 2), new(2, 30, 1), new(3, 50, 3) };

            await core.Allocator.LinkAsync(runs, CancellationToken.None);

            Assert.That(await new ClusterChain(core.Table, 40).ReadAllAsync(CancellationToken.None), Is.EqualTo(runs));
        }

        [Test]
        public async Task SearchComesRoundPastTheLastClusterTest()
        {
            var (_, core, _) = await TestVolumes.CoreAsync(FatKind.ExFat, 300);
            await core.Allocator.AllocateAsync(0, 284, 0, false, CancellationToken.None);

            var runs = await core.Allocator.AllocateAsync(299, 2, 0, false, CancellationToken.None);

            Assert.That(runs, Is.EqualTo(new[] { new ClusterRun(0, 300, 2) }));
            Assert.ThrowsAsync<FatException>(async () => await core.Allocator.AllocateAsync(301, 1, 0, false, CancellationToken.None));
        }

        [Test]
        public async Task FailedChainEndTakesNothingTest()
        {
            const uint CLUSTERS = 1500;
            var bitmap = new byte[(CLUSTERS + 7) / 8];
            bitmap[0] = 0b0000_0111;
            var volume = new ExFatTestVolume(CLUSTERS, fatSectors: 12).WithRoot(bitmap);
            var probe = await volume.CreateAsync();
            var core = volume.Core(probe);
            long exact = await core.Allocator.CountFreeAsync(CancellationToken.None);

            // Eight later table sectors push the first out of the window; ending the chain at
            // the first free cluster has to read it back, and that read fails.
            for (uint k = 1; k <= 8; k++)
                await core.Table.GetAsync(128 * k + 2, CancellationToken.None);
            probe.FailRead = (sector, _) => sector == 1;

            Assert.ThrowsAsync<IOException>(async () => await core.Allocator.AllocateAsync(0, 1, 0, true, CancellationToken.None));
            probe.FailRead = null;

            Assert.That(await UsedAsync(core, 5, 5), Is.EqualTo(new[] { false }), "a cluster whose chain end could not be written is not taken");
            Assert.That(core.Allocator.FreeCount, Is.EqualTo(exact));
            Assert.That(await core.Allocator.CountFreeAsync(CancellationToken.None), Is.EqualTo(exact));
        }

        #endregion

        #region Space Tests

        [Test]
        public async Task FullVolumeTakesNothingTest()
        {
            var (_, core, _) = await TestVolumes.CoreAsync(FatKind.ExFat, 300);
            long free = await core.Allocator.CountFreeAsync(CancellationToken.None);

            var error = Assert.ThrowsAsync<FatException>(async () =>
                await core.Allocator.AllocateAsync(0, free + 1, 0, false, CancellationToken.None));
            var runs = await core.Allocator.AllocateAsync(0, free, 0, false, CancellationToken.None);

            Assert.That(error!.Kind, Is.EqualTo(FatErrorKind.NoSpace));
            Assert.That(runs.Sum(run => run.Count), Is.EqualTo(free));
            Assert.That(core.Allocator.FreeCount, Is.Zero);
        }

        [Test]
        public async Task FailedSearchRollsBackTest()
        {
            var (_, core, _) = await TestVolumes.CoreAsync(FatKind.ExFat, 300);
            var existing = await core.Allocator.AllocateAsync(0, 1, 0, true, CancellationToken.None);

            var error = Assert.ThrowsAsync<FatException>(async () =>
                await core.Allocator.AllocateAsync(existing[0].FirstCluster, 1000, 1, true, CancellationToken.None));

            Assert.That(error!.Kind, Is.EqualTo(FatErrorKind.NoSpace));
            Assert.That((core.Allocator.FreeCount, core.Allocator.IsCountExact), Is.EqualTo(((long?)285, true)), "counted when the search found none");
            Assert.That(await core.Allocator.CountFreeAsync(CancellationToken.None), Is.EqualTo(285));
            Assert.That((await core.Table.GetAsync(existing[0].FirstCluster, CancellationToken.None)).Kind, Is.EqualTo(FatTableEntryKind.End));
        }

        [Test]
        public async Task CountIsReadFromTheBitmapTest()
        {
            var (_, core, _) = await TestVolumes.CoreAsync(FatKind.ExFat, 300);

            Assert.That(core.Allocator.FreeCount, Is.Null);
            Assert.That(await core.Allocator.CountFreeAsync(CancellationToken.None), Is.EqualTo(300 - 14));
            Assert.That(core.Allocator.IsCountExact, Is.True);
        }

        #endregion

        #region Tools

        private static IEnumerable<BlockDeviceRequest> TableWrites(BlockDeviceProbe probe, FatVolumeCore core)
        {
            return probe.Of(BlockDeviceOperation.Write)
                .Where(request => request.Sector < core.Info.FatOffset + core.Info.FatSectors && request.Sector + request.Count > core.Info.FatOffset);
        }

        private static async Task<List<bool>> UsedAsync(FatVolumeCore core, uint first, uint last)
        {
            var bitmap = ((ExFatAllocator)core.Allocator).Bitmap!;
            var used = new List<bool>();
            for (uint cluster = first; cluster <= last; cluster++)
                used.Add(await bitmap.IsUsedAsync(cluster, CancellationToken.None));
            return used;
        }

        #endregion
    }
}
