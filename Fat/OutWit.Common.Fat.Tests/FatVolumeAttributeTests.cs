using OutWit.Common.Fat.Exceptions;
using OutWit.Common.Fat.Model;
using OutWit.Common.Fat.Tests.Utils;
using OutWit.Common.Fat.Utils;

namespace OutWit.Common.Fat.Tests
{
    /// <summary>
    /// Setting the attributes a caller may set: read-only, hidden, system and archive.
    /// </summary>
    [TestFixture]
    public class FatVolumeAttributeTests
    {
        #region Constants

        private const string FILE = "/A long file name.txt";

        private static readonly FatKind[] KINDS = { FatKind.Fat12, FatKind.Fat16, FatKind.Fat32, FatKind.ExFat };

        #endregion

        #region Set Tests

        [TestCaseSource(nameof(KINDS))]
        public async Task FileAttributesAreReplacedTest(FatKind kind)
        {
            var (disk, volume) = await TestVolumes.BlankAsync(kind);
            await using (volume)
            {
                await volume.WriteAllBytesAsync(FILE, FatVolumeOracleTests.Pattern("att", 700));
                var before = (await volume.GetEntryAsync(FILE))!;
                Assert.That(before.Attributes, Is.EqualTo(FatAttributes.Archive));

                var changed = await volume.SetAttributesAsync(FILE, FatAttributes.ReadOnly | FatAttributes.Hidden);

                Assert.That(changed.Attributes, Is.EqualTo(FatAttributes.ReadOnly | FatAttributes.Hidden));
                Assert.That(changed.Modified, Is.EqualTo(before.Modified));
                Assert.That(changed.Created, Is.EqualTo(before.Created));
                Assert.That(changed.Length, Is.EqualTo(700));
            }

            await using var again = await TestVolumes.RemountAsync(disk);
            var entry = (await again.GetEntryAsync(FILE))!;
            Assert.That(entry.Attributes, Is.EqualTo(FatAttributes.ReadOnly | FatAttributes.Hidden));
            Assert.That(await again.ReadAllBytesAsync(FILE), Is.EqualTo(FatVolumeOracleTests.Pattern("att", 700)));
            Assert.That((await FatChecker.CheckAsync(again)).IsClean, Is.True);
        }

        [TestCaseSource(nameof(KINDS))]
        public async Task DirectoryStaysADirectoryTest(FatKind kind)
        {
            var (_, volume) = await TestVolumes.BlankAsync(kind);
            await using (volume)
            {
                await volume.CreateDirectoryAsync("/dir");
                await volume.WriteAllBytesAsync("/dir/inside.txt", new byte[3]);

                var changed = await volume.SetAttributesAsync("/dir", FatAttributes.Hidden | FatAttributes.System);

                Assert.That(changed.Attributes, Is.EqualTo(FatAttributes.Directory | FatAttributes.Hidden | FatAttributes.System));
                Assert.That(changed.IsDirectory, Is.True);
                Assert.That((await volume.GetEntryAsync("/dir/inside.txt"))!.Length, Is.EqualTo(3));
                Assert.That((await FatChecker.CheckAsync(volume)).IsClean, Is.True);
            }
        }

        [TestCaseSource(nameof(KINDS))]
        public async Task ReadOnlyEntryIsNotDeletedUntilClearedTest(FatKind kind)
        {
            var (_, volume) = await TestVolumes.BlankAsync(kind);
            await using (volume)
            {
                await volume.WriteAllBytesAsync("/keep.bin", new byte[5]);
                await volume.SetAttributesAsync("/keep.bin", FatAttributes.ReadOnly);

                var error = Assert.ThrowsAsync<FatException>(async () => await volume.DeleteAsync("/keep.bin"));
                Assert.That(error!.Kind, Is.EqualTo(FatErrorKind.AccessDenied));

                await volume.SetAttributesAsync("/keep.bin", FatAttributes.None);
                await volume.DeleteAsync("/keep.bin");
                Assert.That(await volume.GetEntryAsync("/keep.bin"), Is.Null);
            }
        }

        [TestCaseSource(nameof(KINDS))]
        public async Task FileOpenForWritingKeepsItsNewAttributesTest(FatKind kind)
        {
            var (_, volume) = await TestVolumes.BlankAsync(kind);
            await using (volume)
            {
                await using (var stream = await volume.OpenAsync("/open.bin", FileMode.CreateNew, FileAccess.Write))
                {
                    await volume.SetAttributesAsync("/open.bin", FatAttributes.System);
                    await stream.WriteAsync(FatVolumeOracleTests.Pattern("opn", 2 * volume.Info.ClusterSize + 3));
                }

                var entry = (await volume.GetEntryAsync("/open.bin"))!;
                Assert.That(entry.Attributes, Is.EqualTo(FatAttributes.System | FatAttributes.Archive), "A write sets the archive bit again.");
                Assert.That(entry.Length, Is.EqualTo(2 * volume.Info.ClusterSize + 3));
                Assert.That((await FatChecker.CheckAsync(volume)).IsClean, Is.True);
            }
        }

        [Test]
        public async Task SameAttributesWriteNothingTest()
        {
            var (probe, _, _) = await TestVolumes.CoreAsync(FatKind.ExFat);
            await using var volume = await FatVolume.MountAsync(probe, TestVolumes.Options(null));
            await volume.WriteAllBytesAsync("/same.txt", new byte[1]);
            await volume.FlushAsync();
            probe.Clear();

            var entry = await volume.SetAttributesAsync("/same.txt", FatAttributes.Archive);

            Assert.That(entry.Attributes, Is.EqualTo(FatAttributes.Archive));
            Assert.That(probe.Of(BlockDeviceOperation.Write), Is.Empty);
        }

        #endregion

        #region Refusal Tests

        [TestCase(FatAttributes.Directory)]
        [TestCase(FatAttributes.VolumeLabel)]
        [TestCase(FatAttributes.ReadOnly | FatAttributes.Directory)]
        [TestCase((FatAttributes)0x40)]
        public async Task AttributeThatTellsTheKindIsRefusedTest(FatAttributes attributes)
        {
            var (_, volume) = await TestVolumes.BlankAsync(FatKind.Fat16);
            await using (volume)
            {
                await volume.WriteAllBytesAsync("/x.bin", new byte[1]);

                Assert.That(async () => await volume.SetAttributesAsync("/x.bin", attributes), Throws.ArgumentException);
                Assert.That((await volume.GetEntryAsync("/x.bin"))!.Attributes, Is.EqualTo(FatAttributes.Archive));
            }
        }

        [TestCase("/")]
        [TestCase("/../x")]
        public async Task RootIsRefusedTest(string path)
        {
            var (_, volume) = await TestVolumes.BlankAsync(FatKind.ExFat);
            await using (volume)
                Assert.That(async () => await volume.SetAttributesAsync(path, FatAttributes.Hidden), Throws.ArgumentException);
        }

        [Test]
        public async Task MissingEntryIsReportedTest()
        {
            var (_, volume) = await TestVolumes.BlankAsync(FatKind.Fat32);
            await using (volume)
            {
                var error = Assert.ThrowsAsync<FatException>(async () => await volume.SetAttributesAsync("/missing.txt", FatAttributes.Hidden));

                Assert.That(error!.Kind, Is.EqualTo(FatErrorKind.NotFound));
            }
        }

        [Test]
        public async Task ReadOnlyDeviceRefusesAttributesTest()
        {
            var disk = await ReferenceImages.OpenAsync(ReferenceImages.Get("exfat-c8"));
            await using var volume = await FatVolume.MountDiskAsync(disk);

            Assert.That(async () => await volume.SetAttributesAsync("/README.TXT", FatAttributes.Hidden), Throws.TypeOf<NotSupportedException>());
        }

        #endregion
    }
}
