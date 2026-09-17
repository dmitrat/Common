using OutWit.Common.Fat.Devices;
using OutWit.Common.Fat.Model;
using OutWit.Common.Fat.Tests.Utils;
using OutWit.Common.Fat.Utils;

namespace OutWit.Common.Fat.Tests
{
    /// <summary>
    /// Labels and attributes the library sets, as fsck, mtools, exfatlabel and dump.exfat
    /// read them.
    /// </summary>
    /// <remarks>Ignored where WSL, or Linux with the tools and passwordless sudo, is not available.</remarks>
    [TestFixture]
    [Category(WslOracle.CATEGORY)]
    public class FatVolumeEditOracleTests
    {
        #region Constants

        private static readonly object[] CASES =
        {
            new object[] { FatKind.Fat12, "floppy" },
            new object[] { FatKind.Fat16, "Data 16" },
            new object[] { FatKind.Fat32, "CARD-2026" },
            new object[] { FatKind.ExFat, "Метка тома" }
        };

        #endregion

        #region Oracle Tests

        [TestCaseSource(nameof(CASES))]
        public async Task LabelAndAttributesPassTheLinuxToolsTest(FatKind kind, string label)
        {
            WslOracle.Require();
            var (disk, volume) = await FatCheckerCases.FormatAsync(kind);
            await using (volume)
            {
                await FatDamage.PopulateAsync(volume);
                await volume.SetLabelAsync(label);
                await volume.SetAttributesAsync(FatDamage.FIRST, FatAttributes.ReadOnly);
                await volume.SetAttributesAsync(FatDamage.SECOND, FatAttributes.Hidden | FatAttributes.System | FatAttributes.Archive);
                await volume.SetAttributesAsync(FatDamage.LONG_NAME, FatAttributes.None);
                await volume.SetAttributesAsync(FatDamage.INNER, FatAttributes.Hidden);
            }

            await AssertCleanAsync(disk);

            await using (var again = await TestVolumes.RemountAsync(disk))
            {
                await again.SetLabelAsync(null);
                await again.SetAttributesAsync(FatDamage.SECOND, FatAttributes.Archive);
            }

            await AssertCleanAsync(disk);
        }

        [TestCase(FatKind.Fat16)]
        [TestCase(FatKind.ExFat)]
        public async Task OracleNoticesAttributesTheToolsDoNotSeeTest(FatKind kind)
        {
            WslOracle.Require();
            var (disk, volume) = await FatCheckerCases.FormatAsync(kind);
            OracleExpectation expected;
            await using (volume)
            {
                await volume.WriteAllBytesAsync("/hidden.txt", new byte[10]);
                await volume.SetAttributesAsync("/hidden.txt", FatAttributes.Hidden);
                expected = await OracleExpectation.FromVolumeAsync(volume);
            }

            expected.Entries.Single(entry => entry.Path == "/hidden.txt").Attributes = (int)FatAttributes.Archive;
            var problems = await WslOracle.CheckAsync(disk, expected);

            Assert.That(problems, Has.Some.Contains("/hidden.txt").And.Some.Contains("attributes"));
        }

        #endregion

        #region Tools

        private static async Task AssertCleanAsync(BlockDeviceMemory disk)
        {
            await using var volume = await TestVolumes.RemountAsync(disk);
            var expected = await OracleExpectation.FromVolumeAsync(volume);
            var report = await FatChecker.CheckAsync(volume);

            var problems = await WslOracle.CheckAsync(disk, expected);

            Assert.That(problems, Is.Empty);
            Assert.That(report.IsClean, Is.True);
        }

        #endregion
    }
}
