using OutWit.Common.Fat.Devices;
using OutWit.Common.Fat.Tests.Utils;

namespace OutWit.Common.Fat.Tests.Devices
{
    [TestFixture]
    public class BlockDeviceCachedTests
    {
        #region Read Tests

        [Test]
        public async Task SecondReadIsAnsweredFromCacheTest()
        {
            var (probe, cache) = Create(100, 16);
            await probe.WriteAsync(3, Sector(0x33));
            probe.Clear();

            var first = new byte[512];
            var second = new byte[512];
            await cache.ReadAsync(3, first);
            await cache.ReadAsync(3, second);

            Assert.That(first, Is.All.EqualTo(0x33));
            Assert.That(second, Is.All.EqualTo(0x33));
            Assert.That(probe.Of(BlockDeviceOperation.Read).Count(), Is.EqualTo(1));
            Assert.That(cache.Hits, Is.EqualTo(1));
            Assert.That(cache.Misses, Is.EqualTo(1));
        }

        [Test]
        public async Task MissesAreFetchedInRunsTest()
        {
            var (probe, cache) = Create(100, 32);
            await cache.ReadAsync(3, new byte[2 * 512]);
            await cache.ReadAsync(7, new byte[512]);
            probe.Clear();

            await cache.ReadAsync(0, new byte[10 * 512]);

            Assert.That(probe.Of(BlockDeviceOperation.Read), Is.EqualTo(new[]
            {
                new BlockDeviceRequest(BlockDeviceOperation.Read, 0, 3),
                new BlockDeviceRequest(BlockDeviceOperation.Read, 5, 2),
                new BlockDeviceRequest(BlockDeviceOperation.Read, 8, 2)
            }));
            Assert.That(cache.CachedCount, Is.EqualTo(10));
        }

        [Test]
        public async Task ReadLargerThanCapacityReturnsAllDataTest()
        {
            var (probe, cache) = Create(100, 4);
            var data = Enumerable.Range(0, 20 * 512).Select(i => (byte)(i / 512)).ToArray();
            await probe.WriteAsync(10, data);

            var read = new byte[20 * 512];
            await cache.ReadAsync(10, read);

            Assert.That(read, Is.EqualTo(data));
            Assert.That(cache.CachedCount, Is.EqualTo(4));
        }

        [Test]
        public async Task LeastRecentlyUsedSectorIsEvictedTest()
        {
            var (probe, cache) = Create(100, 2);
            await cache.ReadAsync(0, new byte[512]);
            await cache.ReadAsync(1, new byte[512]);
            await cache.ReadAsync(0, new byte[512]);
            await cache.ReadAsync(2, new byte[512]);
            probe.Clear();

            await cache.ReadAsync(0, new byte[512]);
            await cache.ReadAsync(1, new byte[512]);

            Assert.That(probe.Of(BlockDeviceOperation.Read), Is.EqualTo(new[] { new BlockDeviceRequest(BlockDeviceOperation.Read, 1, 1) }));
        }

        #endregion

        #region Write Tests

        [Test]
        public async Task WritesWaitForFlushTest()
        {
            var (probe, cache) = Create(100, 16);

            await cache.WriteAsync(5, Sector(0x55));
            var inner = new byte[512];
            await probe.ReadAsync(5, inner);
            var cached = new byte[512];
            await cache.ReadAsync(5, cached);

            Assert.That(probe.Of(BlockDeviceOperation.Write), Is.Empty);
            Assert.That(inner, Is.All.EqualTo(0));
            Assert.That(cached, Is.All.EqualTo(0x55));
            Assert.That(cache.DirtyCount, Is.EqualTo(1));
        }

        [Test]
        public async Task FlushWritesConsecutiveSectorsInOneRequestTest()
        {
            var (probe, cache) = Create(100, 16);
            await cache.WriteAsync(5, Sector(5));
            await cache.WriteAsync(3, Sector(3));
            await cache.WriteAsync(10, Sector(10));
            await cache.WriteAsync(4, Sector(4));
            await cache.WriteAsync(4, Sector(44));

            await cache.FlushAsync();

            Assert.That(probe.Requests, Is.EqualTo(new[]
            {
                new BlockDeviceRequest(BlockDeviceOperation.Write, 3, 3),
                new BlockDeviceRequest(BlockDeviceOperation.Write, 10, 1),
                new BlockDeviceRequest(BlockDeviceOperation.Flush, 0, 0)
            }));
            var read = new byte[3 * 512];
            await probe.ReadAsync(3, read);
            Assert.That(read.Chunk(512).Select(sector => sector[0]), Is.EqualTo(new byte[] { 3, 44, 5 }));
            Assert.That(cache.DirtyCount, Is.Zero);
        }

        [Test]
        public async Task FlushWithNothingDirtyOnlyFlushesInnerTest()
        {
            var (probe, cache) = Create(100, 16);
            await cache.ReadAsync(0, new byte[512]);
            probe.Clear();

            await cache.FlushAsync();

            Assert.That(probe.Requests, Is.EqualTo(new[] { new BlockDeviceRequest(BlockDeviceOperation.Flush, 0, 0) }));
        }

        [Test]
        public async Task EvictingDirtySectorWritesAllDirtySectorsTest()
        {
            var (probe, cache) = Create(100, 2);
            await cache.WriteAsync(7, Sector(7));
            await cache.WriteAsync(8, Sector(8));

            await cache.WriteAsync(20, Sector(20));

            Assert.That(probe.Of(BlockDeviceOperation.Write), Is.EqualTo(new[] { new BlockDeviceRequest(BlockDeviceOperation.Write, 7, 2) }));
            Assert.That(cache.DirtyCount, Is.EqualTo(1));
            var read = new byte[512];
            await cache.ReadAsync(7, read);
            Assert.That(read, Is.All.EqualTo(7));
        }

        [Test]
        public async Task FailedWriteKeepsSectorsDirtyTest()
        {
            var (probe, cache) = Create(100, 16);
            await cache.WriteAsync(1, Sector(1));
            await cache.WriteAsync(2, Sector(2));
            await cache.WriteAsync(9, Sector(9));
            probe.FailingWrites = 1;

            Assert.ThrowsAsync<IOException>(async () => await cache.FlushAsync());
            Assert.That(cache.DirtyCount, Is.EqualTo(3));

            await cache.FlushAsync();
            Assert.That(cache.DirtyCount, Is.Zero);
            var read = new byte[512];
            await probe.ReadAsync(9, read);
            Assert.That(read, Is.All.EqualTo(9));
        }

        [TestCase(512)]
        [TestCase(4096)]
        public async Task LongDirtyRunIsSplitTest(int sectorSize)
        {
            int perRun = BlockDeviceCached.MAX_RUN_BYTES / sectorSize;
            var probe = new BlockDeviceProbe(new BlockDeviceMemory(3 * perRun, sectorSize));
            await using var cache = new BlockDeviceCached(probe, 3 * perRun);
            var data = new byte[(2 * perRun + 5) * sectorSize];
            for (int i = 0; i < data.Length; i++)
                data[i] = (byte)(i / sectorSize);
            await cache.WriteAsync(0, data);

            await cache.FlushAsync();

            Assert.That(probe.Of(BlockDeviceOperation.Write), Is.EqualTo(new[]
            {
                new BlockDeviceRequest(BlockDeviceOperation.Write, 0, perRun),
                new BlockDeviceRequest(BlockDeviceOperation.Write, perRun, perRun),
                new BlockDeviceRequest(BlockDeviceOperation.Write, 2 * perRun, 5)
            }));
            var last = new byte[sectorSize];
            await probe.ReadAsync(2 * perRun + 4, last);
            Assert.That(last, Is.All.EqualTo((byte)(2 * perRun + 4)));
        }

        [Test]
        public async Task ReadOnlyInnerMakesCacheReadOnlyTest()
        {
            var inner = await BlockDeviceMemory.LoadAsync(new MemoryStream(new byte[4096]), isReadOnly: true);
            await using var cache = new BlockDeviceCached(inner);

            Assert.That(cache.IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() => _ = cache.WriteAsync(0, new byte[512]));
        }

        [Test]
        public async Task WriteThatCannotMakeRoomIsNotAppliedTest()
        {
            var (probe, cache) = Create(100, 3);
            await cache.ReadAsync(7, new byte[512]);
            await cache.WriteAsync(8, Sector(8));
            await cache.WriteAsync(9, Sector(9));
            probe.FailWrite = (sector, _) => sector == 8;

            // Room for three sectors means evicting the dirty ones, whose flush fails.
            var three = Enumerable.Range(20, 3).SelectMany(sector => Sector((byte)sector)).ToArray();
            Assert.ThrowsAsync<IOException>(async () => await cache.WriteAsync(20, three));
            probe.FailWrite = null;

            Assert.That(cache.DirtyCount, Is.EqualTo(2), "the sectors that could not be flushed stay dirty");
            await cache.FlushAsync();
            Assert.That(probe.Of(BlockDeviceOperation.Write).Select(request => request.Sector), Has.None.InRange(20L, 22L),
                "nothing of the write reached the device, not even its first sector");
        }

        #endregion

        #region Lifetime Tests

        [Test]
        public async Task DisposeFlushesAndDisposesInnerTest()
        {
            var (probe, cache) = Create(100, 16);
            await cache.WriteAsync(4, Sector(4));

            await cache.DisposeAsync();

            Assert.That(probe.Requests, Is.EqualTo(new[]
            {
                new BlockDeviceRequest(BlockDeviceOperation.Write, 4, 1),
                new BlockDeviceRequest(BlockDeviceOperation.Flush, 0, 0)
            }));
            Assert.That(probe.IsDisposed, Is.True);
        }

        [Test]
        public async Task FailedFlushOnDisposeKeepsDataTest()
        {
            var (probe, cache) = Create(100, 16);
            await cache.WriteAsync(6, Sector(6));
            probe.FailingWrites = 1;

            Assert.ThrowsAsync<IOException>(async () => await cache.DisposeAsync());

            Assert.That(probe.IsDisposed, Is.False);
            Assert.That(cache.DirtyCount, Is.EqualTo(1));
            await cache.DisposeAsync();
            Assert.That(probe.IsDisposed, Is.True);
            Assert.That(probe.Of(BlockDeviceOperation.Write).Last(), Is.EqualTo(new BlockDeviceRequest(BlockDeviceOperation.Write, 6, 1)));
        }

        [Test]
        public async Task FailedInnerFlushOnDisposeIsRetriedTest()
        {
            var (probe, cache) = Create(100, 16);
            await cache.WriteAsync(6, Sector(6));
            probe.FailingFlushes = 1;

            // The dirty sector reaches the device, and then the device's own flush fails.
            Assert.ThrowsAsync<IOException>(async () => await cache.DisposeAsync());

            Assert.That(probe.IsDisposed, Is.False);
            Assert.That(cache.DirtyCount, Is.Zero);
            await cache.DisposeAsync();
            Assert.That(probe.IsDisposed, Is.True);
            Assert.That(probe.Of(BlockDeviceOperation.Flush).Count(), Is.EqualTo(2), "the retried disposal flushes again, though nothing is dirty");
        }

        [Test]
        public async Task LeaveOpenKeepsInnerOpenTest()
        {
            var probe = new BlockDeviceProbe(new BlockDeviceMemory(10));

            await new BlockDeviceCached(probe, leaveOpen: true).DisposeAsync();

            Assert.That(probe.IsDisposed, Is.False);
            await probe.DisposeAsync();
        }

        [Test]
        public void InvalidArgumentsAreRejectedTest()
        {
            Assert.Throws<ArgumentNullException>(() => _ = new BlockDeviceCached(null!));
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new BlockDeviceCached(new BlockDeviceMemory(10), 0));
        }

        #endregion

        #region Workload Tests

        [TestCase(1, 1)]
        [TestCase(3, 2)]
        [TestCase(8, 3)]
        [TestCase(64, 4)]
        public async Task RandomWorkloadMatchesPlainDeviceTest(int capacity, int seed)
        {
            const int SECTORS = 48;
            var random = new System.Random(seed);
            var inner = new BlockDeviceMemory(SECTORS);
            await using var cache = new BlockDeviceCached(inner, capacity, leaveOpen: true);
            await using var reference = new BlockDeviceMemory(SECTORS);

            for (int step = 0; step < 2000; step++)
            {
                long sector = random.Next(SECTORS);
                int count = random.Next(1, (int)Math.Min(6, SECTORS - sector) + 1);
                int action = random.Next(10);

                if (action < 5)
                {
                    var data = new byte[count * 512];
                    random.NextBytes(data);
                    await cache.WriteAsync(sector, data);
                    await reference.WriteAsync(sector, data);
                }
                else if (action < 9)
                {
                    var fromCache = new byte[count * 512];
                    var expected = new byte[count * 512];
                    await cache.ReadAsync(sector, fromCache);
                    await reference.ReadAsync(sector, expected);
                    Assert.That(fromCache, Is.EqualTo(expected), $"step {step}, sectors {sector}+{count}");
                }
                else
                {
                    await cache.FlushAsync();
                }

                Assert.That(cache.CachedCount, Is.LessThanOrEqualTo(capacity));
            }

            await cache.FlushAsync();
            var flushed = new byte[SECTORS * 512];
            var wanted = new byte[SECTORS * 512];
            await inner.ReadAsync(0, flushed);
            await reference.ReadAsync(0, wanted);
            Assert.That(flushed, Is.EqualTo(wanted));
        }

        #endregion

        #region Tools

        private static (BlockDeviceProbe Probe, BlockDeviceCached Cache) Create(long sectors, int capacity)
        {
            var probe = new BlockDeviceProbe(new BlockDeviceMemory(sectors));
            return (probe, new BlockDeviceCached(probe, capacity));
        }

        private static byte[] Sector(byte value)
        {
            return Enumerable.Repeat(value, 512).ToArray();
        }

        #endregion
    }
}
