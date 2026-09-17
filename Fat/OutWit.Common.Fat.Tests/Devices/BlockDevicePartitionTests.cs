using OutWit.Common.Fat.Devices;
using OutWit.Common.Fat.Tests.Utils;

namespace OutWit.Common.Fat.Tests.Devices
{
    [TestFixture]
    public class BlockDevicePartitionTests
    {
        #region Mapping Tests

        [Test]
        public async Task SectorsAreOffsetIntoDiskTest()
        {
            await using var disk = new BlockDeviceMemory(100);
            await using var partition = new BlockDevicePartition(disk, 30, 50);
            var data = Enumerable.Repeat((byte)0x5A, 2 * 512).ToArray();

            await partition.WriteAsync(10, data);
            var read = new byte[2 * 512];
            await disk.ReadAsync(40, read);

            Assert.That(read, Is.EqualTo(data));
            Assert.That(partition.FirstSector, Is.EqualTo(30));
            Assert.That(partition.SectorCount, Is.EqualTo(50));
            Assert.That(partition.Disk, Is.SameAs(disk));
        }

        [Test]
        public async Task WindowEndsAtItsOwnLastSectorTest()
        {
            await using var disk = new BlockDeviceMemory(100);
            await using var partition = new BlockDevicePartition(disk, 30, 50);

            Assert.DoesNotThrowAsync(async () => await partition.ReadAsync(49, new byte[512]));
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = partition.ReadAsync(50, new byte[512]));
        }

        [Test]
        public async Task GeometryFollowsDiskTest()
        {
            await using var disk = await BlockDeviceMemory.LoadAsync(new MemoryStream(new byte[64 * 4096]), 4096, isReadOnly: true);
            await using var partition = new BlockDevicePartition(disk, 8, 8);

            Assert.That(partition.SectorSize, Is.EqualTo(4096));
            Assert.That(partition.IsReadOnly, Is.True);
        }

        #endregion

        #region Extent Tests

        [TestCase(-1, 10)]
        [TestCase(0, -1)]
        [TestCase(91, 10)]
        [TestCase(100, 1)]
        [TestCase(1, long.MaxValue)]
        public async Task ExtentOutsideDiskIsRejectedTest(long firstSector, long sectorCount)
        {
            await using var disk = new BlockDeviceMemory(100);

            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new BlockDevicePartition(disk, firstSector, sectorCount));
        }

        [TestCase(0, 100)]
        [TestCase(90, 10)]
        [TestCase(100, 0)]
        public async Task ExtentWithinDiskIsAcceptedTest(long firstSector, long sectorCount)
        {
            await using var disk = new BlockDeviceMemory(100);

            Assert.DoesNotThrow(() => _ = new BlockDevicePartition(disk, firstSector, sectorCount));
        }

        [Test]
        public void NullDiskIsRejectedTest()
        {
            Assert.Throws<ArgumentNullException>(() => _ = new BlockDevicePartition(null!, 0, 0));
        }

        #endregion

        #region Ownership Tests

        [Test]
        public async Task FlushIsPassedToDiskTest()
        {
            var probe = new BlockDeviceProbe(new BlockDeviceMemory(100));
            await using var partition = new BlockDevicePartition(probe, 10, 10);

            await partition.FlushAsync();

            Assert.That(probe.Of(BlockDeviceOperation.Flush).Count(), Is.EqualTo(1));
        }

        [Test]
        public async Task DisposeLeavesDiskOpenTest()
        {
            var probe = new BlockDeviceProbe(new BlockDeviceMemory(100));

            await new BlockDevicePartition(probe, 10, 10).DisposeAsync();

            Assert.That(probe.IsDisposed, Is.False);
            Assert.DoesNotThrowAsync(async () => await probe.ReadAsync(0, new byte[512]));
            await probe.DisposeAsync();
        }

        #endregion
    }
}
