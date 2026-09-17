using OutWit.Common.Fat.Devices;
using OutWit.Common.Fat.Utils;

namespace OutWit.Common.Fat.Tests.Devices
{
    [TestFixture]
    public class BlockDeviceFileTests
    {
        #region Fields

        private string m_directory = null!;

        #endregion

        [SetUp]
        public void Setup()
        {
            m_directory = Path.Combine(Path.GetTempPath(), "outwit-fat-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(m_directory);
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(m_directory, recursive: true);
        }

        #region Create and Open Tests

        [Test]
        public async Task CreateMakesZeroFilledFileTest()
        {
            string path = Path.Combine(m_directory, "new.img");

            await using (var device = BlockDeviceFile.Create(path, 100, 1024))
            {
                Assert.That(device.SectorCount, Is.EqualTo(100));
                Assert.That(device.SectorSize, Is.EqualTo(1024));
                Assert.That(device.Path, Is.EqualTo(Path.GetFullPath(path)));

                var read = new byte[100 * 1024];
                await device.ReadAsync(0, read);
                Assert.That(read, Is.All.EqualTo(0));
            }

            Assert.That(new FileInfo(path).Length, Is.EqualTo(100 * 1024));
        }

        [Test]
        public async Task WrittenSectorsSurviveReopeningTest()
        {
            string path = Path.Combine(m_directory, "data.img");
            var data = new byte[3 * 512];
            new System.Random(5).NextBytes(data);

            await using (var device = BlockDeviceFile.Create(path, 64))
            {
                await device.WriteAsync(40, data);
                await device.FlushAsync();
            }

            await using (var device = BlockDeviceFile.Open(path, isReadOnly: true))
            {
                var read = new byte[3 * 512];
                await device.ReadAsync(40, read);
                Assert.That(read, Is.EqualTo(data));
            }

            var raw = await File.ReadAllBytesAsync(path);
            Assert.That(raw.AsSpan(40 * 512, 3 * 512).ToArray(), Is.EqualTo(data));
        }

        [Test]
        public async Task TrailingPartialSectorIsNotPartOfDeviceTest()
        {
            string path = Path.Combine(m_directory, "odd.img");
            await File.WriteAllBytesAsync(path, new byte[10 * 512 + 100]);

            await using var device = BlockDeviceFile.Open(path);

            Assert.That(device.SectorCount, Is.EqualTo(10));
        }

        [Test]
        public async Task ReadOnlyOpenRejectsWritesAndSharesReadingTest()
        {
            string path = Path.Combine(m_directory, "shared.img");
            await File.WriteAllBytesAsync(path, new byte[8 * 512]);

            await using var first = BlockDeviceFile.Open(path, isReadOnly: true);
            await using var second = BlockDeviceFile.Open(path, isReadOnly: true);

            Assert.That(first.IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() => _ = first.WriteAsync(0, new byte[512]));
            Assert.DoesNotThrowAsync(async () => await second.ReadAsync(0, new byte[512]));
            Assert.DoesNotThrowAsync(async () => await first.FlushAsync());
        }

        [Test]
        public async Task WritableOpenIsExclusiveTest()
        {
            string path = Path.Combine(m_directory, "exclusive.img");
            await File.WriteAllBytesAsync(path, new byte[8 * 512]);

            await using var device = BlockDeviceFile.Open(path);

            Assert.Throws<IOException>(() => BlockDeviceFile.Open(path, isReadOnly: true));
        }

        [Test]
        public void MissingFileIsReportedTest()
        {
            Assert.Throws<FileNotFoundException>(() => BlockDeviceFile.Open(Path.Combine(m_directory, "missing.img")));
        }

        [Test]
        public void InvalidSectorSizeLeavesNoFileTest()
        {
            string path = Path.Combine(m_directory, "invalid.img");

            Assert.Throws<ArgumentOutOfRangeException>(() => BlockDeviceFile.Create(path, 10, 500));
            Assert.Throws<ArgumentOutOfRangeException>(() => BlockDeviceFile.Create(path, long.MaxValue / 512 + 1));
            Assert.That(File.Exists(path), Is.False);
        }

        #endregion
    }
}
