using System.Text;
using OutWit.Common.Fat.Devices;
using OutWit.Common.Fat.ExFat;
using OutWit.Common.Fat.Exceptions;
using OutWit.Common.Fat.Model;
using OutWit.Common.Fat.Tests.Utils;
using OutWit.Common.Fat.Utils;
using OutWit.Common.Fat.Volumes;

namespace OutWit.Common.Fat.Tests
{
    /// <summary>
    /// Setting and removing the volume label: in the root, and on FAT in the boot sector
    /// and its backup as well.
    /// </summary>
    [TestFixture]
    public class FatVolumeLabelTests
    {
        #region Constants

        private static readonly FatKind[] KINDS = { FatKind.Fat12, FatKind.Fat16, FatKind.Fat32, FatKind.ExFat };

        private static readonly object[] INVALID =
        {
            new object[] { FatKind.Fat16, "a*b" },
            new object[] { FatKind.Fat16, "twelve chars" },
            new object[] { FatKind.Fat12, " DATA" },
            new object[] { FatKind.Fat32, "Метка" },
            new object[] { FatKind.ExFat, "twelve chars" },
            new object[] { FatKind.ExFat, "tab\there" }
        };

        #endregion

        #region Set Tests

        [TestCaseSource(nameof(KINDS))]
        public async Task LabelIsSetOnAVolumeWithoutOneTest(FatKind kind)
        {
            var (disk, volume) = await FatCheckerCases.FormatAsync(kind);
            string expected = kind == FatKind.ExFat ? "Backup 1" : "BACKUP 1";
            await using (volume)
            {
                Assert.That(await volume.GetLabelAsync(), Is.Null);

                Assert.That(await volume.SetLabelAsync("Backup 1"), Is.EqualTo(expected));
                Assert.That(await volume.GetLabelAsync(), Is.EqualTo(expected));
            }

            await using var again = await TestVolumes.RemountAsync(disk);
            Assert.That(await again.GetLabelAsync(), Is.EqualTo(expected));
            Assert.That(await BootLabelsAsync(disk, again.Info), Is.All.EqualTo(kind == FatKind.ExFat ? null : "BACKUP 1   "));
            Assert.That(await LabelSlotsAsync(again), Is.EqualTo(1));
            Assert.That((await FatChecker.CheckAsync(again)).IsClean, Is.True);
        }

        [TestCaseSource(nameof(KINDS))]
        public async Task LabelIsReplacedInPlaceTest(FatKind kind)
        {
            var disk = await FormatAsync(kind, "Old");
            await using var volume = await TestVolumes.RemountAsync(disk);
            await volume.WriteAllBytesAsync("/file.txt", new byte[10]);

            await volume.SetLabelAsync("Newer");

            Assert.That(await volume.GetLabelAsync(), Is.EqualTo(kind == FatKind.ExFat ? "Newer" : "NEWER"));
            Assert.That(await LabelSlotsAsync(volume), Is.EqualTo(1));
            Assert.That(await BootLabelsAsync(disk, volume.Info), Is.All.EqualTo(kind == FatKind.ExFat ? null : "NEWER      "));
            Assert.That((await volume.GetEntryAsync("/file.txt"))!.Length, Is.EqualTo(10));
            Assert.That((await FatChecker.CheckAsync(volume)).IsClean, Is.True);
        }

        [TestCaseSource(nameof(KINDS))]
        public async Task LabelIsRemovedTest(FatKind kind)
        {
            var disk = await FormatAsync(kind, "Gone soon");
            await using (var volume = await TestVolumes.RemountAsync(disk))
                Assert.That(await volume.SetLabelAsync(null), Is.Null);

            await using var again = await TestVolumes.RemountAsync(disk);
            Assert.That(await again.GetLabelAsync(), Is.Null);
            Assert.That(await BootLabelsAsync(disk, again.Info), Is.All.EqualTo(kind == FatKind.ExFat ? null : "NO NAME    "));
            Assert.That(await LabelSlotsAsync(again), Is.EqualTo(kind == FatKind.ExFat ? 1 : 0), "exFAT keeps an empty label entry.");
            Assert.That((await FatChecker.CheckAsync(again)).IsClean, Is.True);
        }

        [TestCase(FatKind.Fat12, "")]
        [TestCase(FatKind.Fat16, "   ")]
        [TestCase(FatKind.ExFat, "")]
        public async Task BlankLabelRemovesTheLabelTest(FatKind kind, string label)
        {
            var disk = await FormatAsync(kind, "Blank me");
            await using var volume = await TestVolumes.RemountAsync(disk);

            Assert.That(await volume.SetLabelAsync(label), Is.Null);
            Assert.That(await volume.GetLabelAsync(), Is.Null);
        }

        [TestCase(FatKind.Fat16)]
        [TestCase(FatKind.Fat32)]
        public async Task RemovingNoLabelChangesNothingTest(FatKind kind)
        {
            var (disk, volume) = await FatCheckerCases.FormatAsync(kind);
            await volume.DisposeAsync();
            var before = await ImageAsync(disk);

            await using (var again = await TestVolumes.RemountAsync(disk))
                await again.SetLabelAsync(null);

            Assert.That(await ImageAsync(disk), Is.EqualTo(before));
        }

        [Test]
        public async Task UnicodeLabelIsKeptOnExFatTest()
        {
            var disk = await FormatAsync(FatKind.ExFat, null);
            await using (var volume = await TestVolumes.RemountAsync(disk))
                await volume.SetLabelAsync("Метка тома");

            await using var again = await TestVolumes.RemountAsync(disk);
            Assert.That(await again.GetLabelAsync(), Is.EqualTo("Метка тома"));
        }

        [Test]
        public async Task FullRootOfFat32GrowsForTheLabelTest()
        {
            var (disk, volume) = await FatCheckerCases.FormatAsync(FatKind.Fat32);
            int slots = volume.Info.ClusterSize / 32;
            await using (volume)
            {
                for (int i = 0; i < slots; i++)
                    await volume.WriteAllBytesAsync($"/F{i:D2}.BIN", new byte[1]);
                Assert.That((await volume.GetClusterRunsAsync("/", CancellationToken.None)).Sum(run => run.Count), Is.EqualTo(1));

                await volume.SetLabelAsync("GROWN");

                Assert.That((await volume.GetClusterRunsAsync("/", CancellationToken.None)).Sum(run => run.Count), Is.EqualTo(2));
            }

            await using var again = await TestVolumes.RemountAsync(disk);
            Assert.That(await again.GetLabelAsync(), Is.EqualTo("GROWN"));
            var report = await FatChecker.CheckAsync(again);
            Assert.That(report.IsClean, Is.True);
            Assert.That(report.Files, Is.EqualTo(slots));
        }

        #endregion

        #region Refusal Tests

        [TestCaseSource(nameof(INVALID))]
        public async Task InvalidLabelIsRefusedTest(FatKind kind, string label)
        {
            var disk = await FormatAsync(kind, "Kept");
            await using var volume = await TestVolumes.RemountAsync(disk);
            var before = await ImageAsync(disk);

            Assert.That(async () => await volume.SetLabelAsync(label), Throws.ArgumentException);
            Assert.That(await ImageAsync(disk), Is.EqualTo(before));
        }

        [Test]
        public async Task FullFixedRootRefusesANewLabelTest()
        {
            var (disk, volume) = await TestVolumes.BlankAsync(FatKind.Fat16, rootEntries: 16);
            await using (volume)
            {
                for (int i = 0; i < 16; i++)
                    await volume.WriteAllBytesAsync($"/F{i:D2}.BIN", new byte[1]);
                var before = await ImageAsync(disk);

                var error = Assert.ThrowsAsync<FatException>(async () => await volume.SetLabelAsync("NO ROOM"));

                Assert.That(error!.Kind, Is.EqualTo(FatErrorKind.NoSpace));
                Assert.That(await ImageAsync(disk), Is.EqualTo(before));
            }
        }

        [Test]
        public async Task ReadOnlyDeviceRefusesALabelTest()
        {
            var disk = await ReferenceImages.OpenAsync(ReferenceImages.Get("fat16-c1"));
            await using var volume = await FatVolume.MountDiskAsync(disk);

            Assert.That(async () => await volume.SetLabelAsync("NOPE"), Throws.TypeOf<NotSupportedException>());
        }

        [Test]
        public async Task DisposedVolumeRefusesALabelTest()
        {
            var (_, volume) = await TestVolumes.BlankAsync(FatKind.Fat12);
            await volume.DisposeAsync();

            Assert.That(async () => await volume.SetLabelAsync("LATE"), Throws.TypeOf<ObjectDisposedException>());
        }

        #endregion

        #region Tools

        private static async Task<BlockDeviceMemory> FormatAsync(FatKind kind, string? label)
        {
            var (disk, volume) = await FatCheckerCases.FormatAsync(kind);
            await volume.SetLabelAsync(label);
            await volume.DisposeAsync();
            return disk;
        }

        /// <summary>
        /// The label of the boot sector and of its backup, as stored; <c>null</c> on exFAT.
        /// </summary>
        private static async Task<List<string?>> BootLabelsAsync(IBlockDevice disk, FatVolumeInfo info)
        {
            var labels = new List<string?>();
            foreach (long sector in info.BackupBootSector is { } backup ? new long[] { 0, backup } : new long[] { 0 })
            {
                var bytes = new byte[info.SectorSize];
                await disk.ReadAsync(sector, bytes);
                int offset = info.Kind == FatKind.Fat32 ? 71 : 43;
                labels.Add(info.Kind == FatKind.ExFat ? null : Encoding.ASCII.GetString(bytes, offset, 11));
            }

            return labels;
        }

        /// <summary>
        /// How many live label entries the root holds.
        /// </summary>
        private static async Task<int> LabelSlotsAsync(FatVolume volume)
        {
            bool isExFat = volume.Info.Kind == FatKind.ExFat;
            int count = 0;
            await foreach (var chunk in volume.Core.OpenDirectory(volume.Names.Root).ReadSlotsAsync(CancellationToken.None))
            {
                for (int offset = 0; offset < chunk.Length; offset += 32)
                {
                    var slot = chunk.Span.Slice(offset, 32);
                    count += isExFat
                        ? slot[0] == ExFatEntry.VOLUME_LABEL ? 1 : 0
                        : slot[0] is not (0 or 0xE5) && (slot[11] & 0x3F) != 0x0F && (slot[11] & 0x18) == 0x08 ? 1 : 0;
                }
            }

            return count;
        }

        private static async Task<byte[]> ImageAsync(BlockDeviceMemory disk)
        {
            using var copy = new MemoryStream();
            await disk.SaveAsync(copy);
            return copy.ToArray();
        }

        #endregion
    }
}
