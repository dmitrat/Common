using OutWit.Common.Fat.Tests.Utils;

namespace OutWit.Common.Fat.Tests
{
    /// <summary>
    /// The damage the checker names, shown to <c>fsck.vfat -n</c> and <c>fsck.exfat -n</c>:
    /// fsck must find something wherever the cases say it does, and nothing where they say
    /// the checker goes further than fsck.
    /// </summary>
    /// <remarks>Ignored where WSL, or Linux with the tools and passwordless sudo, is not available.</remarks>
    [TestFixture]
    [Category(WslOracle.CATEGORY)]
    public class FatCheckerOracleTests
    {
        #region Oracle Tests

        [TestCase(FatKind.Fat12)]
        [TestCase(FatKind.Fat16)]
        [TestCase(FatKind.Fat32)]
        [TestCase(FatKind.ExFat)]
        public async Task FsckSaysNothingOfTheUndamagedVolumeTest(FatKind kind)
        {
            WslOracle.Require();
            var (disk, volume) = await FatCheckerCases.FormatAsync(kind);
            await using (volume)
                await FatDamage.PopulateAsync(volume);

            var problems = await WslOracle.FsckAsync(disk, volume.Info);

            Assert.That(problems, Is.Empty);
        }

        [TestCaseSource(typeof(FatCheckerCases), nameof(FatCheckerCases.DAMAGE))]
        public async Task FsckSeesWhatTheCheckerSeesTest(FatKind kind, string damage, FatProblemKind[] expected, bool isSeenByFsck)
        {
            WslOracle.Require();
            var disk = await FatCheckerCases.DamagedAsync(kind, damage);
            var info = (await FatDetector.DetectVolumeAsync(disk))!;

            var problems = await WslOracle.FsckAsync(disk, info);

            TestContext.Out.WriteLine(string.Join("\n", problems));
            Assert.That(problems.Count > 0, Is.EqualTo(isSeenByFsck), string.Join("\n", problems));
        }

        #endregion
    }
}
