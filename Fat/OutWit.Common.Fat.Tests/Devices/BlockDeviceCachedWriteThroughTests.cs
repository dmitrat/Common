using OutWit.Common.Fat.Devices;
using OutWit.Common.Fat.Tests.Utils;

namespace OutWit.Common.Fat.Tests.Devices
{
    /// <summary>
    /// <see cref="BlockDeviceCached"/> created to write through.
    /// </summary>
    [TestFixture]
    public class BlockDeviceCachedWriteThroughTests
    {
        #region Write Tests

        [Test]
        public async Task WritesGoThroughInTheOrderMadeTest()
        {
            var (probe, cache) = Create(64, 16);

            await cache.WriteAsync(40, Sectors(2, 1));
            await cache.WriteAsync(3, Sectors(1, 2));
            await cache.WriteAsync(41, Sectors(1, 3));

            Assert.That(probe.Of(BlockDeviceOperation.Write), Is.EqualTo(new[]
            {
                new BlockDeviceRequest(BlockDeviceOperation.Write, 40, 2),
                new BlockDeviceRequest(BlockDeviceOperation.Write, 3, 1),
                new BlockDeviceRequest(BlockDeviceOperation.Write, 41, 1)
            }));
            Assert.That(cache.DirtyCount, Is.Zero);
            Assert.That(cache.WritesThrough, Is.True);
        }

        [Test]
        public async Task WrittenSectorsAreReadFromMemoryTest()
        {
            var (probe, cache) = Create(64, 16);
            await cache.ReadAsync(5, new byte[512]);
            await cache.WriteAsync(5, Sectors(2, 7));
            probe.Clear();

            var read = new byte[2 * 512];
            await cache.ReadAsync(5, read);

            Assert.That(read, Is.EqualTo(Sectors(2, 7)));
            Assert.That(probe.Requests, Is.Empty);
        }

        [Test]
        public async Task FailedWriteIsReadAgainFromTheDeviceTest()
        {
            var (probe, cache) = Create(64, 16);
            await cache.WriteAsync(8, Sectors(1, 1));
            probe.FailingWrites = 1;

            Assert.ThrowsAsync<IOException>(async () => await cache.WriteAsync(8, Sectors(1, 2)));
            probe.Clear();
            var read = new byte[512];
            await cache.ReadAsync(8, read);

            Assert.That(read, Is.EqualTo(Sectors(1, 1)));
            Assert.That(probe.Of(BlockDeviceOperation.Read).Count(), Is.EqualTo(1));
        }

        [Test]
        public async Task FullCacheEvictsWithoutWritingTest()
        {
            var (probe, cache) = Create(64, 4);

            for (int sector = 0; sector < 10; sector++)
                await cache.WriteAsync(sector, Sectors(1, (byte)sector));

            Assert.That(cache.CachedCount, Is.EqualTo(4));
            Assert.That(probe.Of(BlockDeviceOperation.Write).Count(), Is.EqualTo(10));
        }

        [Test]
        public async Task FlushOnlyFlushesTheDeviceTest()
        {
            var (probe, cache) = Create(64, 16);
            await cache.WriteAsync(1, Sectors(1, 9));
            probe.Clear();

            await cache.FlushAsync();
            await cache.DisposeAsync();

            Assert.That(probe.Requests, Is.EqualTo(new[] { new BlockDeviceRequest(BlockDeviceOperation.Flush, 0, 0) }));
            Assert.That(probe.IsDisposed, Is.True);
        }

        [Test]
        public async Task DisposeFlushesWhatWasWrittenTest()
        {
            var (probe, cache) = Create(64, 16);
            await cache.WriteAsync(1, Sectors(1, 9));

            await cache.DisposeAsync();

            Assert.That(probe.Requests, Is.EqualTo(new[]
            {
                new BlockDeviceRequest(BlockDeviceOperation.Write, 1, 1),
                new BlockDeviceRequest(BlockDeviceOperation.Flush, 0, 0)
            }));
            Assert.That(probe.IsDisposed, Is.True);
        }

        #endregion

        #region Tools

        private static (BlockDeviceProbe Probe, BlockDeviceCached Cache) Create(long sectors, int capacity)
        {
            var probe = new BlockDeviceProbe(new BlockDeviceMemory(sectors));
            return (probe, new BlockDeviceCached(probe, capacity, writesThrough: true));
        }

        private static byte[] Sectors(int count, byte value)
        {
            return Enumerable.Repeat(value, count * 512).ToArray();
        }

        #endregion
    }
}
