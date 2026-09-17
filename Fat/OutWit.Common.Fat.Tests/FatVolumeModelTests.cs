using OutWit.Common.Fat.Tests.Utils;

namespace OutWit.Common.Fat.Tests
{
    /// <summary>
    /// Random sequences of changes, checked against a model of what the volume should hold.
    /// </summary>
    [TestFixture]
    public class FatVolumeModelTests
    {
        #region Constants

        private const int STEPS = 400;

        private const int CHECK_EVERY = 25;

        #endregion

        #region Model Tests

        [TestCase(FatKind.Fat12, 1, 150, 3000)]
        [TestCase(FatKind.Fat12, 2, 2000, 20000)]
        [TestCase(FatKind.Fat16, 3, 4200, 30000)]
        [TestCase(FatKind.Fat16, 4, 5000, 400)]
        [TestCase(FatKind.Fat32, 5, 100, 4000)]
        [TestCase(FatKind.Fat32, 6, 3000, 40000)]
        [TestCase(FatKind.ExFat, 8, 120, 4000)]
        [TestCase(FatKind.ExFat, 9, 3000, 40000)]
        [TestCase(FatKind.ExFat, 10, 800, 300)]
        public async Task RandomChangesMatchTheModelTest(FatKind kind, int seed, int clusters, int maxBytes)
        {
            var (disk, volume) = await TestVolumes.BlankAsync(kind, clusters, rootEntries: 32);
            var model = await VolumeModel.ReadAsync(volume);
            var operations = new RandomOperations(volume, model, seed, maxBytes);

            await RunAsync(operations, STEPS);
            await VolumeConsistency.AssertAsync(volume);
            long free = await volume.CountFreeClustersAsync();
            await volume.DisposeAsync();

            await using var reopened = await TestVolumes.RemountAsync(disk);
            await model.AssertMatchesAsync(reopened);
            await VolumeConsistency.AssertAsync(reopened);
            if (kind == FatKind.Fat32)
                Assert.That((await BlankVolume.ReadFsInfoAsync(disk)).FreeCount, Is.EqualTo(free));

            TestContext.Out.WriteLine($"{operations.NoSpace} of {STEPS} changes found the volume full; {model.Files().Count} files, " +
                                      $"{model.Directories().Count} directories left.");
        }

        [TestCase("fat16-mbr-c16", 7)]
        [TestCase("exfat-mbr-c1", 11)]
        public async Task ReferenceImageTakesRandomChangesTest(string name, int seed)
        {
            var (disk, volume) = await TestVolumes.ReferenceAsync(name);
            var model = await VolumeModel.ReadAsync(volume);
            var operations = new RandomOperations(volume, model, seed, 100000);

            await RunAsync(operations, 200);
            await volume.DisposeAsync();

            await using var reopened = await TestVolumes.RemountAsync(disk);
            await model.AssertMatchesAsync(reopened);
            await VolumeConsistency.AssertAsync(reopened);
        }

        #endregion

        #region Tools

        /// <summary>
        /// Runs the operations and shows the last of them when one fails.
        /// </summary>
        internal static async Task RunAsync(RandomOperations operations, int steps)
        {
            try
            {
                await operations.RunAsync(steps, CHECK_EVERY);
            }
            catch
            {
                TestContext.Out.WriteLine(string.Join(Environment.NewLine, operations.Log.TakeLast(40)));
                throw;
            }
        }

        #endregion
    }
}
