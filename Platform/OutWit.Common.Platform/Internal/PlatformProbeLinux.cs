using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using OutWit.Common.Platform.Models.SystemInfo;

namespace OutWit.Common.Platform.Internal
{
    /// <summary>
    /// Linux-specific platform probe. Reads CPU/memory details from
    /// <c>/proc/cpuinfo</c> and <c>/proc/meminfo</c>, storage rotational flag
    /// from <c>/sys/block/{dev}/queue/rotational</c>, and GPU list (best-effort)
    /// from <c>lspci -mm</c>. User-activity detection is not reliable from
    /// a headless library — defaults to <c>true</c>.
    /// </summary>
    internal class PlatformProbeLinux : IPlatformProbe
    {
        #region Constants

        private const string CPU_INFO_PATH = "/proc/cpuinfo";
        private const string MEM_INFO_PATH = "/proc/meminfo";
        private const string STAT_PATH = "/proc/stat";

        #endregion

        #region Fields

        private CpuStat? m_lastCpuStat;

        #endregion

        #region IPlatformProbe

        public virtual PlatformKind Kind => PlatformKind.Linux;

        public virtual string GetCpuModelName()
        {
            try
            {
                if (!File.Exists(CPU_INFO_PATH))
                    return string.Empty;

                foreach (var line in File.ReadLines(CPU_INFO_PATH))
                {
                    if (!line.StartsWith("model name", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var separator = line.IndexOf(':');
                    if (separator < 0)
                        continue;

                    return line.Substring(separator + 1).Trim();
                }
            }
            catch
            {
                return string.Empty;
            }

            return string.Empty;
        }

        public virtual IReadOnlyList<SystemGpuInfo> GetGpus()
        {
            // Best-effort: parse `lspci -mm` if available. VRAM cannot be read
            // reliably without root + per-vendor probes, so VRamMb defaults to 0.
            var gpus = new List<SystemGpuInfo>();
            var output = TryRunCommand("lspci", "-mm");
            if (string.IsNullOrEmpty(output))
                return gpus;

            // Format example: 00:02.0 "VGA compatible controller" "Intel Corporation" "Iris Plus Graphics" -r02 "Intel Corporation" "Iris Plus Graphics"
            var regex = new Regex("\"(?<class>[^\"]+)\"\\s+\"(?<vendor>[^\"]+)\"\\s+\"(?<name>[^\"]+)\"");
            foreach (var line in output!.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var match = regex.Match(line);
                if (!match.Success)
                    continue;

                var classText = match.Groups["class"].Value.ToUpperInvariant();
                if (!classText.Contains("VGA") && !classText.Contains("3D") && !classText.Contains("DISPLAY"))
                    continue;

                var vendor = match.Groups["vendor"].Value;
                var name = match.Groups["name"].Value;

                gpus.Add(new SystemGpuInfo
                {
                    ModelName = $"{vendor} {name}".Trim(),
                    VRamMb = 0,
                    GpuType = GpuClassifier.DetectType(vendor, name),
                    SupportedFeatures = GpuClassifier.DetectFeatures(vendor, name)
                });
            }

            return gpus;
        }

        public virtual SystemStorageType GetStorageType(string rootPath)
        {
            // Try to find the block device backing the path via `df`,
            // then check /sys/block/{dev}/queue/rotational.
            try
            {
                var dfOutput = TryRunCommand("df", $"--output=source \"{rootPath}\"");
                if (string.IsNullOrWhiteSpace(dfOutput))
                    return SystemStorageType.Unknown;

                var lines = dfOutput!.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length < 2)
                    return SystemStorageType.Unknown;

                var source = lines[1].Trim();
                if (string.IsNullOrEmpty(source))
                    return SystemStorageType.Unknown;

                // Strip leading /dev/ and trailing partition digits (e.g. /dev/nvme0n1p2 → nvme0n1).
                var devName = Path.GetFileName(source);
                if (devName.StartsWith("nvme", StringComparison.OrdinalIgnoreCase))
                    return SystemStorageType.NVMe;

                var trimmed = TrimPartitionSuffix(devName);
                var rotationalPath = $"/sys/block/{trimmed}/queue/rotational";
                if (!File.Exists(rotationalPath))
                    return SystemStorageType.Unknown;

                var rotational = File.ReadAllText(rotationalPath).Trim();
                return rotational == "0" ? SystemStorageType.SSD : SystemStorageType.HDD;
            }
            catch
            {
                return SystemStorageType.Unknown;
            }
        }

        public virtual double GetCpuLoadPercent()
        {
            try
            {
                if (!File.Exists(STAT_PATH))
                    return 0.0;

                var current = ReadCpuStat();
                if (current == null)
                    return 0.0;

                if (m_lastCpuStat == null)
                {
                    m_lastCpuStat = current;
                    return 0.0;
                }

                var deltaTotal = current.Value.Total - m_lastCpuStat.Value.Total;
                var deltaIdle = current.Value.Idle - m_lastCpuStat.Value.Idle;
                m_lastCpuStat = current;

                if (deltaTotal <= 0)
                    return 0.0;

                return 100.0 * (deltaTotal - deltaIdle) / deltaTotal;
            }
            catch
            {
                return 0.0;
            }
        }

        public virtual long GetAvailableRamMb()
        {
            try
            {
                if (!File.Exists(MEM_INFO_PATH))
                    return 0;

                foreach (var line in File.ReadLines(MEM_INFO_PATH))
                {
                    if (!line.StartsWith("MemAvailable:", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2)
                        return 0;

                    if (long.TryParse(parts[1], out var kib))
                        return kib / 1024L;
                }
            }
            catch
            {
                return 0;
            }

            return 0;
        }

        public virtual bool IsUserActive()
        {
            // Reliable idle detection on Linux requires X11/Wayland queries from
            // the active session, which a headless library cannot do safely.
            // Conservative default: assume active so the scheduler does not
            // preempt unexpectedly.
            return true;
        }

        public virtual string? GetRawMachineIdentity()
        {
            foreach (var path in new[] { "/etc/machine-id", "/var/lib/dbus/machine-id" })
            {
                try
                {
                    if (!File.Exists(path))
                        continue;

                    var value = File.ReadAllText(path).Trim();
                    if (!string.IsNullOrWhiteSpace(value))
                        return value;
                }
                catch
                {
                    // Continue with the next probe path.
                }
            }

            return null;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
        }

        #endregion

        #region Tools

        protected static string? TryRunCommand(string fileName, string arguments)
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

        private static string TrimPartitionSuffix(string devName)
        {
            // sda1 → sda, mmcblk0p1 → mmcblk0, nvme0n1p2 → nvme0n1
            // Algorithm: trim trailing digits, then trim trailing 'p' if a
            // digit-letter-digit prefix remains (nvme, mmcblk style).
            var end = devName.Length;
            while (end > 0 && char.IsDigit(devName[end - 1]))
                end--;

            if (end > 0 && devName[end - 1] == 'p'
                && end >= 2 && char.IsDigit(devName[end - 2]))
            {
                end--;
            }

            return devName.Substring(0, end);
        }

        private static CpuStat? ReadCpuStat()
        {
            try
            {
                var firstLine = File.ReadLines(STAT_PATH).FirstOrDefault();
                if (firstLine == null || !firstLine.StartsWith("cpu ", StringComparison.Ordinal))
                    return null;

                var parts = firstLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                // parts[0] = "cpu", then user nice system idle iowait irq softirq steal guest guest_nice
                if (parts.Length < 5)
                    return null;

                long total = 0;
                for (int i = 1; i < parts.Length; i++)
                {
                    if (long.TryParse(parts[i], out var value))
                        total += value;
                }

                long.TryParse(parts[4], out var idle);

                return new CpuStat(total, idle);
            }
            catch
            {
                return null;
            }
        }

        private readonly struct CpuStat
        {
            public CpuStat(long total, long idle)
            {
                Total = total;
                Idle = idle;
            }

            public long Total { get; }
            public long Idle { get; }
        }

        #endregion
    }
}
