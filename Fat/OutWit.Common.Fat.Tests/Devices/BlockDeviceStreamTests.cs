using System.IO.Compression;
using OutWit.Common.Fat.Devices;

namespace OutWit.Common.Fat.Tests.Devices
{
    [TestFixture]
    public class BlockDeviceStreamTests
    {
        #region Mapping Tests

        [Test]
        public async Task SectorsMapToStreamOffsetsTest()
        {
            var stream = new MemoryStream(new byte[16 * 2048]);
            await using var device = new BlockDeviceStream(stream, 2048);
            var data = Enumerable.Range(0, 2 * 2048).Select(i => (byte)i).ToArray();

            await device.WriteAsync(5, data);
            await device.FlushAsync();
            var read = new byte[2 * 2048];
            await device.ReadAsync(5, read);

            Assert.That(read, Is.EqualTo(data));
            Assert.That(stream.ToArray().AsSpan(5 * 2048, 2 * 2048).ToArray(), Is.EqualTo(data));
        }

        [Test]
        public async Task FileStreamIsFlushedThroughTest()
        {
            string path = Path.Combine(Path.GetTempPath(), $"outwit-fat-{Guid.NewGuid():N}.img");
            try
            {
                await File.WriteAllBytesAsync(path, new byte[8 * 512]);
                await using (var device = new BlockDeviceStream(new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read)))
                {
                    await device.WriteAsync(3, Enumerable.Repeat((byte)0x3C, 512).ToArray());
                    await device.FlushAsync();
                }

                var raw = await File.ReadAllBytesAsync(path);
                Assert.That(raw.AsSpan(3 * 512, 512).ToArray(), Is.All.EqualTo(0x3C));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public async Task SectorCountRoundsDownTest()
        {
            await using var device = new BlockDeviceStream(new MemoryStream(new byte[3 * 512 + 511]));

            Assert.That(device.SectorCount, Is.EqualTo(3));
        }

        [Test]
        public async Task StreamThatCannotWriteMakesReadOnlyDeviceTest()
        {
            await using var device = new BlockDeviceStream(new MemoryStream(new byte[4096], writable: false));

            Assert.That(device.IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() => _ = device.WriteAsync(0, new byte[512]));
        }

        [Test]
        public async Task ReadOnlyFlagIsHonouredOnWritableStreamTest()
        {
            await using var device = new BlockDeviceStream(new MemoryStream(new byte[4096]), isReadOnly: true);

            Assert.That(device.IsReadOnly, Is.True);
        }

        #endregion

        #region Argument Tests

        [Test]
        public void NonSeekableStreamIsRejectedTest()
        {
            using var gzip = new GZipStream(new MemoryStream(), CompressionMode.Decompress);

            Assert.Throws<ArgumentException>(() => _ = new BlockDeviceStream(gzip));
        }

        [Test]
        public void NullStreamIsRejectedTest()
        {
            Assert.Throws<ArgumentNullException>(() => _ = new BlockDeviceStream(null!));
        }

        [Test]
        public void InvalidSectorSizeIsRejectedTest()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new BlockDeviceStream(new MemoryStream(new byte[4096]), 0));
        }

        #endregion

        #region Ownership Tests

        [Test]
        public async Task DisposeClosesStreamByDefaultTest()
        {
            var stream = new MemoryStream(new byte[4096]);

            await new BlockDeviceStream(stream).DisposeAsync();

            Assert.That(stream.CanRead, Is.False);
        }

        [Test]
        public async Task LeaveOpenKeepsStreamOpenTest()
        {
            var stream = new MemoryStream(new byte[4096]);

            await new BlockDeviceStream(stream, leaveOpen: true).DisposeAsync();

            Assert.That(stream.CanRead, Is.True);
        }

        #endregion
    }
}
