using System.Security.Cryptography;
using OutWit.Common.Fat.Directories;
using OutWit.Common.Fat.Model;
using OutWit.Common.Fat.Tests.Utils;
using OutWit.Common.Fat.Utils;
using OutWit.Common.Fat.Volumes;

namespace OutWit.Common.Fat.Tests
{
    /// <summary>
    /// Reading FAT12/16/32 and exFAT volumes against what the Linux kernel, mtools and
    /// dump.exfat saw in them.
    /// </summary>
    [TestFixture]
    public class FatVolumeReferenceTests
    {
        #region Constants

        private static readonly IEnumerable<string> IMAGES = ReferenceImages.Names;

        private static readonly IEnumerable<string> FAT_IMAGES = ReferenceImages.FatNames;

        #endregion

        #region Listing Tests

        [TestCaseSource(nameof(IMAGES))]
        public async Task ListingMatchesKernelTest(string name)
        {
            var (image, volume) = await MountAsync(name);
            await using var _ = volume;

            var listed = new List<FatDirectoryEntry>();
            await foreach (var entry in volume.EnumerateAsync("/", recursive: true))
                listed.Add(entry);

            var expected = image.Volumes[0].Entries;
            Assert.That(listed.Select(e => e.Path).OrderBy(p => p, StringComparer.Ordinal),
                Is.EqualTo(expected.Select(e => e.Path).OrderBy(p => p, StringComparer.Ordinal)));

            var kind = volume.Info.Kind == FatKind.ExFat ? DateTimeKind.Utc : DateTimeKind.Unspecified;
            var byPath = listed.ToDictionary(e => e.Path);
            using (Assert.EnterMultipleScope())
            {
                foreach (var reference in expected)
                {
                    var entry = byPath[reference.Path];
                    Assert.That(entry.IsDirectory, Is.EqualTo(reference.IsDirectory), reference.Path);
                    Assert.That(entry.Name, Is.EqualTo(reference.Name), reference.Path);
                    if (reference.IsDirectory)
                        continue;

                    Assert.That(entry.Length, Is.EqualTo(reference.Size), reference.Path);
                    Assert.That(entry.Modified, Is.EqualTo(reference.Modified), reference.Path);
                    Assert.That(entry.Modified!.Value.Kind, Is.EqualTo(kind), reference.Path);
                }
            }
        }

        [TestCaseSource(nameof(IMAGES))]
        public async Task RecursiveListingPutsChildrenAfterTheirDirectoryTest(string name)
        {
            var (_, volume) = await MountAsync(name);
            await using var _ = volume;

            var seen = new HashSet<string> { "/" };
            await foreach (var entry in volume.EnumerateAsync("/", recursive: true))
            {
                string parent = entry.Path[..Math.Max(1, entry.Path.LastIndexOf('/'))];
                Assert.That(seen, Does.Contain(parent), entry.Path);
                seen.Add(entry.Path);
            }
        }

        [TestCaseSource(nameof(IMAGES))]
        public async Task LabelMatchesOracleTest(string name)
        {
            var (image, volume) = await MountAsync(name);
            await using var _ = volume;

            Assert.That(await volume.GetLabelAsync(), Is.EqualTo(image.Volumes[0].Label));
        }

        [TestCaseSource(nameof(FAT_IMAGES))]
        public async Task ShortNamesMatchMtoolsTest(string name)
        {
            var (image, volume) = await MountAsync(name);
            await using var _ = volume;

            var withAlias = image.Volumes[0].Entries.Where(e => e.ShortName != null).ToList();
            Assert.That(withAlias, Has.Count.GreaterThan(300));

            using (Assert.EnterMultipleScope())
            {
                foreach (var reference in withAlias)
                {
                    var entry = await volume.GetEntryAsync(reference.Path);
                    Assert.That(entry?.ShortName, Is.EqualTo(reference.ShortName).IgnoreCase, reference.Path);
                }
            }
        }

        #endregion

        #region Lookup Tests

        [TestCaseSource(nameof(IMAGES))]
        public async Task EntriesAreFoundByAnyCaseTest(string name)
        {
            var (image, volume) = await MountAsync(name);
            await using var _ = volume;

            var samples = image.Volumes[0].Entries.Where(e => !e.Path.StartsWith("/many/")).ToList();
            using (Assert.EnterMultipleScope())
            {
                foreach (var reference in samples)
                {
                    var byUpper = await volume.GetEntryAsync(reference.Path.ToUpperInvariant().Replace('/', '\\'));
                    var byLower = await volume.GetEntryAsync(reference.Path.ToLowerInvariant());

                    Assert.That(byUpper?.Path, Is.EqualTo(reference.Path), reference.Path);
                    Assert.That(byLower?.Path, Is.EqualTo(reference.Path), reference.Path);
                }
            }
        }

        [TestCaseSource(nameof(FAT_IMAGES))]
        public async Task EntriesAreFoundByAliasTest(string name)
        {
            var (image, volume) = await MountAsync(name);
            await using var _ = volume;

            var samples = image.Volumes[0].Entries.Where(e => e.ShortName != null && !e.Path.StartsWith("/many/")).ToList();
            using (Assert.EnterMultipleScope())
            {
                foreach (var reference in samples)
                {
                    string parent = reference.Path[..reference.Path.LastIndexOf('/')];
                    var byAlias = await volume.GetEntryAsync(parent + "/" + reference.ShortName!.ToLowerInvariant());

                    Assert.That(byAlias?.Path, Is.EqualTo(reference.Path), reference.Path);
                }
            }
        }

        #endregion

        #region Content Tests

        [TestCaseSource(nameof(IMAGES))]
        public async Task FilesMatchKernelHashesTest(string name)
        {
            var (image, volume) = await MountAsync(name);
            await using var _ = volume;

            using (Assert.EnterMultipleScope())
            {
                foreach (var reference in image.Volumes[0].Entries.Where(e => !e.IsDirectory))
                {
                    var contents = await volume.ReadAllBytesAsync(reference.Path);
                    Assert.That(Convert.ToHexStringLower(SHA256.HashData(contents)), Is.EqualTo(reference.ContentSha256), reference.Path);
                }
            }
        }

        [TestCaseSource(nameof(IMAGES))]
        public async Task ClusterRunsMatchOracleTest(string name)
        {
            var (image, volume) = await MountAsync(name);
            await using var _ = volume;

            var withRuns = image.Volumes[0].Entries.Where(e => e.Clusters != null).ToList();
            Assert.That(withRuns.Any(e => e.Clusters!.Count > 1), Is.True, "the image has fragmented files");

            using (Assert.EnterMultipleScope())
            {
                foreach (var reference in withRuns)
                {
                    var runs = await volume.GetClusterRunsAsync(reference.Path, CancellationToken.None);
                    Assert.That(runs.Select(r => new[] { (long)r.FirstCluster, (long)r.LastCluster }), Is.EqualTo(reference.Clusters), reference.Path);
                }
            }
        }

        [TestCaseSource(nameof(IMAGES))]
        public async Task FreeClustersMatchOracleTest(string name)
        {
            var (image, volume) = await MountAsync(name);
            await using var _ = volume;

            Assert.That(await volume.CountFreeClustersAsync(), Is.EqualTo(image.Volumes[0].FreeClusters));
        }

        [TestCaseSource(nameof(IMAGES))]
        public async Task ScatteredReadsMatchWholeFileTest(string name)
        {
            var (_, volume) = await MountAsync(name);
            await using var _ = volume;
            var random = new System.Random(name.Length);
            var paths = new List<string> { "/data/random.bin", "/frag/a.bin", "/cluster-plus-one.bin" };
            if (volume.Info.Kind == FatKind.ExFat)
                paths.Add("/unwritten-tail.bin");

            foreach (string path in paths)
            {
                var whole = await volume.ReadAllBytesAsync(path);
                await using var stream = await volume.OpenReadAsync(path);
                for (int step = 0; step < 40; step++)
                {
                    int position = random.Next(whole.Length + 10);
                    var buffer = new byte[random.Next(1, volume.Info.ClusterSize * 3)];
                    stream.Position = position;

                    int read = await stream.ReadAsync(buffer);

                    int expected = Math.Clamp(whole.Length - position, 0, buffer.Length);
                    Assert.That(read, Is.EqualTo(expected), $"{path} at {position}");
                    Assert.That(buffer.AsSpan(0, read).SequenceEqual(whole.AsSpan(Math.Min(position, whole.Length), read)), Is.True, $"{path} at {position}");
                }
            }
        }

        #endregion

        #region Cost Tests

        [TestCaseSource(nameof(IMAGES))]
        public async Task ReadingOneFileNeverScansTheTableTest(string name)
        {
            var image = ReferenceImages.Get(name);
            var probe = new BlockDeviceProbe(await ReferenceImages.OpenAsync(image));
            await using var volume = await FatVolume.MountDiskAsync(probe);

            var contents = await volume.ReadAllBytesAsync("/data/random.bin");

            long dataClusters = (contents.Length + volume.Info.ClusterSize - 1) / volume.Info.ClusterSize;
            long entriesPerSector = volume.Info.SectorSize * 8 / (volume.Info.Kind switch { FatKind.Fat12 => 12, FatKind.Fat16 => 16, _ => 32 });
            long tableSectors = TableSectorsRead(probe, image, volume);
            Assert.That(tableSectors, Is.LessThanOrEqualTo(dataClusters / entriesPerSector + 2));
            Assert.That(tableSectors, Is.LessThan(image.Volumes[0].FatSectors));
        }

        [TestCaseSource(nameof(IMAGES))]
        public async Task ContiguousFileIsReadInOneRequestTest(string name)
        {
            var image = ReferenceImages.Get(name);
            var probe = new BlockDeviceProbe(await ReferenceImages.OpenAsync(image));
            await using var volume = await FatVolume.MountDiskAsync(probe);
            var entry = await volume.GetEntryAsync("/data/random.bin");
            var runs = await volume.GetClusterRunsAsync(entry!.Path, CancellationToken.None);
            Assume.That(runs, Has.Count.EqualTo(1), "the file was written in one piece");
            await using var stream = await volume.OpenReadAsync("/data/random.bin");
            long dataStart = image.Volumes[0].FirstSector + volume.Info.ClusterHeapSector;
            probe.Clear();

            await stream.ReadExactlyAsync(new byte[entry.Length]);

            var dataReads = probe.Of(BlockDeviceOperation.Read).Where(r => r.Sector >= dataStart).ToList();
            Assert.That(dataReads, Has.Count.LessThanOrEqualTo(2), "whole sectors in one request, the partial last one in another");
        }

        #endregion

        #region Tools

        /// <summary>
        /// How many sectors of the allocation tables the probe has seen read.
        /// </summary>
        internal static long TableSectorsRead(BlockDeviceProbe probe, ReferenceImage image, FatVolume volume)
        {
            var info = image.Volumes[0];
            long tableStart = info.FirstSector + info.FatOffset;
            long tableEnd = tableStart + volume.Info.FatCount * info.FatSectors;
            return probe.Of(BlockDeviceOperation.Read)
                .Sum(r => Math.Max(0, Math.Min(r.Sector + r.Count, tableEnd) - Math.Max(r.Sector, tableStart)));
        }

        private static async Task<(ReferenceImage Image, FatVolume Volume)> MountAsync(string name)
        {
            var image = ReferenceImages.Get(name);
            var disk = await ReferenceImages.OpenAsync(image);
            return (image, await FatVolume.MountDiskAsync(disk));
        }

        #endregion
    }
}
