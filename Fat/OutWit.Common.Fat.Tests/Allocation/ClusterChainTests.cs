using OutWit.Common.Fat.Allocation;
using OutWit.Common.Fat.Tests.Utils;

namespace OutWit.Common.Fat.Tests.Allocation
{
    [TestFixture]
    public class ClusterChainTests
    {
        #region Run Tests

        [Test]
        public async Task ContiguousChainIsOneRunTest()
        {
            var chain = await CreateAsync(new FatTableImage(FatKind.Fat16, 100, 1).Chain(10, 11, 12), 10);

            var runs = await chain.ReadAllAsync(CancellationToken.None);

            Assert.That(runs, Is.EqualTo(new[] { new ClusterRun(0, 10, 3) }));
            Assert.That(chain.IsComplete, Is.True);
        }

        [Test]
        public async Task FragmentedChainKeepsItsOrderTest()
        {
            var chain = await CreateAsync(new FatTableImage(FatKind.Fat12, 100, 1).Chain(10, 11, 20, 21, 5), 10);

            var runs = await chain.ReadAllAsync(CancellationToken.None);
            var third = await chain.FindAsync(3, 1, CancellationToken.None);

            Assert.That(runs, Is.EqualTo(new[] { new ClusterRun(0, 10, 2), new ClusterRun(2, 20, 2), new ClusterRun(4, 5, 1) }));
            Assert.That(third, Is.EqualTo(new ClusterRun(2, 20, 2)));
            Assert.That(third!.Value.ClusterAt(3), Is.EqualTo(21));
            Assert.That(third.Value.EndIndex, Is.EqualTo(4));
            Assert.That(third.Value.LastCluster, Is.EqualTo(21));
        }

        [Test]
        public async Task SingleClusterChainTest()
        {
            var chain = await CreateAsync(new FatTableImage(FatKind.Fat32, 100, 1).Chain(7), 7);

            Assert.That(await chain.FindAsync(0, 5, CancellationToken.None), Is.EqualTo(new ClusterRun(0, 7, 1)));
            Assert.That(await chain.FindAsync(1, 1, CancellationToken.None), Is.Null);
        }

        [Test]
        public async Task PositionPastTheEndIsNotFoundTest()
        {
            var chain = await CreateAsync(new FatTableImage(FatKind.Fat16, 100, 1).Chain(10, 30), 10);

            Assert.That(await chain.FindAsync(2, 1, CancellationToken.None), Is.Null);
            Assert.That(await chain.FindAsync(1, 1, CancellationToken.None), Is.EqualTo(new ClusterRun(1, 30, 1)));
        }

        [Test]
        public async Task ChainIsFollowedOnlyAsFarAsAskedTest()
        {
            var clusters = Enumerable.Range(2, 1000).Select(c => (uint)c).ToArray();
            var chain = await CreateAsync(new FatTableImage(FatKind.Fat16, 2000, 8).Chain(clusters), 2);

            var run = await chain.FindAsync(3, 10, CancellationToken.None);

            Assert.That(chain.KnownCount, Is.EqualTo(13));
            Assert.That(chain.IsComplete, Is.False);
            Assert.That(run, Is.EqualTo(new ClusterRun(0, 2, 13)));
        }

        #endregion

        #region Corruption Tests

        [Test]
        public async Task LoopIsCaughtAtTheFirstRepeatTest()
        {
            var image = new FatTableImage(FatKind.Fat16, 60000, 236).Set(10, 11).Set(11, 10);
            var probe = new BlockDeviceProbe(await image.CreateDeviceAsync());
            var chain = new ClusterChain(image.CreateTable(probe), 10);

            var error = Assert.ThrowsAsync<FatException>(async () => await chain.ReadAllAsync(CancellationToken.None));

            Assert.That(error!.Message, Does.Contain("comes back to cluster 10"));
            Assert.That(chain.KnownCount, Is.EqualTo(2));
            Assert.That(probe.Requests, Has.Count.EqualTo(1));
        }

        [TestCase(new uint[] { 10, 11, 12, 20, 11 }, 11u)]
        [TestCase(new uint[] { 7, 7 }, 7u)]
        [TestCase(new uint[] { 12, 13, 5, 6, 7, 8, 9, 10, 11, 12 }, 12u)]
        [TestCase(new uint[] { 30, 40, 41, 42, 50, 41 }, 41u)]
        public async Task ChainComingBackIsCorruptTest(uint[] clusters, uint repeated)
        {
            var image = new FatTableImage(FatKind.Fat16, 100, 1);
            for (int i = 0; i < clusters.Length - 1; i++)
                image.Set(clusters[i], clusters[i + 1]);
            var chain = await CreateAsync(image, clusters[0]);

            var error = Assert.ThrowsAsync<FatException>(async () => await chain.ReadAllAsync(CancellationToken.None));

            Assert.That(error!.Kind, Is.EqualTo(FatErrorKind.Corrupt));
            Assert.That(error.Message, Does.Contain($"comes back to cluster {repeated}"));
        }

        [Test]
        public async Task ChainThroughEveryClusterIsAcceptedTest()
        {
            var clusters = Enumerable.Range(2, 100).Reverse().Select(c => (uint)c).ToArray();
            var chain = await CreateAsync(new FatTableImage(FatKind.Fat16, 100, 1).Chain(clusters), clusters[0]);

            var runs = await chain.ReadAllAsync(CancellationToken.None);

            Assert.That(runs, Has.Count.EqualTo(100));
            Assert.That(chain.KnownCount, Is.EqualTo(100));
        }

        [Test]
        public async Task LoopIsCorruptTest()
        {
            var image = new FatTableImage(FatKind.Fat16, 50, 1).Set(10, 11).Set(11, 12).Set(12, 10);
            var chain = await CreateAsync(image, 10);

            var error = Assert.ThrowsAsync<FatException>(async () => await chain.ReadAllAsync(CancellationToken.None));

            Assert.That(error!.Kind, Is.EqualTo(FatErrorKind.Corrupt));
            Assert.That(error.Message, Does.Contain("loops"));
        }

        [TestCase(0x0000u, "free")]
        [TestCase(0xFFF7u, "bad")]
        [TestCase(0x0001u, "invalid")]
        [TestCase(0x0099u, "invalid")]
        public async Task ChainIntoWrongEntryIsCorruptTest(uint value, string kind)
        {
            var image = new FatTableImage(FatKind.Fat16, 100, 1).Set(10, 11).Set(11, value);
            var chain = await CreateAsync(image, 10);

            var error = Assert.ThrowsAsync<FatException>(async () => await chain.FindAsync(5, 1, CancellationToken.None));

            Assert.That(error!.Kind, Is.EqualTo(FatErrorKind.Corrupt));
            Assert.That(error.Message, Does.Contain(kind).And.Contain("cluster 11"));
        }

        [TestCase(0u)]
        [TestCase(1u)]
        [TestCase(102u)]
        public async Task StartOutsideVolumeIsCorruptTest(uint first)
        {
            var image = new FatTableImage(FatKind.Fat16, 100, 1);
            var table = image.CreateTable(await image.CreateDeviceAsync());

            var error = Assert.Throws<FatException>(() => _ = new ClusterChain(table, first));

            Assert.That(error!.Kind, Is.EqualTo(FatErrorKind.Corrupt));
        }

        #endregion

        #region Edit Tests

        [Test]
        public async Task ChainFromRunsIsCompleteAndMergedTest()
        {
            var table = await CreateTableAsync();

            var chain = ClusterChain.FromRuns(table, new[] { new ClusterRun(0, 10, 2), new ClusterRun(2, 12, 3), new ClusterRun(5, 40, 1) });

            Assert.That(chain.IsComplete, Is.True);
            Assert.That(await chain.ReadAllAsync(CancellationToken.None), Is.EqualTo(new[] { new ClusterRun(0, 10, 5), new ClusterRun(5, 40, 1) }));
            Assert.That(chain.LastCluster, Is.EqualTo(40));
        }

        [Test]
        public async Task AppendedRunsContinueTheChainTest()
        {
            var chain = await CreateAsync(new FatTableImage(FatKind.Fat16, 100, 1).Chain(10, 11), 10);
            await chain.ReadAllAsync(CancellationToken.None);

            chain.Append(new[] { new ClusterRun(2, 12, 2), new ClusterRun(4, 30, 1), new ClusterRun(5, 31, 1) });

            Assert.That(await chain.ReadAllAsync(CancellationToken.None), Is.EqualTo(new[] { new ClusterRun(0, 10, 4), new ClusterRun(4, 30, 2) }));
            Assert.That((await chain.FindAsync(5, 1, CancellationToken.None))!.Value.ClusterAt(5), Is.EqualTo(31));
        }

        [Test]
        public async Task IncompleteChainCannotGrowTest()
        {
            var chain = await CreateAsync(new FatTableImage(FatKind.Fat16, 100, 1).Chain(10, 11), 10);

            Assert.Throws<InvalidOperationException>(() => chain.Append(new[] { new ClusterRun(2, 12, 1) }));
        }

        [Test]
        public async Task TruncateReturnsTheRestInOrderTest()
        {
            var chain = await CreateAsync(new FatTableImage(FatKind.Fat16, 100, 1).Chain(10, 11, 12, 20, 21, 5), 10);
            await chain.ReadAllAsync(CancellationToken.None);

            var removed = chain.Truncate(2);

            Assert.That(removed, Is.EqualTo(new[] { new ClusterRun(2, 12, 1), new ClusterRun(3, 20, 2), new ClusterRun(5, 5, 1) }));
            Assert.That(chain.KnownCount, Is.EqualTo(2));
            Assert.That(chain.LastCluster, Is.EqualTo(11));
            Assert.That(await chain.FindAsync(2, 1, CancellationToken.None), Is.Null);
        }

        [Test]
        public async Task TruncatedChainGrowsAgainTest()
        {
            var chain = await CreateAsync(new FatTableImage(FatKind.Fat16, 100, 1).Chain(10, 20, 30), 10);
            await chain.ReadAllAsync(CancellationToken.None);

            chain.Truncate(2);
            chain.Append(new[] { new ClusterRun(2, 21, 1) });

            Assert.That(await chain.ReadAllAsync(CancellationToken.None), Is.EqualTo(new[] { new ClusterRun(0, 10, 1), new ClusterRun(1, 20, 2) }));
        }

        [TestCase(0)]
        [TestCase(4)]
        public async Task TruncateOutsideTheChainIsRejectedTest(int count)
        {
            var chain = await CreateAsync(new FatTableImage(FatKind.Fat16, 100, 1).Chain(10, 11, 12), 10);
            await chain.ReadAllAsync(CancellationToken.None);

            Assert.Throws<InvalidOperationException>(() => chain.Truncate(count));
        }

        #endregion

        #region Tools

        private static async Task<ClusterChain> CreateAsync(FatTableImage image, uint first)
        {
            return new ClusterChain(image.CreateTable(await image.CreateDeviceAsync()), first);
        }

        private static async Task<FatTable> CreateTableAsync()
        {
            var image = new FatTableImage(FatKind.Fat16, 100, 1);
            return image.CreateTable(await image.CreateDeviceAsync());
        }

        #endregion
    }
}
