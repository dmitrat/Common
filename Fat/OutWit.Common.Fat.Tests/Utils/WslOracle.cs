using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using OutWit.Common.Fat.Devices;

namespace OutWit.Common.Fat.Tests.Utils
{
    /// <summary>
    /// Runs <c>Fixtures/check_image.py</c> on an image the library wrote: in WSL on
    /// Windows, directly on Linux. Tests that use it are ignored where it cannot run.
    /// </summary>
    internal static class WslOracle
    {
        #region Constants

        public const string CATEGORY = "Wsl";

        public const string SCRIPT = "check_image.py";

        private const string DISTRIBUTION_VARIABLE = "OUTWIT_FAT_WSL_DISTRIBUTION";

        private const string DEFAULT_DISTRIBUTION = "Ubuntu";

        private static readonly TimeSpan TIMEOUT = TimeSpan.FromMinutes(2);

        private static readonly Lazy<string?> UNAVAILABLE = new(Probe);

        #endregion

        #region Functions

        /// <summary>
        /// Ignores the calling test when the oracle cannot run here.
        /// </summary>
        public static void Require()
        {
            if (UNAVAILABLE.Value is { } reason)
                Assert.Ignore($"The WSL oracle is not available: {reason}");
        }

        /// <summary>
        /// Saves the disk and has the Linux tools judge it against what the volume on it
        /// holds according to the library.
        /// </summary>
        /// <returns>The problems found; empty when the image is clean and agrees.</returns>
        public static Task<IReadOnlyList<string>> CheckAsync(BlockDeviceMemory disk, OracleExpectation expected)
        {
            return RunCheckAsync(disk, expected, isFsckOnly: false);
        }

        /// <summary>
        /// Saves the disk and has fsck alone judge it, for a volume damaged on purpose.
        /// </summary>
        /// <returns>What fsck complains of; empty when it finds nothing.</returns>
        public static Task<IReadOnlyList<string>> FsckAsync(BlockDeviceMemory disk, FatVolumeInfo info, long partitionStart = 0)
        {
            var expected = new OracleExpectation
            {
                Kind = info.Kind.ToString().ToLowerInvariant(),
                SectorSize = info.SectorSize,
                PartitionStart = partitionStart,
                Entries = new List<OracleEntry>()
            };
            return RunCheckAsync(disk, expected, isFsckOnly: true);
        }

        private static async Task<IReadOnlyList<string>> RunCheckAsync(BlockDeviceMemory disk, OracleExpectation expected, bool isFsckOnly)
        {
            Require();

            string work = Directory.CreateTempSubdirectory("outwit-fat-oracle-").FullName;
            try
            {
                string image = Path.Combine(work, "image.img");
                string expectation = Path.Combine(work, "expected.json");
                await using (var file = File.Create(image))
                    await disk.SaveAsync(file);
                await File.WriteAllTextAsync(expectation, JsonSerializer.Serialize(expected, OracleExpectation.OPTIONS));

                var (exitCode, output, error) = isFsckOnly
                    ? await RunAsync(Script, "--fsck", image, expectation)
                    : await RunAsync(Script, image, expectation);
                if (string.IsNullOrWhiteSpace(output))
                    return new[] { $"{SCRIPT} failed with exit code {exitCode}: {error}" };

                var result = JsonSerializer.Deserialize<OracleResult>(output.Trim(), OracleExpectation.OPTIONS)
                             ?? throw new InvalidDataException($"{SCRIPT} printed nothing useful: {output}");
                if (exitCode != 0 && result.Problems.Count == 0)
                    return new[] { $"{SCRIPT} failed with exit code {exitCode}: {error}" };

                TestContext.Out.WriteLine(result.Fsck);
                return result.Problems;
            }
            finally
            {
                Directory.Delete(work, recursive: true);
            }
        }

        private static string? Probe()
        {
            if (!File.Exists(Script))
                return $"{Script} is missing.";
            if (!OperatingSystem.IsWindows() && !OperatingSystem.IsLinux())
                return $"{RuntimeInformation.OSDescription} is neither Windows nor Linux.";

            try
            {
                var (exitCode, output, error) = RunAsync(Script, "--probe").GetAwaiter().GetResult();
                return exitCode == 0 ? null : $"{output.Trim()} {error.Trim()}".Trim();
            }
            catch (Exception failure) when (failure is IOException or InvalidOperationException or System.ComponentModel.Win32Exception or TimeoutException)
            {
                return failure.Message;
            }
        }

        private static async Task<(int ExitCode, string Output, string Error)> RunAsync(params string[] arguments)
        {
            var start = new ProcessStartInfo
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            if (OperatingSystem.IsWindows())
            {
                start.FileName = "wsl.exe";
                foreach (string argument in new[] { "-d", Distribution, "--", "python3" })
                    start.ArgumentList.Add(argument);
                foreach (string argument in arguments)
                    start.ArgumentList.Add(argument.StartsWith("--", StringComparison.Ordinal) ? argument : ToLinuxPath(argument));
            }
            else
            {
                start.FileName = "python3";
                foreach (string argument in arguments)
                    start.ArgumentList.Add(argument);
            }

            using var process = Process.Start(start) ?? throw new InvalidOperationException($"{start.FileName} did not start.");
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TIMEOUT);
            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                process.Kill(entireProcessTree: true);
                throw new TimeoutException($"{SCRIPT} did not finish in {TIMEOUT}.");
            }

            return (process.ExitCode, await output, await error);
        }

        /// <summary>
        /// Where WSL mounts a Windows path, as its default automount root puts it.
        /// </summary>
        private static string ToLinuxPath(string path)
        {
            string full = Path.GetFullPath(path);
            return $"/mnt/{char.ToLowerInvariant(full[0])}{full[2..].Replace('\\', '/')}";
        }

        #endregion

        #region Properties

        private static string Script => Path.Combine(ReferenceImages.Root, SCRIPT);

        private static string Distribution => Environment.GetEnvironmentVariable(DISTRIBUTION_VARIABLE) is { Length: > 0 } name
            ? name
            : DEFAULT_DISTRIBUTION;

        #endregion
    }
}
