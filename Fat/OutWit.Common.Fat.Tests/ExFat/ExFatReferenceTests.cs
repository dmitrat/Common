using OutWit.Common.Fat.Directories;
using OutWit.Common.Fat.ExFat;
using OutWit.Common.Fat.Tests.Utils;
using OutWit.Common.Fat.Utils;
using OutWit.Common.Fat.Volumes;

namespace OutWit.Common.Fat.Tests.ExFat
{
    /// <summary>
    /// What is particular to exFAT, against what dump.exfat read in the reference images.
    /// </summary>
    [TestFixture]
    public class ExFatReferenceTests
    {
        #region Constants

        private static readonly IEnumerable<string> IMAGES = ReferenceImages.ExFatNames;

        private const string UNWRITTEN_TAIL = "/unwritten-tail.bin";

        #endregion

        #region Entry Tests

        [TestCaseSource(nameof(IMAGES))]
        public async Task StreamExtensionsMatchDumpExfatTest(string name)
        {
            var (image, _, names) = await OpenAsync(name);
            var entries = image.Volumes[0].Entries;
            Assert.That(entries.Any(e => e.Contiguous == true) && entries.Any(e => e.Contiguous == false), Is.True,
                "the image has files in both forms");

            using (Assert.EnterMultipleScope())
            {
                foreach (var reference in entries)
                {
                    var item = await names.FindAsync(FatPath.Split(reference.Path), CancellationToken.None);
                    Assert.That(item!.IsContiguous, Is.EqualTo(reference.Contiguous), reference.Path);
                    if (reference.IsDirectory)
                        continue;

                    Assert.That(item.DataLength, Is.EqualTo(reference.Size), reference.Path);
                    Assert.That(item.ValidLength, Is.EqualTo(reference.ValidDataLength), reference.Path);
                }
            }
        }

        [TestCaseSource(nameof(IMAGES))]
        public async Task NameHashesMatchTheKernelsTest(string name)
        {
            var (image, core, _) = await OpenAsync(name);
            var upcase = await core.ExFat!.GetUpcaseTableAsync(CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                foreach (var reference in image.Volumes[0].Entries)
                    Assert.That(upcase.HashOf(reference.Name), Is.EqualTo(reference.NameHash), reference.Path);
            }
        }

        #endregion

        #region Root Tests

        [TestCaseSource(nameof(IMAGES))]
        public async Task BitmapIsWhereDumpExfatFindsItTest(string name)
        {
            var image = ReferenceImages.Get(name);
            var (core, _, _) = await ReferenceImages.OpenCoreAsync(await ReferenceImages.OpenAsync(image));

            var bitmap = await core.ExFat!.GetBitmapAsync(CancellationToken.None);

            var expected = image.Volumes[0].Bitmap!;
            Assert.That((bitmap.FirstCluster, (long)bitmap.DataLength), Is.EqualTo((expected.FirstCluster, expected.Length)));
        }

        [TestCaseSource(nameof(IMAGES))]
        public async Task UpcaseTableSumsAsDumpExfatSaysTest(string name)
        {
            var image = ReferenceImages.Get(name);
            var disk = await ReferenceImages.OpenAsync(image);
            var expected = image.Volumes[0].Upcase!;
            var volume = image.Volumes[0];

            var table = new List<byte>();
            foreach (var run in expected.Clusters)
            {
                long sector = volume.FirstSector + volume.ClusterHeapSector + (run[0] - 2) * volume.SectorsPerCluster;
                var data = new byte[(run[1] - run[0] + 1) * volume.SectorsPerCluster * volume.SectorSize];
                await disk.ReadAsync(sector, data);
                table.AddRange(data);
            }

            Assert.That(ExFatChecksum.OfUpcaseTable(table.Take((int)expected.Length).ToArray()), Is.EqualTo(expected.Checksum));
        }

        #endregion

        #region Valid Length Tests

        [TestCaseSource(nameof(IMAGES))]
        public async Task UnwrittenTailIsNeverReadTest(string name)
        {
            var image = ReferenceImages.Get(name);
            var volume = image.Volumes[0];
            var reference = volume.Entries.Single(e => e.Path == UNWRITTEN_TAIL);
            var probe = new BlockDeviceProbe(await ReferenceImages.OpenAsync(image));
            await using var mounted = await FatVolume.MountDiskAsync(probe);
            await using var stream = await mounted.OpenReadAsync(UNWRITTEN_TAIL);
            probe.Clear();

            var contents = new byte[reference.Size!.Value];
            await stream.ReadExactlyAsync(contents);

            var sectors = FileSectors(volume, reference);
            int validSectors = (int)((reference.ValidDataLength!.Value + volume.SectorSize - 1) / volume.SectorSize);
            var unwritten = sectors.Skip(validSectors).ToHashSet();
            var read = probe.Of(BlockDeviceOperation.Read).SelectMany(r => Enumerable.Range(0, r.Count).Select(i => r.Sector + i));
            Assert.That(read.Where(unwritten.Contains), Is.Empty);
            Assert.That(contents.Skip((int)reference.ValidDataLength.Value), Is.All.Zero);

            var stored = new byte[volume.SectorSize];
            await probe.ReadAsync(sectors[validSectors], stored);
            Assert.That(stored, Has.Some.Not.Zero, "the fixture put junk past the valid data");
        }

        [TestCaseSource(nameof(IMAGES))]
        public async Task ContiguousFileIsReadWithoutTheTableTest(string name)
        {
            var image = ReferenceImages.Get(name);
            var probe = new BlockDeviceProbe(await ReferenceImages.OpenAsync(image));
            await using var mounted = await FatVolume.MountDiskAsync(probe);
            await using var stream = await mounted.OpenReadAsync("/data/random.bin");
            probe.Clear();

            await stream.ReadExactlyAsync(new byte[stream.Length]);

            Assert.That(FatVolumeReferenceTests.TableSectorsRead(probe, image, mounted), Is.Zero);
        }

        #endregion

        #region Tools

        private static async Task<(ReferenceImage Image, FatVolumeCore Core, FatNamespace Names)> OpenAsync(string name)
        {
            var image = ReferenceImages.Get(name);
            var (core, names, _) = await ReferenceImages.OpenCoreAsync(await ReferenceImages.OpenAsync(image));
            return (image, core, names);
        }

        /// <summary>
        /// The disk sectors of a file, in file order.
        /// </summary>
        private static List<long> FileSectors(ReferenceVolume volume, ReferenceEntry entry)
        {
            var sectors = new List<long>();
            foreach (var run in entry.Clusters!)
            {
                long first = volume.FirstSector + volume.ClusterHeapSector + (run[0] - 2) * volume.SectorsPerCluster;
                long count = (run[1] - run[0] + 1) * volume.SectorsPerCluster;
                for (long i = 0; i < count; i++)
                    sectors.Add(first + i);
            }

            return sectors;
        }

        #endregion
    }
}
