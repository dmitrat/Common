using OutWit.Common.Fat.Devices;
using OutWit.Common.Fat.Directories;
using OutWit.Common.Fat.Exceptions;
using OutWit.Common.Fat.Model;
using OutWit.Common.Fat.Tests.Utils;
using OutWit.Common.Fat.Utils;

namespace OutWit.Common.Fat.Tests.Files
{
    /// <summary>
    /// <see cref="FatVolume.OpenAsync"/> for every mode and access, as <see cref="FileStream"/> behaves.
    /// </summary>
    [TestFixture]
    public class FatFileOpenerTests
    {
        #region Constants

        private const string EXISTING = "/existing.txt";

        private const string MISSING = "/missing.txt";

        private static readonly byte[] CONTENTS = { 1, 2, 3 };

        #endregion

        #region Mode Tests

        [TestCase(FileMode.Open, FileAccess.Read, 0, 3)]
        [TestCase(FileMode.Open, FileAccess.ReadWrite, 0, 3)]
        [TestCase(FileMode.OpenOrCreate, FileAccess.Read, 0, 3)]
        [TestCase(FileMode.OpenOrCreate, FileAccess.Write, 0, 3)]
        [TestCase(FileMode.Create, FileAccess.Write, 0, 0)]
        [TestCase(FileMode.Truncate, FileAccess.ReadWrite, 0, 0)]
        [TestCase(FileMode.Append, FileAccess.Write, 3, 3)]
        public async Task ExistingFileOpensTest(FileMode mode, FileAccess access, long position, long length)
        {
            await using var volume = await CreateAsync();

            await using (var stream = await volume.OpenAsync(EXISTING, mode, access))
                Assert.That((stream.Position, stream.Length), Is.EqualTo((position, length)));

            Assert.That((await volume.GetEntryAsync(EXISTING))!.Length, Is.EqualTo(length));
        }

        [TestCase(FileMode.CreateNew, FileAccess.Write)]
        [TestCase(FileMode.Create, FileAccess.ReadWrite)]
        [TestCase(FileMode.OpenOrCreate, FileAccess.Read)]
        [TestCase(FileMode.OpenOrCreate, FileAccess.Write)]
        [TestCase(FileMode.Append, FileAccess.Write)]
        public async Task MissingFileIsCreatedTest(FileMode mode, FileAccess access)
        {
            var (disk, volume) = await TestVolumes.BlankAsync(FatKind.Fat16);

            await using (var stream = await volume.OpenAsync("/New File.txt", mode, access))
                Assert.That(stream.Length, Is.Zero);

            await volume.DisposeAsync();
            await using var reopened = await TestVolumes.RemountAsync(disk);
            var entry = (await reopened.GetEntryAsync("/new file.TXT"))!;
            Assert.That(entry.Name, Is.EqualTo("New File.txt"));
            Assert.That((entry.Length, entry.FirstCluster, entry.Attributes), Is.EqualTo((0L, 0u, FatAttributes.Archive)));
            Assert.That(entry.Created, Is.EqualTo(TestVolumes.NOW));
        }

        [Test]
        public async Task ExistingFileCannotBeCreatedNewTest()
        {
            await using var volume = await CreateAsync();

            var error = Assert.ThrowsAsync<FatException>(async () => await volume.OpenAsync(EXISTING, FileMode.CreateNew, FileAccess.Write));

            Assert.That(error!.Kind, Is.EqualTo(FatErrorKind.AlreadyExists));
            Assert.That(await volume.ReadAllBytesAsync(EXISTING), Is.EqualTo(CONTENTS));
        }

        [TestCase(FileMode.Open)]
        [TestCase(FileMode.Truncate)]
        public async Task MissingFileIsNotFoundTest(FileMode mode)
        {
            await using var volume = await CreateAsync();

            var error = Assert.ThrowsAsync<FatException>(async () => await volume.OpenAsync(MISSING, mode, FileAccess.ReadWrite));

            Assert.That(error!.Kind, Is.EqualTo(FatErrorKind.NotFound));
            Assert.That(await volume.ExistsAsync(MISSING), Is.False);
        }

        #endregion

        #region Argument Tests

        [TestCase(FileMode.CreateNew)]
        [TestCase(FileMode.Create)]
        [TestCase(FileMode.Truncate)]
        [TestCase(FileMode.Append)]
        public async Task WritingModeNeedsWriteAccessTest(FileMode mode)
        {
            await using var volume = await CreateAsync();

            Assert.ThrowsAsync<ArgumentException>(async () => await volume.OpenAsync(EXISTING, mode, FileAccess.Read));
            Assert.That(await volume.ReadAllBytesAsync(EXISTING), Is.EqualTo(CONTENTS));
        }

        [Test]
        public async Task AppendCannotReadTest()
        {
            await using var volume = await CreateAsync();

            Assert.ThrowsAsync<ArgumentException>(async () => await volume.OpenAsync(EXISTING, FileMode.Append, FileAccess.ReadWrite));
        }

        [TestCase(0, FileAccess.Read)]
        [TestCase(7, FileAccess.Read)]
        [TestCase(3, 0)]
        [TestCase(3, 4)]
        public async Task UndefinedValuesAreRejectedTest(int mode, FileAccess access)
        {
            await using var volume = await CreateAsync();

            Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await volume.OpenAsync(EXISTING, (FileMode)mode, access));
        }

        [TestCase("/")]
        [TestCase("/../x.txt")]
        [TestCase("/bad?name.txt")]
        [TestCase("/trailing.")]
        public async Task BadPathIsRejectedTest(string path)
        {
            await using var volume = await CreateAsync();

            Assert.ThrowsAsync<ArgumentException>(async () => await volume.OpenAsync(path, FileMode.Create, FileAccess.Write));
        }

        #endregion

        #region Path Tests

        [Test]
        public async Task DirectoryIsNotAFileTest()
        {
            await using var volume = await CreateAsync();
            await volume.CreateDirectoryAsync("/dir");

            var error = Assert.ThrowsAsync<FatException>(async () => await volume.OpenAsync("/DIR", FileMode.OpenOrCreate, FileAccess.Write));

            Assert.That(error!.Kind, Is.EqualTo(FatErrorKind.NotAFile));
        }

        [Test]
        public async Task PathThroughAFileIsNotADirectoryTest()
        {
            await using var volume = await CreateAsync();

            var error = Assert.ThrowsAsync<FatException>(async () => await volume.OpenAsync(EXISTING + "/inner.txt", FileMode.Create, FileAccess.Write));

            Assert.That(error!.Kind, Is.EqualTo(FatErrorKind.NotADirectory));
        }

        [Test]
        public async Task MissingDirectoryIsNotFoundTest()
        {
            await using var volume = await CreateAsync();

            var error = Assert.ThrowsAsync<FatException>(async () => await volume.OpenAsync("/nowhere/file.txt", FileMode.Create, FileAccess.Write));

            Assert.That(error!.Kind, Is.EqualTo(FatErrorKind.NotFound));
        }

        #endregion

        #region Protection Tests

        [Test]
        public async Task ReadOnlyFileOpensOnlyForReadingTest()
        {
            await using var volume = await CreateAsync();
            await MarkReadOnlyAsync(volume, EXISTING);

            var error = Assert.ThrowsAsync<FatException>(async () => await volume.OpenAsync(EXISTING, FileMode.Open, FileAccess.ReadWrite));
            var truncated = Assert.ThrowsAsync<FatException>(async () => await volume.OpenAsync(EXISTING, FileMode.Create, FileAccess.Write));

            Assert.That(error!.Kind, Is.EqualTo(FatErrorKind.AccessDenied));
            Assert.That(truncated!.Kind, Is.EqualTo(FatErrorKind.AccessDenied));
            Assert.That(await volume.ReadAllBytesAsync(EXISTING), Is.EqualTo(CONTENTS));
        }

        [TestCase(EXISTING, FileMode.Open, FileAccess.Write)]
        [TestCase(MISSING, FileMode.OpenOrCreate, FileAccess.Read)]
        [TestCase(MISSING, FileMode.CreateNew, FileAccess.Write)]
        public async Task ReadOnlyDeviceRefusesWritingTest(string path, FileMode mode, FileAccess access)
        {
            await using var volume = await ReadOnlyAsync();

            Assert.ThrowsAsync<NotSupportedException>(async () => await volume.OpenAsync(path, mode, access));
        }

        [Test]
        public async Task ReadOnlyDeviceStillReadsTest()
        {
            await using var volume = await ReadOnlyAsync();

            await using var stream = await volume.OpenAsync(EXISTING, FileMode.OpenOrCreate, FileAccess.Read);
            Assert.That(volume.IsReadOnly, Is.True);
            Assert.That(stream.Length, Is.EqualTo(3));
        }

        #endregion

        #region Tools

        private static async Task<FatVolume> CreateAsync()
        {
            var (_, volume) = await TestVolumes.BlankAsync(FatKind.Fat16);
            await volume.WriteAllBytesAsync(EXISTING, CONTENTS);
            return volume;
        }

        private static async Task<FatVolume> ReadOnlyAsync()
        {
            var (disk, volume) = await TestVolumes.BlankAsync(FatKind.Fat16);
            await volume.WriteAllBytesAsync(EXISTING, CONTENTS);
            await volume.DisposeAsync();

            var image = new MemoryStream();
            await disk.SaveAsync(image);
            image.Position = 0;
            var readOnly = await BlockDeviceMemory.LoadAsync(image, disk.SectorSize, isReadOnly: true);
            return await FatVolume.MountAsync(readOnly);
        }

        /// <summary>
        /// Sets the read-only attribute, which the library offers no way to set yet.
        /// </summary>
        public static async Task MarkReadOnlyAsync(FatVolume volume, string path)
        {
            var entry = (await volume.GetEntryAsync(path))!;
            string shortName = entry.ShortName.Contains('.')
                ? entry.ShortName.Split('.')[0].PadRight(8) + entry.ShortName.Split('.')[1].PadRight(3)
                : entry.ShortName.PadRight(11);
            var info = volume.Info;
            long directorySector = VolumeModel.Parent(path) == "/" && info.RootDirectorySector is { } root
                ? root
                : ImageSlots.ClusterSector(info, 0, (await volume.GetEntryAsync(VolumeModel.Parent(path)))?.FirstCluster ?? info.RootCluster!.Value);
            long slot = await ImageSlots.FindAsync(volume.Device, directorySector, info.SectorsPerCluster, shortName.ToUpperInvariant());
            await ImageSlots.WriteAsync(volume.Device, slot + 11, new[] { (byte)(entry.Attributes | FatAttributes.ReadOnly) });
        }

        #endregion
    }
}
