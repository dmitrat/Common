using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using OutWit.Common.Platform.Models.SystemInfo;

namespace OutWit.Common.Platform.Internal
{
    /// <summary>
    /// macOS-specific platform probe. Reads CPU/GPU/storage details via
    /// <c>sysctl</c> and <c>system_profiler</c>, CPU/RAM load via <c>top</c>
    /// (best-effort). Idle detection is not reliable from a headless library —
    /// defaults to <c>true</c>.
    /// </summary>
    internal sealed class PlatformProbeMacOS : IPlatformProbe
    {
        #region IPlatformProbe

        public PlatformKind Kind => PlatformKind.MacOS;

        public string GetCpuModelName()
        {
            return TryRunCommand("sysctl", "-n machdep.cpu.brand_string")?.Trim() ?? string.Empty;
        }

        public IReadOnlyList<SystemGpuInfo> GetGpus()
        {
            // `system_profiler SPDisplaysDataType` produces a verbose human-readable
            // tree. Parse the lines we care about (Chipset Model / Vendor / VRAM).
            var gpus = new List<SystemGpuInfo>();
            var output = TryRunCommand("system_profiler", "SPDisplaysDataType");
            if (string.IsNullOrEmpty(output))
                return gpus;

            string? currentModel = null;
            string? currentVendor = null;
            long currentVramMb = 0;

            foreach (var rawLine in output!.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var line = rawLine.Trim();

                if (line.StartsWith("Chipset Model:", StringComparison.OrdinalIgnoreCase))
                {
                    // Flush previous block if any.
                    AppendGpu(gpus, currentModel, currentVendor, currentVramMb);
                    currentModel = line.Substring("Chipset Model:".Length).Trim();
                    currentVendor = null;
                    currentVramMb = 0;
                    continue;
                }

                if (line.StartsWith("Vendor:", StringComparison.OrdinalIgnoreCase))
                {
                    currentVendor = line.Substring("Vendor:".Length).Trim();
                    continue;
                }

                if (line.StartsWith("VRAM (Total):", StringComparison.OrdinalIgnoreCase)
                    || line.StartsWith("VRAM (Dynamic, Max):", StringComparison.OrdinalIgnoreCase))
                {
                    var value = line.Substring(line.IndexOf(':') + 1).Trim();
                    currentVramMb = ParseVramMb(value);
                }
            }

            AppendGpu(gpus, currentModel, currentVendor, currentVramMb);
            return gpus;
        }

        public SystemStorageType GetStorageType(string rootPath)
        {
            // `diskutil info -plist /` is the canonical source, but parsing plist
            // adds a dependency. Best-effort: try `diskutil info <path>` text and
            // look for "Solid State" + "Protocol".
            var output = TryRunCommand("diskutil", $"info \"{rootPath}\"");
            if (string.IsNullOrEmpty(output))
                return SystemStorageType.Unknown;

            var text = output!;
            if (Regex.IsMatch(text, @"Protocol:\s+(NVMe|Apple Fabric)", RegexOptions.IgnoreCase))
                return SystemStorageType.NVMe;

            if (Regex.IsMatch(text, @"Solid State:\s+Yes", RegexOptions.IgnoreCase))
                return SystemStorageType.SSD;

            if (Regex.IsMatch(text, @"Solid State:\s+No", RegexOptions.IgnoreCase))
                return SystemStorageType.HDD;

            return SystemStorageType.Unknown;
        }

        public double GetCpuLoadPercent()
        {
            // `top -l 1 -n 0 -s 0` writes a snapshot like "CPU usage: 12.5% user, 6.3% sys, 81.2% idle"
            var output = TryRunCommand("top", "-l 1 -n 0 -s 0");
            if (string.IsNullOrEmpty(output))
                return 0.0;

            var match = Regex.Match(output!, @"(\d+(?:\.\d+)?)%\s+idle", RegexOptions.IgnoreCase);
            if (!match.Success || !double.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var idle))
                return 0.0;

            return Math.Max(0.0, Math.Min(100.0, 100.0 - idle));
        }

        public long GetAvailableRamMb()
        {
            // vm_stat reports pages of "Pages free" and "Pages inactive" — combine.
            var output = TryRunCommand("vm_stat", string.Empty);
            if (string.IsNullOrEmpty(output))
                return 0;

            long pageSize = 4096;
            long freePages = 0;
            long inactivePages = 0;

            foreach (var rawLine in output!.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var line = rawLine.Trim();

                if (line.StartsWith("Mach Virtual Memory Statistics", StringComparison.OrdinalIgnoreCase))
                {
                    var sizeMatch = Regex.Match(line, @"page size of (\d+) bytes");
                    if (sizeMatch.Success && long.TryParse(sizeMatch.Groups[1].Value, out var ps))
                        pageSize = ps;
                    continue;
                }

                if (line.StartsWith("Pages free:", StringComparison.OrdinalIgnoreCase))
                    freePages = ParsePageCount(line);
                else if (line.StartsWith("Pages inactive:", StringComparison.OrdinalIgnoreCase))
                    inactivePages = ParsePageCount(line);
            }

            return (freePages + inactivePages) * pageSize / (1024L * 1024L);
        }

        public bool IsUserActive()
        {
            // Reliable detection requires CGEventSourceSecondsSinceLastEventType
            // bridged through native interop, which is out of scope here.
            return true;
        }

        public string? GetRawMachineIdentity()
        {
            return TryRunCommand("sysctl", "-n kern.uuid")?.Trim();
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
        }

        #endregion

        #region Tools

        private static void AppendGpu(List<SystemGpuInfo> gpus, string? model, string? vendor, long vramMb)
        {
            if (string.IsNullOrWhiteSpace(model))
                return;

            var name = model!;
            var vendorText = vendor ?? string.Empty;

            gpus.Add(new SystemGpuInfo
            {
                ModelName = name,
                VRamMb = vramMb,
                GpuType = GpuClassifier.DetectType(vendorText, name),
                SupportedFeatures = GpuClassifier.DetectFeatures(vendorText, name)
            });
        }

        private static long ParseVramMb(string text)
        {
            // Examples: "8 GB", "2048 MB"
            var match = Regex.Match(text, @"(\d+(?:\.\d+)?)\s*(GB|MB)", RegexOptions.IgnoreCase);
            if (!match.Success)
                return 0;

            if (!double.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value))
                return 0;

            return match.Groups[2].Value.ToUpperInvariant() == "GB"
                ? (long)(value * 1024L)
                : (long)value;
        }

        private static long ParsePageCount(string line)
        {
            var match = Regex.Match(line, @"(\d+)");
            return match.Success && long.TryParse(match.Groups[1].Value, out var value) ? value : 0L;
        }

        private static string? TryRunCommand(string fileName, string arguments)
        {
            try
            {
                using var process = new Process();
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(3000);

                return process.ExitCode == 0 ? output : null;
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }
}
