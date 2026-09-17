using OutWit.Common.Fat.Devices;
using OutWit.Common.Fat.Tests.Utils;
using OutWit.Common.Fat.Utils;

namespace OutWit.Common.Fat.Tests.Utils
{
    [TestFixture]
    public class BlockDeviceExtensionsTests
    {
        #region Read Tests

        [TestCase(0, 512)]
        [TestCase(0, 1)]
        [TestCase(511, 2)]
        [TestCase(300, 1200)]
        [TestCase(1024, 3072)]
        [TestCase(7, 8 * 512 - 7)]
        public async Task BytesAnywhereAreReadTest(int offset, int length)
        {
            var (probe, image) = await CreateAsync(512, 8);

            var read = new byte[length];
            await probe.ReadBytesAsync(offset, read);

            Assert.That(read, Is.EqualTo(image.AsSpan(offset, length).ToArray()));
        }

        [Test]
        public async Task WholeSectorsInTheMiddleAreOneRequestTest()
        {
            var (probe, _) = await CreateAsync(4096, 16);

            await probe.ReadBytesAsync(100, new byte[10 * 4096]);

            Assert.That(probe.Requests, Is.EqualTo(new[]
            {
                new BlockDeviceRequest(BlockDeviceOperation.Read, 0, 1),
                new BlockDeviceRequest(BlockDeviceOperation.Read, 1, 9),
                new BlockDeviceRequest(BlockDeviceOperation.Read, 10, 1)
            }));
        }

        [Test]
        public async Task EmptyRangeReadsNothingTest()
        {
            var (probe, _) = await CreateAsync(512, 8);

            await probe.ReadBytesAsync(8 * 512, Memory<byte>.Empty);

            Assert.That(probe.Requests, Is.Empty);
        }

        [TestCase(-1, 1)]
        [TestCase(8 * 512, 1)]
        [TestCase(8 * 512 - 1, 2)]
        public async Task RangeOutsideDeviceIsRejectedTest(int offset, int length)
        {
            var (probe, _) = await CreateAsync(512, 8);

            Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await probe.ReadBytesAsync(offset, new byte[length]));
        }

        [Test]
        public void NullDeviceIsRejectedTest()
        {
            Assert.ThrowsAsync<ArgumentNullException>(async () => await BlockDeviceExtensions.ReadBytesAsync(null!, 0, new byte[1]));
        }

        #endregion

        #region Write Tests

        [TestCase(0, 512)]
        [TestCase(0, 1)]
        [TestCase(511, 2)]
        [TestCase(300, 1200)]
        [TestCase(1024, 3072)]
        [TestCase(7, 8 * 512 - 7)]
        public async Task BytesAnywhereAreWrittenAndNeighboursKeptTest(int offset, int length)
        {
            var (probe, image) = await CreateAsync(512, 8);
            var data = new byte[length];
            new System.Random(length).NextBytes(data);

            await probe.WriteBytesAsync(offset, data);

            data.CopyTo(image, offset);
            var written = new byte[image.Length];
            await probe.ReadAsync(0, written);
            Assert.That(written, Is.EqualTo(image));
        }

        [Test]
        public async Task WholeSectorsInTheMiddleAreWrittenInOneRequestTest()
        {
            var (probe, _) = await CreateAsync(4096, 16);

            await probe.WriteBytesAsync(100, new byte[10 * 4096]);

            Assert.That(probe.Of(BlockDeviceOperation.Write), Is.EqualTo(new[]
            {
                new BlockDeviceRequest(BlockDeviceOperation.Write, 0, 1),
                new BlockDeviceRequest(BlockDeviceOperation.Write, 1, 9),
                new BlockDeviceRequest(BlockDeviceOperation.Write, 10, 1)
            }));
            Assert.That(probe.Of(BlockDeviceOperation.Read), Is.EqualTo(new[]
            {
                new BlockDeviceRequest(BlockDeviceOperation.Read, 0, 1),
                new BlockDeviceRequest(BlockDeviceOperation.Read, 10, 1)
            }));
        }

        [Test]
        public async Task AlignedWriteReadsNothingTest()
        {
            var (probe, _) = await CreateAsync(512, 8);

            await probe.WriteBytesAsync(512, new byte[1024]);

            Assert.That(probe.Requests, Is.EqualTo(new[] { new BlockDeviceRequest(BlockDeviceOperation.Write, 1, 2) }));
        }

        [TestCase(-1, 1)]
        [TestCase(8 * 512, 1)]
        [TestCase(8 * 512 - 1, 2)]
        public async Task WriteOutsideDeviceIsRejectedTest(int offset, int length)
        {
            var (probe, _) = await CreateAsync(512, 8);

            Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await probe.WriteBytesAsync(offset, new byte[length]));
            Assert.That(probe.Requests, Is.Empty);
        }

        #endregion

        #region Tools

        private static async Task<(BlockDeviceProbe Probe, byte[] Image)> CreateAsync(int sectorSize, int sectors)
        {
            var image = new byte[sectorSize * sectors];
            new System.Random(sectorSize).NextBytes(image);
            var probe = new BlockDeviceProbe(await BlockDeviceMemory.LoadAsync(new MemoryStream(image.ToArray()), sectorSize, isReadOnly: false));
            return (probe, image);
        }

        #endregion
    }
}
