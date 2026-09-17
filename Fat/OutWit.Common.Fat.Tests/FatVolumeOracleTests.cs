using System.Text;
using OutWit.Common.Fat.Devices;
using OutWit.Common.Fat.Directories;
using OutWit.Common.Fat.Exceptions;
using OutWit.Common.Fat.Model;
using OutWit.Common.Fat.Tests.Utils;
using OutWit.Common.Fat.Utils;

namespace OutWit.Common.Fat.Tests
{
    /// <summary>
    /// Writing to FAT12/16/32 and exFAT volumes, judged by fsck.vfat and fsck.exfat, the
    /// Linux kernel, mtools and dump.exfat.
    /// </summary>
    /// <remarks>Ignored where WSL, or Linux with the tools and passwordless sudo, is not available.</remarks>
    [TestFixture]
    [Category(WslOracle.CATEGORY)]
    public class FatVolumeOracleTests
    {
        #region Constants

        private static readonly IEnumerable<string> IMAGES = ReferenceImages.Names;

        private static readonly FatVolumeOptions LEAVE_OPEN = new() { LeaveOpen = true };

        #endregion

        #region Oracle Tests

        [TestCaseSource(nameof(IMAGES))]
        public async Task ScriptedChangesPassTheLinuxToolsTest(string name)
        {
            WslOracle.Require();
            var (disk, volume) = await MountAsync(name);
            var model = await VolumeModel.ReadAsync(volume);
            int cluster = volume.Info.ClusterSize;

            await WriteAsync(volume, model, "/new.txt", Pattern("new", 100));
            await WriteAsync(volume, model, "/Long File Name 4.txt", Pattern("lfn4", 20));
            await WriteAsync(volume, model, "/unicode/Ещё один файл.txt", Pattern("cyr", cluster + 3));
            await WriteAsync(volume, model, "/lower2.txt", Pattern("low", 5));
            await WriteAsync(volume, model, "/MIXED.Txt", Pattern("mix", 7));

            await volume.CreateDirectoryAsync("/made/deeper/deepest");
            model.CreateDirectory("/made/deeper/deepest");
            await WriteAsync(volume, model, "/made/deeper/deepest/file.bin", Pattern("deep", 3 * cluster + 7));
            for (int i = 0; i < 40; i++)
                await WriteAsync(volume, model, $"/many/added entry {i:D2}.dat", Pattern($"a{i:D2}", i));

            await DeleteAsync(volume, model, "/frag/a.bin");
            await AppendAsync(volume, model, "/frag/b.bin", Pattern("more", 2 * cluster + 1));
            await DeleteAsync(volume, model, "/level01", recursive: true);
            await DeleteAsync(volume, model, "/Empty Directory");

            await MoveAsync(volume, model, "/Mixed Case Name.txt", "/made/Renamed Mixed.txt");
            await MoveAsync(volume, model, "/made/deeper", "/unicode/deeper moved");
            await MoveAsync(volume, model, "/lower.txt", "/LOWER.TXT");
            await MoveAsync(volume, model, "/one.bin", "/README.TXT", overwrite: true);

            await SetLengthAsync(volume, model, "/cluster-plus-one.bin", 1);
            await SetLengthAsync(volume, model, "/cluster.bin", 2 * cluster + 5);
            await SetLengthAsync(volume, model, "/data/random.bin", 0);
            await WriteAsync(volume, model, "/empty.bin", Array.Empty<byte>());

            await volume.CreateDirectoryAsync("/inter");
            model.CreateDirectory("/inter");
            await InterleaveAsync(volume, model, "/inter/x.bin", "/inter/y.bin", cluster, 5);

            await SetLengthAsync(volume, model, "/grown.bin", 0, create: true);
            await SetLengthAsync(volume, model, "/grown.bin", 3 * cluster + 11);
            await AppendAsync(volume, model, "/grown.bin", Pattern("grn", cluster));
            if (volume.Info.Kind == FatKind.ExFat)
            {
                await AppendAsync(volume, model, "/unwritten-tail.bin", Pattern("tail", 100));
                await WriteAtAsync(volume, model, "/cluster.bin", 5 * cluster, Pattern("far", 10));
            }

            await model.AssertMatchesAsync(volume);
            if (volume.Info.Kind != FatKind.ExFat)
            {
                Assert.That((await volume.GetEntryAsync("/Long File Name 4.txt"))!.ShortName, Is.EqualTo("LONGFI~4.TXT"));
                Assert.That((await volume.GetEntryAsync("/LOWER.TXT"))!.ShortName, Is.EqualTo("LOWER.TXT"));
            }

            await CheckAsync(name, disk, volume, model);
        }

        [TestCase("fat12-floppy")]
        [TestCase("fat16-4k-1fat")]
        [TestCase("fat32-few-c64")]
        [TestCase("exfat-c8")]
        [TestCase("exfat-4k-c4")]
        public async Task FullVolumePassesTheLinuxToolsTest(string name)
        {
            WslOracle.Require();
            var (disk, volume) = await MountAsync(name);
            var model = await VolumeModel.ReadAsync(volume);
            long free = await volume.CountFreeClustersAsync();
            int chunk = (int)Math.Min(free * volume.Info.ClusterSize / 3, 1 << 24);

            await volume.CreateDirectoryAsync("/fill");
            model.CreateDirectory("/fill");
            for (int i = 0; ; i++)
            {
                string path = $"/fill/part {i}.bin";
                try
                {
                    await WriteAsync(volume, model, path, Pattern($"f{i}", chunk + i));
                }
                catch (FatException error) when (error.Kind == FatErrorKind.NoSpace)
                {
                    if (await volume.ExistsAsync(path))
                        model.Write(path, Array.Empty<byte>());
                    break;
                }
            }

            Assert.That(await volume.CountFreeClustersAsync(), Is.LessThan((long)chunk / volume.Info.ClusterSize + 2));
            await DeleteAsync(volume, model, "/fill/part 0.bin");
            await WriteAsync(volume, model, "/fill/after.bin", Pattern("aft", chunk / 2));

            await CheckAsync(name, disk, volume, model);
        }

        [TestCase(FatKind.Fat12, 11, 150, 3000)]
        [TestCase(FatKind.Fat16, 12, 4200, 30000)]
        [TestCase(FatKind.Fat32, 13, 100, 4000)]
        [TestCase(FatKind.Fat32, 14, 3000, 40000)]
        [TestCase(FatKind.ExFat, 16, 2100, 30000)]
        [TestCase(FatKind.ExFat, 17, 3000, 500)]
        public async Task RandomChangesPassTheLinuxToolsTest(FatKind kind, int seed, int clusters, int maxBytes)
        {
            WslOracle.Require();
            var (disk, volume) = await TestVolumes.BlankAsync(kind, clusters, rootEntries: 32);
            var model = await VolumeModel.ReadAsync(volume);

            await FatVolumeModelTests.RunAsync(new RandomOperations(volume, model, seed, maxBytes), 300);

            await volume.DisposeAsync();
            await using var reopened = await TestVolumes.RemountAsync(disk);
            var problems = await WslOracle.CheckAsync(disk, await OracleExpectation.FromVolumeAsync(reopened));
            Assert.That(problems, Is.Empty);
        }

        [TestCase("fat32-mbr-c2", 15)]
        [TestCase("exfat-mbr-c256", 18)]
        public async Task RandomChangesOnAReferenceImagePassTheLinuxToolsTest(string name, int seed)
        {
            WslOracle.Require();
            var (disk, volume) = await MountAsync(name);
            var model = await VolumeModel.ReadAsync(volume);

            await FatVolumeModelTests.RunAsync(new RandomOperations(volume, model, seed, 200000), 200);

            await CheckAsync(name, disk, volume, model);
        }

        #endregion

        #region Tools

        private static async Task CheckAsync(string name, BlockDeviceMemory disk, FatVolume volume, VolumeModel model)
        {
            await volume.DisposeAsync();
            await using var reopened = await FatVolume.MountDiskAsync(disk, LEAVE_OPEN);
            await model.AssertMatchesAsync(reopened);

            var image = ReferenceImages.Get(name);
            long start = image.Partitions.Count > 0 ? image.Partitions[0].FirstSector : 0;
            var expected = await OracleExpectation.FromVolumeAsync(reopened, start);
            var problems = await WslOracle.CheckAsync(disk, expected);

            Assert.That(problems, Is.Empty);
        }

        private static async Task<(BlockDeviceMemory Disk, FatVolume Volume)> MountAsync(string name)
        {
            var disk = await ReferenceImages.OpenAsync(ReferenceImages.Get(name), isReadOnly: false);
            var volume = await FatVolume.MountDiskAsync(disk, LEAVE_OPEN);
            return (disk, volume);
        }

        private static async Task WriteAsync(FatVolume volume, VolumeModel model, string path, byte[] data)
        {
            await volume.WriteAllBytesAsync(path, data);
            model.Write(path, data);
        }

        private static async Task AppendAsync(FatVolume volume, VolumeModel model, string path, byte[] data)
        {
            await using (var stream = await volume.OpenAsync(path, FileMode.Append, FileAccess.Write))
                await stream.WriteAsync(data);
            model.Write(path, model.Read(path).Concat(data).ToArray());
        }

        private static async Task SetLengthAsync(FatVolume volume, VolumeModel model, string path, int length, bool create = false)
        {
            await using (var stream = await volume.OpenAsync(path, create ? FileMode.CreateNew : FileMode.Open, FileAccess.ReadWrite))
                await stream.SetLengthAsync(length);
            var data = create ? Array.Empty<byte>() : model.Read(path);
            var resized = new byte[length];
            data.AsSpan(0, Math.Min(length, data.Length)).CopyTo(resized);
            model.Write(path, resized);
        }

        /// <summary>
        /// Writes at a position, past the end when it lies there.
        /// </summary>
        private static async Task WriteAtAsync(FatVolume volume, VolumeModel model, string path, int position, byte[] data)
        {
            await using (var stream = await volume.OpenAsync(path, FileMode.Open, FileAccess.ReadWrite))
            {
                stream.Position = position;
                await stream.WriteAsync(data);
            }

            var old = model.Read(path);
            var written = new byte[Math.Max(old.Length, position + data.Length)];
            old.CopyTo(written, 0);
            data.CopyTo(written, position);
            model.Write(path, written);
        }

        private static async Task DeleteAsync(FatVolume volume, VolumeModel model, string path, bool recursive = false)
        {
            await volume.DeleteAsync(path, recursive);
            model.Delete(path);
        }

        private static async Task MoveAsync(FatVolume volume, VolumeModel model, string source, string destination, bool overwrite = false)
        {
            await volume.MoveAsync(source, destination, overwrite);
            model.Move(source, destination);
        }

        /// <summary>
        /// Grows two files a chunk at a time, alternately, so their chains interleave.
        /// </summary>
        private static async Task InterleaveAsync(FatVolume volume, VolumeModel model, string first, string second, int chunk, int chunks)
        {
            var one = Pattern("one", chunk * chunks);
            var two = Pattern("two", chunk * chunks);
            await using (var a = await volume.OpenAsync(first, FileMode.CreateNew, FileAccess.Write))
            await using (var b = await volume.OpenAsync(second, FileMode.CreateNew, FileAccess.Write))
            {
                for (int i = 0; i < chunks; i++)
                {
                    await a.WriteAsync(one.AsMemory(i * chunk, chunk));
                    await b.WriteAsync(two.AsMemory(i * chunk, chunk));
                }
            }

            model.Write(first, one);
            model.Write(second, two);
        }

        /// <summary>
        /// Bytes in which every 16-byte record names its own offset.
        /// </summary>
        public static byte[] Pattern(string tag, int size)
        {
            var text = new StringBuilder(size + 16);
            for (int offset = 0; text.Length < size; offset += 16)
                text.Append($"{tag,-4}"[..4]).Append(offset.ToString("D10")).Append("\r\n");
            return Encoding.ASCII.GetBytes(text.ToString(0, size));
        }

        #endregion
    }
}
