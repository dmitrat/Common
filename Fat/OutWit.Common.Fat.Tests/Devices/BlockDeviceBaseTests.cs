using OutWit.Common.Fat.Devices;
using OutWit.Common.Fat.Tests.Utils;

namespace OutWit.Common.Fat.Tests.Devices
{
    [TestFixture]
    public class BlockDeviceBaseTests
    {
        #region Geometry Tests

        [TestCase(512)]
        [TestCase(1024)]
        [TestCase(2048)]
        [TestCase(4096)]
        public void ValidSectorSizeIsAcceptedTest(int sectorSize)
        {
            Assert.That(BlockDeviceBase.IsValidSectorSize(sectorSize), Is.True);
            Assert.DoesNotThrow(() => _ = new BlockDeviceMemory(1, sectorSize));
        }

        [TestCase(0)]
        [TestCase(256)]
        [TestCase(1000)]
        [TestCase(8192)]
        [TestCase(-512)]
        public void InvalidSectorSizeIsRejectedTest(int sectorSize)
        {
            Assert.That(BlockDeviceBase.IsValidSectorSize(sectorSize), Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new BlockDeviceMemory(1, sectorSize));
        }

        [Test]
        public void NegativeSectorCountIsRejectedTest()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new BlockDeviceMemory(-1));
        }

        [TestCase(512)]
        [TestCase(4096)]
        public void SectorCountMustKeepBytesAddressableTest(int sectorSize)
        {
            long largest = long.MaxValue / sectorSize;

            Assert.DoesNotThrow(() => _ = new BlockDeviceMemory(largest, sectorSize));
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new BlockDeviceMemory(largest + 1, sectorSize));
        }

        #endregion

        #region Range Tests

        [Test]
        public async Task LastSectorCanBeReadTest()
        {
            await using var device = new BlockDeviceMemory(8);

            Assert.DoesNotThrowAsync(async () => await device.ReadAsync(7, new byte[512]));
            Assert.DoesNotThrowAsync(async () => await device.ReadAsync(0, new byte[8 * 512]));
        }

        [TestCase(-1, 1)]
        [TestCase(8, 1)]
        [TestCase(7, 2)]
        [TestCase(0, 9)]
        [TestCase(long.MaxValue, 1)]
        public async Task RangeOutsideDeviceIsRejectedTest(long sector, int count)
        {
            await using var device = new BlockDeviceMemory(8);

            Assert.Throws<ArgumentOutOfRangeException>(() => _ = device.ReadAsync(sector, new byte[count * 512]));
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = device.WriteAsync(sector, new byte[count * 512]));
        }

        [TestCase(1)]
        [TestCase(511)]
        [TestCase(513)]
        public async Task PartialSectorBufferIsRejectedTest(int length)
        {
            await using var device = new BlockDeviceMemory(8);

            Assert.Throws<ArgumentException>(() => _ = device.ReadAsync(0, new byte[length]));
            Assert.Throws<ArgumentException>(() => _ = device.WriteAsync(0, new byte[length]));
        }

        [Test]
        public async Task EmptyBufferAtEndIsAcceptedTest()
        {
            await using var device = new BlockDeviceMemory(8);

            Assert.DoesNotThrowAsync(async () => await device.ReadAsync(8, Memory<byte>.Empty));
            Assert.DoesNotThrowAsync(async () => await device.WriteAsync(8, ReadOnlyMemory<byte>.Empty));
        }

        #endregion

        #region State Tests

        [Test]
        public async Task CancelledTokenCancelsCallsTest()
        {
            await using var device = new BlockDeviceMemory(8);
            using var source = new CancellationTokenSource();
            await source.CancelAsync();

            Assert.CatchAsync<OperationCanceledException>(async () => await device.ReadAsync(0, new byte[512], source.Token));
            Assert.CatchAsync<OperationCanceledException>(async () => await device.WriteAsync(0, new byte[512], source.Token));
            Assert.CatchAsync<OperationCanceledException>(async () => await device.FlushAsync(source.Token));
        }

        [Test]
        public async Task DisposedDeviceRejectsCallsTest()
        {
            var device = new BlockDeviceMemory(8);
            await device.DisposeAsync();

            Assert.That(device.IsDisposed, Is.True);
            Assert.Throws<ObjectDisposedException>(() => _ = device.ReadAsync(0, new byte[512]));
            Assert.Throws<ObjectDisposedException>(() => _ = device.WriteAsync(0, new byte[512]));
            Assert.Throws<ObjectDisposedException>(() => _ = device.FlushAsync());
            Assert.DoesNotThrowAsync(async () => await device.DisposeAsync());
        }

        [Test]
        public async Task FailedDisposalLeavesDeviceOpenTest()
        {
            var probe = new BlockDeviceProbe(new BlockDeviceMemory(8));
            var cache = new BlockDeviceCached(probe);
            await cache.WriteAsync(0, new byte[512]);
            probe.FailingWrites = 1;

            Assert.ThrowsAsync<IOException>(async () => await cache.DisposeAsync());
            Assert.That(cache.IsDisposed, Is.False);
            Assert.DoesNotThrowAsync(async () => await cache.ReadAsync(0, new byte[512]));

            await cache.DisposeAsync();
            Assert.That(cache.IsDisposed, Is.True);
        }

        [Test]
        public async Task ReadOnlyDeviceRejectsWritesButAllowsFlushTest()
        {
            await using var device = await BlockDeviceMemory.LoadAsync(new MemoryStream(new byte[4096]), isReadOnly: true);

            Assert.That(device.IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() => _ = device.WriteAsync(0, new byte[512]));
            Assert.DoesNotThrowAsync(async () => await device.FlushAsync());
        }

        #endregion
    }
}
