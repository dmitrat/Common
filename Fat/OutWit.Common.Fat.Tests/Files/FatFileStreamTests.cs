using System.Buffers.Binary;
using OutWit.Common.Fat.Devices;
using OutWit.Common.Fat.Tests.Utils;

namespace OutWit.Common.Fat.Tests.Files
{
    [TestFixture]
    public class FatFileStreamTests
    {
        #region Constants

        private const string IMAGE = "fat16-c1";

        private static readonly FatVolumeOptions LEAVE_OPEN = new() { LeaveOpen = true };

        #endregion

        #region Stream Tests

        [Test]
        public async Task StreamIsReadableAndSeekableOnlyTest()
        {
            await using var volume = await MountAsync();
            await using var stream = await volume.OpenReadAsync("/data/random.bin");

            Assert.That(stream.CanRead, Is.True);
            Assert.That(stream.CanSeek, Is.True);
            Assert.That(stream.CanWrite, Is.False);
            Assert.That(stream.Length, Is.EqualTo(40000));
            Assert.That(stream.Entry.Path, Is.EqualTo("/data/random.bin"));
            Assert.Throws<NotSupportedException>(() => stream.Write(new byte[1], 0, 1));
            Assert.Throws<NotSupportedException>(() => stream.SetLength(0));
            Assert.DoesNotThrow(() => stream.Flush());
        }

        [Test]
        public async Task SeekFollowsItsOriginTest()
        {
            await using var volume = await MountAsync();
            await using var stream = await volume.OpenReadAsync("/data/random.bin");

            Assert.That(stream.Seek(100, SeekOrigin.Begin), Is.EqualTo(100));
            Assert.That(stream.Seek(-50, SeekOrigin.Current), Is.EqualTo(50));
            Assert.That(stream.Seek(-1, SeekOrigin.End), Is.EqualTo(39999));
            Assert.That(stream.Seek(10, SeekOrigin.End), Is.EqualTo(40010));
            Assert.Throws<IOException>(() => stream.Seek(-1, SeekOrigin.Begin));
            Assert.Throws<ArgumentOutOfRangeException>(() => stream.Position = -1);
        }

        [Test]
        public async Task SynchronousReadsMatchAsynchronousTest()
        {
            await using var volume = await MountAsync();
            var whole = await volume.ReadAllBytesAsync("/data/random.bin");
            await using var stream = await volume.OpenReadAsync("/data/random.bin");

            var first = new byte[1000];
            int firstRead = stream.Read(first, 0, first.Length);
            var second = new byte[700];
            int secondRead = stream.Read(second.AsSpan());

            Assert.That(firstRead + secondRead, Is.EqualTo(1700));
            Assert.That(first.Concat(second), Is.EqualTo(whole.Take(1700)));
            Assert.That(stream.Position, Is.EqualTo(1700));
        }

        [Test]
        public async Task EmptyFileReadsNothingTest()
        {
            await using var volume = await MountAsync();
            await using var stream = await volume.OpenReadAsync("/empty.bin");

            Assert.That(stream.Length, Is.Zero);
            Assert.That(await stream.ReadAsync(new byte[10]), Is.Zero);
        }

        [Test]
        public async Task ClosedStreamOrVolumeRefusesReadsTest()
        {
            var volume = await MountAsync();
            var stream = await volume.OpenReadAsync("/one.bin");
            var other = await volume.OpenReadAsync("/one.bin");

            await stream.DisposeAsync();
            await volume.DisposeAsync();

            Assert.That(stream.CanRead, Is.False);
            Assert.ThrowsAsync<ObjectDisposedException>(async () => await stream.ReadExactlyAsync(new byte[1]));
            Assert.ThrowsAsync<ObjectDisposedException>(async () => await other.ReadExactlyAsync(new byte[1]));
        }

        [Test]
        public async Task FailedReadLeavesThePositionTest()
        {
            var image = ReferenceImages.Get(IMAGE);
            var probe = new BlockDeviceProbe(await ReferenceImages.OpenAsync(image));
            await using var volume = await FatVolume.MountAsync(probe);
            var entry = await volume.GetEntryAsync("/frag/a.bin");
            var runs = await volume.GetClusterRunsAsync(entry!.Path, CancellationToken.None);
            long second = ImageSlots.ClusterSector(volume.Info, 0, runs[1].FirstCluster);
            var whole = await volume.ReadAllBytesAsync("/frag/a.bin");
            await using var stream = await volume.OpenReadAsync("/frag/a.bin");
            probe.FailRead = (sector, count) => sector <= second && second < sector + count;

            Assert.ThrowsAsync<IOException>(async () => await stream.ReadExactlyAsync(new byte[whole.Length]));
            Assert.That(stream.Position, Is.Zero);

            probe.FailRead = null;
            var read = new byte[whole.Length];
            await stream.ReadExactlyAsync(read);
            Assert.That(read, Is.EqualTo(whole));
        }

        #endregion

        #region Corruption Tests

        [Test]
        public async Task ChainShorterThanFileIsCorruptTest()
        {
            var image = ReferenceImages.Get(IMAGE);
            var disk = await ReferenceImages.OpenAsync(image, isReadOnly: false);
            await using var volume = await FatVolume.MountAsync(disk);
            var entry = await volume.GetEntryAsync("/cluster-plus-one.bin");
            await EndChainAtAsync(disk, volume.Info, entry!.FirstCluster);
            await using var reopened = await FatVolume.MountAsync(disk, LEAVE_OPEN);
            await using var stream = await reopened.OpenReadAsync("/cluster-plus-one.bin");

            var first = new byte[volume.Info.ClusterSize];
            await stream.ReadExactlyAsync(first);
            var error = Assert.ThrowsAsync<FatException>(async () => await stream.ReadExactlyAsync(new byte[1]));

            Assert.That(error!.Kind, Is.EqualTo(FatErrorKind.Corrupt));
            Assert.That(error.Message, Does.Contain("/cluster-plus-one.bin").And.Contain("needs 2"));
        }

        [Test]
        public async Task ClaimedSizeIsCheckedBeforeMemoryIsTakenTest()
        {
            var disk = await PatchSizeAsync("ONE     BIN", 0x7FFFF000);
            await using var volume = await FatVolume.MountAsync(disk);

            long before = GC.GetTotalAllocatedBytes(precise: true);
            var error = Assert.ThrowsAsync<FatException>(async () => await volume.ReadAllBytesAsync("/one.bin"));
            long allocated = GC.GetTotalAllocatedBytes(precise: true) - before;

            Assert.That(error!.Kind, Is.EqualTo(FatErrorKind.Corrupt));
            Assert.That(allocated, Is.LessThan(64 * 1024 * 1024));
        }

        [Test]
        public async Task FileBeyondArrayLimitIsReadOnlyAsStreamTest()
        {
            var disk = await PatchSizeAsync("ONE     BIN", 0xFFFFFFFF);
            await using var volume = await FatVolume.MountAsync(disk);

            Assert.ThrowsAsync<IOException>(async () => await volume.ReadAllBytesAsync("/one.bin"));
            await using var stream = await volume.OpenReadAsync("/one.bin");
            Assert.That(stream.Length, Is.EqualTo(0xFFFFFFFFL));
        }

        #endregion

        #region Tools

        private static async Task<BlockDeviceMemory> PatchSizeAsync(string name, uint size)
        {
            var disk = await ReferenceImages.OpenAsync(ReferenceImages.Get(IMAGE), isReadOnly: false);
            var info = (await FatDetector.DetectVolumeAsync(disk))!;
            long slot = await ImageSlots.FindAsync(disk, info.RootDirectorySector!.Value, 32, name);
            await ImageSlots.WriteAsync(disk, slot + 28, BitConverter.GetBytes(size));
            return disk;
        }

        private static async Task<FatVolume> MountAsync()
        {
            return await FatVolume.MountAsync(await ReferenceImages.OpenAsync(ReferenceImages.Get(IMAGE)));
        }

        private static async Task EndChainAtAsync(BlockDeviceMemory disk, FatVolumeInfo info, uint cluster)
        {
            var sector = new byte[info.SectorSize];
            long fatSector = info.FatOffset + cluster * 2 / info.SectorSize;
            for (int copy = 0; copy < info.FatCount; copy++)
            {
                await disk.ReadAsync(fatSector + copy * info.FatSectors, sector);
                BinaryPrimitives.WriteUInt16LittleEndian(sector.AsSpan((int)(cluster * 2 % info.SectorSize)), 0xFFFF);
                await disk.WriteAsync(fatSector + copy * info.FatSectors, sector);
            }
        }

        #endregion
    }
}
