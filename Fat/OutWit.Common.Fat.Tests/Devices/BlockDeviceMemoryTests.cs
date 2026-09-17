using System.IO.Compression;
using OutWit.Common.Fat.Devices;

namespace OutWit.Common.Fat.Tests.Devices
{
    [TestFixture]
    public class BlockDeviceMemoryTests
    {
        #region Read and Write Tests

        [Test]
        public async Task NewDeviceReadsZerosTest()
        {
            await using var device = new BlockDeviceMemory(16, 4096);
            var buffer = Enumerable.Repeat((byte)0xAA, 16 * 4096).ToArray();

            await device.ReadAsync(0, buffer);

            Assert.That(buffer, Is.All.EqualTo(0));
            Assert.That(device.SectorCount, Is.EqualTo(16));
            Assert.That(device.SectorSize, Is.EqualTo(4096));
            Assert.That(device.IsReadOnly, Is.False);
        }

        [Test]
        public async Task WriteAcrossChunkBoundaryRoundTripsTest()
        {
            long sectors = 3 * BlockDeviceMemory.CHUNK_SIZE / 512;
            await using var device = new BlockDeviceMemory(sectors);
            long boundary = BlockDeviceMemory.CHUNK_SIZE / 512;
            var data = Pattern(6 * 512, 7);

            await device.WriteAsync(boundary - 3, data);
            var read = new byte[10 * 512];
            await device.ReadAsync(boundary - 5, read);

            Assert.That(read.AsSpan(0, 2 * 512).ToArray(), Is.All.EqualTo(0));
            Assert.That(read.AsSpan(2 * 512, 6 * 512).ToArray(), Is.EqualTo(data));
            Assert.That(read.AsSpan(8 * 512).ToArray(), Is.All.EqualTo(0));
            Assert.That(device.AllocatedBytes, Is.EqualTo(2L * BlockDeviceMemory.CHUNK_SIZE));
        }

        [Test]
        public async Task ZeroWritesAllocateNothingTest()
        {
            await using var device = new BlockDeviceMemory(1_000_000);

            await device.WriteAsync(12345, new byte[4096]);

            Assert.That(device.AllocatedBytes, Is.Zero);
        }

        [Test]
        public async Task OverwritingWithZerosClearsAllocatedChunkTest()
        {
            await using var device = new BlockDeviceMemory(4096);
            await device.WriteAsync(10, Pattern(512, 1));

            await device.WriteAsync(10, new byte[512]);
            var read = new byte[512];
            await device.ReadAsync(10, read);

            Assert.That(read, Is.All.EqualTo(0));
        }

        #endregion

        #region Load and Save Tests

        [Test]
        public async Task LoadAsyncStoresOnlyNonZeroChunksTest()
        {
            var image = new byte[4 * BlockDeviceMemory.CHUNK_SIZE];
            image[BlockDeviceMemory.CHUNK_SIZE + 100] = 1;

            await using var device = await BlockDeviceMemory.LoadAsync(new MemoryStream(image));

            Assert.That(device.SectorCount, Is.EqualTo(image.Length / 512));
            Assert.That(device.AllocatedBytes, Is.EqualTo((long)BlockDeviceMemory.CHUNK_SIZE));
        }

        [Test]
        public async Task LoadAsyncReadsNonSeekableStreamTest()
        {
            var image = Pattern(BlockDeviceMemory.CHUNK_SIZE + 3 * 4096, 3);
            var packed = new MemoryStream();
            await using (var gzip = new GZipStream(packed, CompressionLevel.Fastest, leaveOpen: true))
                await gzip.WriteAsync(image);
            packed.Position = 0;

            await using var device = await BlockDeviceMemory.LoadAsync(new GZipStream(packed, CompressionMode.Decompress), 4096);
            var read = new byte[image.Length];
            await device.ReadAsync(0, read);

            Assert.That(read, Is.EqualTo(image));
        }

        [Test]
        public void LoadAsyncRejectsPartialSectorTest()
        {
            Assert.ThrowsAsync<InvalidDataException>(async () => await BlockDeviceMemory.LoadAsync(new MemoryStream(new byte[4097]), 4096));
        }

        [Test]
        public async Task LoadAsyncOfEmptyStreamGivesEmptyDeviceTest()
        {
            await using var device = await BlockDeviceMemory.LoadAsync(new MemoryStream());

            Assert.That(device.SectorCount, Is.Zero);
        }

        [Test]
        public async Task SaveAsyncReproducesLoadedImageTest()
        {
            var image = new byte[5 * BlockDeviceMemory.CHUNK_SIZE / 2];
            Pattern(1000, 9).CopyTo(image, 0);
            Pattern(1000, 11).CopyTo(image, image.Length - 1000);

            await using var device = await BlockDeviceMemory.LoadAsync(new MemoryStream(image));
            var saved = new MemoryStream();
            await device.SaveAsync(saved);

            Assert.That(saved.ToArray(), Is.EqualTo(image));
        }

        #endregion

        #region Tools

        private static byte[] Pattern(int length, int seed)
        {
            var data = new byte[length];
            new System.Random(seed).NextBytes(data);
            return data;
        }

        #endregion
    }
}
