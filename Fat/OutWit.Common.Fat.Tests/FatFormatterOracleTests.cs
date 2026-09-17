using OutWit.Common.Fat.Devices;
using OutWit.Common.Fat.Model;
using OutWit.Common.Fat.Tests.Utils;
using OutWit.Common.Fat.Utils;

namespace OutWit.Common.Fat.Tests
{
    /// <summary>
    /// Volumes the library formats, judged by fsck, the Linux kernel, mtools and dump.exfat:
    /// empty, and again with files in them.
    /// </summary>
    /// <remarks>Ignored where WSL, or Linux with the tools and passwordless sudo, is not available.</remarks>
    [TestFixture]
    [Category(WslOracle.CATEGORY)]
    public class FatFormatterOracleTests
    {
        #region Constants

        private const long MIB = 1L << 20;

        private static readonly FatVolumeOptions LEAVE_OPEN = new() { LeaveOpen = true };

        private static readonly object[] CASES =
        {
            new object[] { FatKind.Fat12, 1474560L, 512, 0, "FLOPPY", false },
            new object[] { FatKind.Fat12, 8 * MIB, 512, 0, null!, true },
            new object[] { FatKind.Fat16, 64 * MIB, 512, 0, "DATA 16", true },
            new object[] { FatKind.Fat16, 32 * MIB, 4096, 0, null!, false },
            new object[] { FatKind.Fat16, 20 * MIB, 512, 512, "SMALL", false },
            new object[] { FatKind.Fat32, 64 * MIB, 512, 512, "CARD", false },
            new object[] { FatKind.Fat32, 600 * MIB, 512, 0, null!, true },
            new object[] { FatKind.Fat32, 300 * MIB, 4096, 0, "FOURK", true },
            new object[] { FatKind.ExFat, 16 * MIB, 512, 0, "ExFAT Vol", false },
            new object[] { FatKind.ExFat, 64 * MIB, 4096, 16384, null!, true },
            new object[] { FatKind.ExFat, 256 * MIB, 512, 131072, "Big cluster", true }
        };

        #endregion

        #region Oracle Tests

        [TestCaseSource(nameof(CASES))]
        public async Task FormattedVolumePassesTheLinuxToolsTest(FatKind kind, long bytes, int sectorSize, int clusterSize, string? label, bool isPartitioned)
        {
            WslOracle.Require();
            var disk = new BlockDeviceMemory(bytes / sectorSize, sectorSize);
            var options = new FatFormatOptions
            {
                Kind = kind,
                ClusterSize = clusterSize == 0 ? null : clusterSize,
                Label = label,
                RootEntries = bytes < 2 * MIB ? 224 : 512
            };
            if (isPartitioned)
                await FatFormatter.FormatDiskAsync(disk, options);
            else
                await FatFormatter.FormatAsync(disk, options);
            long start = isPartitioned ? MIB / sectorSize : 0;

            await AssertCleanAsync(disk, start);

            await using (var volume = await FatVolume.MountDiskAsync(disk, LEAVE_OPEN))
            {
                await volume.CreateDirectoryAsync("/Made after formatting/deeper");
                await volume.WriteAllBytesAsync("/A long name for a small file.txt", FatVolumeOracleTests.Pattern("lfn", 300));
                await volume.WriteAllBytesAsync("/Made after formatting/data.bin", FatVolumeOracleTests.Pattern("dat", 3 * volume.Info.ClusterSize + 1));
                for (int i = 0; i < 30; i++)
                    await volume.WriteAllBytesAsync($"/Made after formatting/deeper/entry {i:D2}.txt", FatVolumeOracleTests.Pattern($"e{i:D2}", i * 37));
            }

            await AssertCleanAsync(disk, start);
        }

        #endregion

        #region Tools

        private static async Task AssertCleanAsync(BlockDeviceMemory disk, long partitionStart)
        {
            await using var volume = await FatVolume.MountDiskAsync(disk, LEAVE_OPEN);
            var expected = await OracleExpectation.FromVolumeAsync(volume, partitionStart);

            var problems = await WslOracle.CheckAsync(disk, expected);

            Assert.That(problems, Is.Empty);
        }

        #endregion
    }
}
