using OutWit.Common.Fat.Checking;
using OutWit.Common.Fat.Model;
using OutWit.Common.Fat.Tests.Utils;
using OutWit.Common.Fat.Utils;
using OutWit.Common.NUnit;

namespace OutWit.Common.Fat.Tests
{
    /// <summary>
    /// The checker on sound volumes, which it must pass, and on volumes damaged one
    /// structure at a time, where it must name exactly what is wrong.
    /// </summary>
    [TestFixture]
    public class FatCheckerTests
    {
        #region Constants

        private static readonly FatKind[] KINDS = { FatKind.Fat12, FatKind.Fat16, FatKind.Fat32, FatKind.ExFat };

        private static readonly object[] RANDOM_CASES =
        {
            new object[] { FatKind.Fat12, 11 }, new object[] { FatKind.Fat16, 12 },
            new object[] { FatKind.Fat32, 13 }, new object[] { FatKind.ExFat, 14 }, new object[] { FatKind.ExFat, 15 }
        };

        #endregion

        #region Clean Volume Tests

        [TestCaseSource(typeof(ReferenceImages), nameof(ReferenceImages.Names))]
        public async Task ReferenceImageIsCleanTest(string name)
        {
            var image = ReferenceImages.Get(name);
            var expected = image.Volumes[0];
            await using var volume = await FatVolume.MountDiskAsync(await ReferenceImages.OpenAsync(image));

            var report = await FatChecker.CheckAsync(volume);

            Assert.That(Describe(report), Is.Empty);
            Assert.That(report.Files + report.Directories, Is.EqualTo(expected.Entries.Count));
            Assert.That(report.FreeClusters, Is.EqualTo(expected.FreeClusters));
            Assert.That(report.LostClusters, Is.Zero);
            Assert.That(report.UsedClusters + report.FreeClusters + report.BadClusters, Is.EqualTo(expected.ClusterCount));
        }

        [TestCaseSource(nameof(KINDS))]
        public async Task FormattedVolumeIsCleanTest(FatKind kind)
        {
            var (_, volume) = await FatCheckerCases.FormatAsync(kind);
            await using (volume)
            {
                var empty = await FatChecker.CheckAsync(volume);
                long free = await volume.CountFreeClustersAsync();
                await FatDamage.PopulateAsync(volume);
                var populated = await FatChecker.CheckAsync(volume);

                Assert.That(Describe(empty), Is.Empty);
                Assert.That(empty.Files + empty.Directories, Is.Zero);
                Assert.That(empty.FreeClusters, Is.EqualTo(free));
                Assert.That(Describe(populated), Is.Empty);
                Assert.That(populated.Files, Is.EqualTo(7));
                Assert.That(populated.Directories, Is.EqualTo(3));
                Assert.That(populated.FreeClusters, Is.EqualTo(await volume.CountFreeClustersAsync()));
                Assert.That(populated.UsedClusters + populated.FreeClusters, Is.EqualTo(volume.Info.ClusterCount));
            }
        }

        [TestCaseSource(nameof(RANDOM_CASES))]
        public async Task RandomChangesLeaveTheVolumeCleanTest(FatKind kind, int seed)
        {
            var (_, volume) = await TestVolumes.BlankAsync(kind, clusters: kind == FatKind.Fat16 ? 4200 : 600);
            await using (volume)
            {
                var model = await VolumeModel.ReadAsync(volume);
                var operations = new RandomOperations(volume, model, seed, 4 * volume.Info.ClusterSize);
                await FatVolumeModelTests.RunAsync(operations, 150);

                var report = await FatChecker.CheckAsync(volume);

                Assert.That(Describe(report), Is.Empty);
                Assert.That(report.FreeClusters, Is.EqualTo(await volume.CountFreeClustersAsync()));
            }
        }

        [Test]
        public async Task SecondBitmapOfTexFatIsNotLostTest()
        {
            const uint CLUSTERS = 300;
            var disk = await BlankVolumeExFat.CreateAsync(CLUSTERS, fatCount: 2);
            var info = (await FatDetector.DetectVolumeAsync(disk))!;
            uint second = CLUSTERS + 1;
            long rootSector = info.ClusterHeapSector + (info.RootCluster!.Value - 2) * info.SectorsPerCluster;
            await disk.WriteBytesAsync(rootSector * 512 + 2 * ExFatSetBuilder.ENTRY, ExFatSetBuilder.Bitmap(second, (CLUSTERS + 7) / 8, 0x01));
            for (int copy = 0; copy < 2; copy++)
                await disk.WriteBytesAsync((info.FatOffset + copy * info.FatSectors) * 512 + second * 4, BitConverter.GetBytes(uint.MaxValue));
            var bitmap = new byte[1];
            long bit = info.ClusterHeapSector * 512 + (second - 2) / 8;
            await disk.ReadBytesAsync(bit, bitmap);
            bitmap[0] |= (byte)(1 << (int)((second - 2) % 8));
            await disk.WriteBytesAsync(bit, bitmap);
            await using var volume = await FatVolume.MountAsync(disk, TestVolumes.Options(null));

            var report = await FatChecker.CheckAsync(volume);

            Assert.That(Describe(report), Is.Empty);
            Assert.That(report.UsedClusters + report.FreeClusters, Is.EqualTo(CLUSTERS));
        }

        [Test]
        public async Task FileOpenForWritingIsFlushedBeforeTheCheckTest()
        {
            var (_, volume) = await FatCheckerCases.FormatAsync(FatKind.ExFat);
            await using (volume)
            {
                await using var stream = await volume.OpenAsync("/open.bin", FileMode.CreateNew, FileAccess.Write);
                await stream.WriteAsync(FatVolumeOracleTests.Pattern("open", 3 * volume.Info.ClusterSize));

                var report = await FatChecker.CheckAsync(volume);

                Assert.That(Describe(report), Is.Empty);
                Assert.That(report.Files, Is.EqualTo(1));
                Assert.That(report.UsedClusters + report.FreeClusters, Is.EqualTo(volume.Info.ClusterCount));
            }
        }

        #endregion

        #region Damage Tests

        [TestCaseSource(typeof(FatCheckerCases), nameof(FatCheckerCases.DAMAGE))]
        public async Task DamageIsNamedTest(FatKind kind, string damage, FatProblemKind[] expected, bool isSeenByFsck)
        {
            var disk = await FatCheckerCases.DamagedAsync(kind, damage);
            await using var volume = await FatVolume.MountAsync(disk, TestVolumes.Options(null));

            var report = await FatChecker.CheckAsync(volume);

            var found = report.Problems.Select(problem => problem.Kind).Distinct().Order();
            Assert.That(found, Is.EqualTo(expected.Order()), string.Join("\n", Describe(report)));
            Assert.That(report.IsClean, Is.EqualTo(expected.Length == 0));
            Assert.That(report.BadClusters, Is.EqualTo(damage == "bad-cluster" ? 1 : 0));
        }

        [Test]
        public async Task DamageIsNotRepairedTest()
        {
            var disk = await FatCheckerCases.DamagedAsync(FatKind.ExFat, "bitmap-cleared");
            await using var volume = await FatVolume.MountAsync(disk, TestVolumes.Options(null));

            var first = await FatChecker.CheckAsync(volume);
            var second = await FatChecker.CheckAsync(volume);

            Assert.That(second, Was.EqualTo(first));
        }

        #endregion

        #region Report Tests

        [Test]
        public async Task ProblemsOfOneKindAreListedUpToALimitTest()
        {
            var (_, core, names) = await TestVolumes.CoreAsync(FatKind.Fat16);
            var context = new FatCheckContext(core, names);
            for (int i = 0; i < FatCheckContext.MAX_PER_KIND + 5; i++)
                context.Report(FatProblemKind.BrokenEntry, $"/{i}", null, $"Problem {i}.");
            context.Report(FatProblemKind.LostClusters, null, 7, "Lost.");

            var report = context.ToReport();

            Assert.That(report.Problems, Has.Count.EqualTo(FatCheckContext.MAX_PER_KIND + 2));
            Assert.That(report.Problems.Count(problem => problem.Kind == FatProblemKind.BrokenEntry), Is.EqualTo(FatCheckContext.MAX_PER_KIND + 1));
            Assert.That(report.Problems[^1].Message, Does.StartWith("5 more"));
            Assert.That(report.Problems.Single(problem => problem.Kind == FatProblemKind.LostClusters).Cluster, Is.EqualTo(7u));
        }

        [Test]
        public async Task CrossLinkNamesBothOwnersTest()
        {
            var (_, core, names) = await TestVolumes.CoreAsync(FatKind.Fat16);
            var context = new FatCheckContext(core, names);
            int directory = context.Owners.Add("dir", FatCheckOwners.ROOT);
            int first = context.Owners.Add("a.bin", directory);
            int second = context.Owners.Add("b.bin", FatCheckOwners.ROOT);

            var one = FatCheckChain.Run(context, 5, 3, first);
            var two = FatCheckChain.Run(context, 3, 4, second);
            context.ReportShared(two, "/b.bin");

            var problem = context.ToReport().Problems.Single();
            Assert.That(one.IsWhole, Is.True);
            Assert.That((two.Count, two.SharedCluster, two.SharedWith, two.IsWhole), Is.EqualTo((2L, 5u, first, false)));
            Assert.That(problem.Kind, Is.EqualTo(FatProblemKind.CrossLinked));
            Assert.That(problem.Cluster, Is.EqualTo(5u));
            Assert.That(problem.Path, Is.EqualTo("/b.bin"));
            Assert.That(problem.Message, Does.Contain("/dir/a.bin"));
            Assert.That(context.Owners.Used, Is.EqualTo(5));
        }

        [Test]
        public async Task WalkStopsWhereTheChainComesBackTest()
        {
            var (_, core, names) = await TestVolumes.CoreAsync(FatKind.Fat16);
            await core.Table.SetAsync(10, 11, CancellationToken.None);
            await core.Table.SetAsync(11, 12, CancellationToken.None);
            await core.Table.SetAsync(12, 10, CancellationToken.None);
            var context = new FatCheckContext(core, names);

            var walk = await FatCheckChain.WalkAsync(context, 10, context.Owners.Add("loop.bin", FatCheckOwners.ROOT), CancellationToken.None);

            Assert.That(walk.Count, Is.EqualTo(3));
            Assert.That(walk.Problem, Does.Contain("comes back to cluster 10"));
            Assert.That(context.Owners.Used, Is.EqualTo(3));
        }

        [Test]
        public void OwnersOfAHugeVolumeTakeMemoryOnlyWhereUsedTest()
        {
            var owners = new FatCheckOwners(uint.MaxValue - 10);
            int owner = owners.Add("(allocation bitmap)", 0);

            Assert.That(owners.Claim(uint.MaxValue - 9, owner), Is.Zero);
            Assert.That(owners.Claim(uint.MaxValue - 9, owner), Is.EqualTo(owner));
            Assert.That(owners.IsOwned(uint.MaxValue - 9), Is.True);
            Assert.That(owners.IsOwned(2), Is.False);
            Assert.That(owners.Describe(owner), Is.EqualTo("(allocation bitmap)"));
            Assert.That(owners.Describe(FatCheckOwners.ROOT), Is.EqualTo("/"));
        }

        [Test]
        public void NullVolumeIsRefusedTest()
        {
            Assert.That(async () => await FatChecker.CheckAsync(null!), Throws.ArgumentNullException);
        }

        [Test]
        public async Task DisposedVolumeIsRefusedTest()
        {
            var (_, volume) = await TestVolumes.BlankAsync(FatKind.Fat16);
            await volume.DisposeAsync();

            Assert.That(async () => await FatChecker.CheckAsync(volume), Throws.TypeOf<ObjectDisposedException>());
        }

        #endregion

        #region Tools

        private static IEnumerable<string> Describe(FatCheckReport report)
        {
            return report.Problems.Select(problem => $"{problem.Kind} {problem.Path} {problem.Cluster}: {problem.Message}");
        }

        #endregion
    }
}
