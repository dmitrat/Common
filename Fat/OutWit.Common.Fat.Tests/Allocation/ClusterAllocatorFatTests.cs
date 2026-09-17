using OutWit.Common.Fat.Allocation;
using OutWit.Common.Fat.Exceptions;
using OutWit.Common.Fat.Model;
using OutWit.Common.Fat.Tests.Utils;

namespace OutWit.Common.Fat.Tests.Allocation
{
    [TestFixture]
    public class ClusterAllocatorFatTests
    {
        #region Constants

        private const uint CLUSTERS = 100;

        private const uint LAST_CLUSTER = CLUSTERS + 1;

        #endregion

        #region Allocation Tests

        [Test]
        public async Task NewChainIsContiguousFromTheStartTest()
        {
            var (table, allocator, _) = await CreateAsync(new FatTableImage(FatKind.Fat16, CLUSTERS, 1));

            var runs = await allocator.AllocateAsync(0, 5, 0, true, CancellationToken.None);

            Assert.That(runs, Is.EqualTo(new[] { new ClusterRun(0, 2, 5) }));
            Assert.That(await Values(table, 2, 3, 4, 5, 6, 7), Is.EqualTo(new uint[] { 3, 4, 5, 6, 0xFFFF, 0 }));
        }

        [Test]
        public async Task ChainGrowsRightAfterItsEndTest()
        {
            var (table, allocator, _) = await CreateAsync(new FatTableImage(FatKind.Fat16, CLUSTERS, 1).Chain(40, 41));

            var runs = await allocator.AllocateAsync(41, 3, 2, true, CancellationToken.None);

            Assert.That(runs, Is.EqualTo(new[] { new ClusterRun(2, 42, 3) }));
            Assert.That(await Values(table, 41, 42, 43, 44), Is.EqualTo(new uint[] { 42, 43, 44, 0xFFFF }));
        }

        [TestCase(FatKind.Fat12)]
        [TestCase(FatKind.Fat16)]
        [TestCase(FatKind.Fat32)]
        public async Task TakenClustersAreSkippedTest(FatKind kind)
        {
            var image = new FatTableImage(kind, CLUSTERS, 2).Chain(3).Chain(5);
            var (table, allocator, _) = await CreateAsync(image);

            var runs = await allocator.AllocateAsync(0, 4, 0, true, CancellationToken.None);

            Assert.That(runs, Is.EqualTo(new[] { new ClusterRun(0, 2, 1), new ClusterRun(1, 4, 1), new ClusterRun(2, 6, 2) }));
            Assert.That(await Values(table, 2, 4, 6, 7), Is.EqualTo(new uint[] { 4, 6, 7, image.EndOfChain }));
        }

        [Test]
        public async Task SearchWrapsAroundTheVolumeTest()
        {
            var image = new FatTableImage(FatKind.Fat16, CLUSTERS, 1);
            for (uint cluster = 2; cluster <= LAST_CLUSTER; cluster++)
                image.Set(cluster, cluster == 5 ? 0u : 0xFFFFu);
            var (table, allocator, _) = await CreateAsync(image);

            var runs = await allocator.AllocateAsync(LAST_CLUSTER - 1, 1, 1, true, CancellationToken.None);

            Assert.That(runs, Is.EqualTo(new[] { new ClusterRun(1, 5, 1) }));
            Assert.That(await Values(table, LAST_CLUSTER - 1, 5), Is.EqualTo(new uint[] { 5, 0xFFFF }));
        }

        [Test]
        public async Task NextSearchStartsWhereTheLastEndedTest()
        {
            var (_, allocator, _) = await CreateAsync(new FatTableImage(FatKind.Fat16, CLUSTERS, 1));

            await allocator.AllocateAsync(0, 3, 0, true, CancellationToken.None);
            await allocator.ReleaseAsync(new[] { new ClusterRun(0, 2, 1) }, CancellationToken.None);
            var runs = await allocator.AllocateAsync(0, 1, 0, true, CancellationToken.None);

            Assert.That(runs, Is.EqualTo(new[] { new ClusterRun(0, 5, 1) }));
        }

        [Test]
        public async Task ReleasedClustersAreFreeTest()
        {
            var (table, allocator, _) = await CreateAsync(new FatTableImage(FatKind.Fat16, CLUSTERS, 1).Chain(10, 11, 12, 30));

            await allocator.ReleaseAsync(new[] { new ClusterRun(0, 10, 3), new ClusterRun(3, 30, 1) }, CancellationToken.None);

            Assert.That(await Values(table, 10, 11, 12, 30), Is.EqualTo(new uint[] { 0, 0, 0, 0 }));
        }

        #endregion

        #region Full Volume Tests

        [Test]
        public async Task FullVolumeTakesNothingTest()
        {
            var image = new FatTableImage(FatKind.Fat16, CLUSTERS, 1);
            for (uint cluster = 2; cluster <= LAST_CLUSTER; cluster++)
                image.Set(cluster, cluster is 10 or 20 or 30 ? 0u : 0xFFFFu);
            var (table, allocator, _) = await CreateAsync(image);

            var error = Assert.ThrowsAsync<FatException>(async () => await allocator.AllocateAsync(50, 4, 1, true, CancellationToken.None));

            Assert.That(error!.Kind, Is.EqualTo(FatErrorKind.NoSpace));
            Assert.That(await Values(table, 10, 20, 30, 50), Is.EqualTo(new uint[] { 0, 0, 0, 0xFFFF }));
            Assert.That(allocator.FreeCount, Is.EqualTo(3));
        }

        [Test]
        public async Task KnownShortageFailsAtOnceTest()
        {
            var image = new FatTableImage(FatKind.Fat16, CLUSTERS, 1);
            for (uint cluster = 2; cluster <= LAST_CLUSTER; cluster++)
                image.Set(cluster, cluster is 10 or 20 ? 0u : 0xFFFFu);
            var (_, allocator, _) = await CreateAsync(image);

            Assert.That(await allocator.CountFreeAsync(CancellationToken.None), Is.EqualTo(2));

            var error = Assert.ThrowsAsync<FatException>(async () => await allocator.AllocateAsync(0, 3, 0, true, CancellationToken.None));
            var runs = await allocator.AllocateAsync(0, 2, 0, true, CancellationToken.None);

            Assert.That(error!.Kind, Is.EqualTo(FatErrorKind.NoSpace));
            Assert.That(runs, Is.EqualTo(new[] { new ClusterRun(0, 10, 1), new ClusterRun(1, 20, 1) }));
            Assert.That(allocator.FreeCount, Is.Zero);
        }

        [Test]
        public async Task FailedWriteRollsBackTest()
        {
            var (table, allocator, probe) = await CreateAsync(new FatTableImage(FatKind.Fat16, 5000, 20).Chain(2));
            probe.FailingWrites = 1;

            // 2500 clusters span ten table sectors; the eighth is pushed out of the window, and its write fails.
            Assert.ThrowsAsync<IOException>(async () => await allocator.AllocateAsync(2, 2500, 1, true, CancellationToken.None));

            await table.FlushAsync(CancellationToken.None);
            Assert.That(await Values(table, 2, 3, 300, 2047, 2048), Is.EqualTo(new uint[] { 0xFFFF, 0, 0, 0, 0 }));
        }

        [Test]
        public async Task FailedLinkKeepsTheCountTest()
        {
            var image = new FatTableImage(FatKind.Fat16, 5000, 20).Chain(2);
            for (uint cluster = 3; cluster <= 2047; cluster++)
                image.Set(cluster, 0xFFFF);
            var (table, allocator, probe) = await CreateAsync(image);
            long exact = await allocator.CountFreeAsync(CancellationToken.None);

            // The search for a free cluster runs through nine table sectors and pushes the first
            // out of the window; the link from cluster 2 has to read it back, and that read fails.
            int reads = 0;
            probe.FailRead = (sector, _) => sector == 1 && ++reads == 2;
            Assert.ThrowsAsync<IOException>(async () => await allocator.AllocateAsync(2, 1, 1, true, CancellationToken.None));
            probe.FailRead = null;

            Assert.That(await Values(table, 2, 2048), Is.EqualTo(new uint[] { 0xFFFF, 0 }), "the cluster taken is given back");
            Assert.That(allocator.FreeCount, Is.EqualTo(exact), "a cluster given back is not counted free twice");
            Assert.That(await allocator.CountFreeAsync(CancellationToken.None), Is.EqualTo(exact));
        }

        [Test]
        public async Task CountIsExactTest()
        {
            var (_, allocator, _) = await CreateAsync(new FatTableImage(FatKind.Fat12, CLUSTERS, 1).Chain(2, 3, 50).Set(60, 0xFF7));

            Assert.That(allocator.FreeCount, Is.Null);
            Assert.That(await allocator.CountFreeAsync(CancellationToken.None), Is.EqualTo(CLUSTERS - 4));

            await allocator.AllocateAsync(0, 6, 0, true, CancellationToken.None);
            Assert.That(allocator.FreeCount, Is.EqualTo(CLUSTERS - 10));
        }

        #endregion

        #region FSInfo Tests

        [Test]
        public async Task FsInfoHintsAreReadAndWrittenTest()
        {
            var image = new FatTableImage(FatKind.Fat32, CLUSTERS, 4, fsInfoSector: 0);
            var (_, allocator, probe) = await CreateAsync(image, BlankVolume.FsInfo(512, 50, 40));

            var runs = await allocator.AllocateAsync(0, 2, 0, true, CancellationToken.None);
            Assert.That(runs, Is.EqualTo(new[] { new ClusterRun(0, 40, 2) }));
            Assert.That(allocator.FreeCount, Is.EqualTo(48));

            await allocator.FlushAsync(CancellationToken.None);
            Assert.That(await BlankVolume.ReadFsInfoAsync(probe, 0), Is.EqualTo((48u, 42u)));
        }

        [Test]
        public async Task UnknownFreeCountStaysUnknownTest()
        {
            var image = new FatTableImage(FatKind.Fat32, CLUSTERS, 4, fsInfoSector: 0);
            var (_, allocator, probe) = await CreateAsync(image, BlankVolume.FsInfo(512, 0xFFFFFFFF, 0xFFFFFFFF));

            var runs = await allocator.AllocateAsync(0, 1, 0, true, CancellationToken.None);
            await allocator.FlushAsync(CancellationToken.None);

            Assert.That(runs, Is.EqualTo(new[] { new ClusterRun(0, 2, 1) }));
            Assert.That(allocator.FreeCount, Is.Null);
            Assert.That(await BlankVolume.ReadFsInfoAsync(probe, 0), Is.EqualTo((0xFFFFFFFFu, 3u)));
        }

        [Test]
        public async Task ImpossibleFreeCountIsForgottenTest()
        {
            var image = new FatTableImage(FatKind.Fat32, CLUSTERS, 4, fsInfoSector: 0);
            var (_, allocator, probe) = await CreateAsync(image, BlankVolume.FsInfo(512, CLUSTERS + 1, 7));

            await allocator.AllocateAsync(0, 1, 0, true, CancellationToken.None);
            await allocator.FlushAsync(CancellationToken.None);

            Assert.That(await BlankVolume.ReadFsInfoAsync(probe, 0), Is.EqualTo((0xFFFFFFFFu, 8u)));
        }

        [Test]
        public async Task SectorWithoutSignaturesIsLeftAloneTest()
        {
            var image = new FatTableImage(FatKind.Fat32, CLUSTERS, 4, fsInfoSector: 0);
            var damaged = BlankVolume.FsInfo(512, 50, 40);
            damaged[510] = 0;
            var (_, allocator, probe) = await CreateAsync(image, damaged);

            var runs = await allocator.AllocateAsync(0, 1, 0, true, CancellationToken.None);
            await allocator.FlushAsync(CancellationToken.None);

            Assert.That(runs, Is.EqualTo(new[] { new ClusterRun(0, 2, 1) }));
            Assert.That(probe.Of(BlockDeviceOperation.Write).Select(request => request.Sector), Does.Not.Contain(0L));
        }

        [Test]
        public async Task UnchangedFsInfoIsNotWrittenTest()
        {
            var image = new FatTableImage(FatKind.Fat32, CLUSTERS, 4, fsInfoSector: 0);
            var (_, allocator, probe) = await CreateAsync(image, BlankVolume.FsInfo(512, 50, 40));

            await allocator.FlushAsync(CancellationToken.None);

            Assert.That(probe.Of(BlockDeviceOperation.Write), Is.Empty);
        }

        #endregion

        #region Tools

        private static async Task<(FatTable Table, ClusterAllocatorFat Allocator, BlockDeviceProbe Probe)> CreateAsync(FatTableImage image,
            byte[]? sectorZero = null)
        {
            var device = await image.CreateDeviceAsync();
            if (sectorZero != null)
                await device.WriteAsync(0, sectorZero);

            var probe = new BlockDeviceProbe(device);
            var table = image.CreateTable(probe);
            return (table, new ClusterAllocatorFat(probe, table), probe);
        }

        private static async Task<uint[]> Values(FatTable table, params uint[] clusters)
        {
            var values = new uint[clusters.Length];
            for (int i = 0; i < clusters.Length; i++)
                values[i] = (await table.GetAsync(clusters[i], CancellationToken.None)).Value;
            return values;
        }

        #endregion
    }
}
