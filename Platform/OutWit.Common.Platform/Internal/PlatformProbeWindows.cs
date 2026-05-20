using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;
using OutWit.Common.Platform.Models.SystemInfo;

namespace OutWit.Common.Platform.Internal
{
    /// <summary>
    /// Windows-specific platform probe. Reads CPU/GPU/storage details via WMI
    /// + registry, and CPU/RAM load via PerformanceCounter. User-activity
    /// detection uses <c>GetLastInputInfo</c>.
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal sealed class PlatformProbeWindows : IPlatformProbe
    {
        #region Constants

        private const int USER_IDLE_THRESHOLD_SECONDS = 300;
        private const string WINDOWS_MACHINE_GUID_KEY = @"SOFTWARE\Microsoft\Cryptography";
        private const string WINDOWS_MACHINE_GUID_VALUE = "MachineGuid";

        #endregion

        #region Fields

        private PerformanceCounter? m_cpuCounter;
        private PerformanceCounter? m_ramCounter;
        private bool m_countersInitialized;
        private bool m_disposed;

        #endregion

        #region IPlatformProbe

        public PlatformKind Kind => PlatformKind.Windows;

        public string GetCpuModelName()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
                return key?.GetValue("ProcessorNameString")?.ToString()?.Trim() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        public IReadOnlyList<SystemGpuInfo> GetGpus()
        {
            var gpus = new List<SystemGpuInfo>();
            var vramByName = ReadGpuVramFromRegistry();

            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT Name, AdapterRAM, AdapterCompatibility FROM Win32_VideoController");

                foreach (var obj in searcher.Get())
                {
                    var name = obj["Name"]?.ToString() ?? string.Empty;
                    var vendor = obj["AdapterCompatibility"]?.ToString() ?? string.Empty;
                    var vramMb = ResolveGpuMemoryMb(obj, name, vramByName);

                    gpus.Add(new SystemGpuInfo
                    {
                        ModelName = name,
                        VRamMb = vramMb,
                        GpuType = GpuClassifier.DetectType(vendor, name),
                        SupportedFeatures = GpuClassifier.DetectFeatures(vendor, name)
                    });
                }
            }
            catch
            {
                return gpus;
            }

            return gpus;
        }

        public SystemStorageType GetStorageType(string rootPath)
        {
            try
            {
                var driveLetter = rootPath.TrimEnd('\\', '/');

                using var logicalDisk = new ManagementObjectSearcher(
                    $"SELECT DeviceID FROM Win32_LogicalDisk WHERE DeviceID='{driveLetter}'");

                foreach (var logicalDiskObject in logicalDisk.Get())
                {
                    var deviceId = logicalDiskObject["DeviceID"]?.ToString();
                    if (string.IsNullOrEmpty(deviceId))
                        continue;

                    using var diskToPartition = new ManagementObjectSearcher(
                        $"ASSOCIATORS OF {{Win32_LogicalDisk.DeviceID='{deviceId}'}} WHERE AssocClass=Win32_LogicalDiskToPartition");

                    foreach (var partition in diskToPartition.Get())
                    {
                        var partitionId = partition["DeviceID"]?.ToString();
                        if (string.IsNullOrEmpty(partitionId))
                            continue;

                        using var partitionToDisk = new ManagementObjectSearcher(
                            $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{partitionId}'}} WHERE AssocClass=Win32_DiskDriveToDiskPartition");

                        foreach (var disk in partitionToDisk.Get())
                        {
                            var mediaType = disk["MediaType"]?.ToString()?.ToUpperInvariant() ?? string.Empty;
                            var model = disk["Model"]?.ToString()?.ToUpperInvariant() ?? string.Empty;
                            var interfaceType = disk["InterfaceType"]?.ToString()?.ToUpperInvariant() ?? string.Empty;

                            if (interfaceType.Contains("NVME") || model.Contains("NVME"))
                                return SystemStorageType.NVMe;

                            if (mediaType.Contains("SSD") || model.Contains("SSD"))
                                return SystemStorageType.SSD;

                            var detected = DetectFromPhysicalDisk(disk["Index"]);
                            if (detected != SystemStorageType.Unknown)
                                return detected;
                        }
                    }
                }
            }
            catch
            {
                return SystemStorageType.Unknown;
            }

            return SystemStorageType.Unknown;
        }

        public double GetCpuLoadPercent()
        {
            EnsureCountersInitialized();
            try
            {
                return m_cpuCounter?.NextValue() ?? 0.0;
            }
            catch
            {
                return 0.0;
            }
        }

        public long GetAvailableRamMb()
        {
            EnsureCountersInitialized();
            try
            {
                return (long)(m_ramCounter?.NextValue() ?? 0.0);
            }
            catch
            {
                return 0;
            }
        }

        public bool IsUserActive()
        {
            try
            {
                var lastInput = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>() };
                if (!GetLastInputInfo(ref lastInput))
                    return true;

                var idleMs = unchecked((uint)Environment.TickCount) - lastInput.dwTime;
                var idleSeconds = idleMs / 1000.0;
                return idleSeconds < USER_IDLE_THRESHOLD_SECONDS;
            }
            catch
            {
                return true;
            }
        }

        public string? GetRawMachineIdentity()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(WINDOWS_MACHINE_GUID_KEY);
                return key?.GetValue(WINDOWS_MACHINE_GUID_VALUE)?.ToString();
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (m_disposed)
                return;

            m_cpuCounter?.Dispose();
            m_ramCounter?.Dispose();
            m_disposed = true;
        }

        #endregion

        #region Tools

        private void EnsureCountersInitialized()
        {
            if (m_countersInitialized)
                return;

            try
            {
                m_cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                m_cpuCounter.NextValue(); // First reading is always 0 — discard.
                m_ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            }
            catch
            {
                m_cpuCounter?.Dispose();
                m_ramCounter?.Dispose();
                m_cpuCounter = null;
                m_ramCounter = null;
            }

            m_countersInitialized = true;
        }

        private static long ResolveGpuMemoryMb(ManagementBaseObject obj, string name, IReadOnlyDictionary<string, long> vramByName)
        {
            if (vramByName.TryGetValue(name, out var registryVramMb) && registryVramMb > 0)
                return registryVramMb;

            var adapterRam = Convert.ToInt64(obj["AdapterRAM"] ?? 0L);
            return adapterRam / (1024L * 1024L);
        }

        private static Dictionary<string, long> ReadGpuVramFromRegistry()
        {
            var result = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

            try
            {
                const string videoClassKey = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}";

                using var classKey = Registry.LocalMachine.OpenSubKey(videoClassKey);
                if (classKey == null)
                    return result;

                foreach (var subKeyName in classKey.GetSubKeyNames())
                {
                    if (!int.TryParse(subKeyName, out _))
                        continue;

                    try
                    {
                        using var adapterKey = classKey.OpenSubKey(subKeyName);
                        if (adapterKey == null)
                            continue;

                        var description = adapterKey.GetValue("DriverDesc")?.ToString();
                        if (string.IsNullOrEmpty(description))
                            continue;

                        var memorySize = adapterKey.GetValue("HardwareInformation.qwMemorySize");
                        if (memorySize is long longValue && longValue > 0)
                        {
                            result[description] = longValue / (1024L * 1024L);
                        }
                        else if (memorySize is byte[] bytes && bytes.Length >= 8)
                        {
                            var value = BitConverter.ToInt64(bytes, 0);
                            if (value > 0)
                                result[description] = value / (1024L * 1024L);
                        }
                    }
                    catch
                    {
                        // Ignore per-adapter registry errors.
                    }
                }
            }
            catch
            {
                return result;
            }

            return result;
        }

        private static SystemStorageType DetectFromPhysicalDisk(object? diskIndex)
        {
            if (diskIndex == null)
                return SystemStorageType.Unknown;

            try
            {
                using var searcher = new ManagementObjectSearcher(
                    @"root\Microsoft\Windows\Storage",
                    $"SELECT MediaType, BusType FROM MSFT_PhysicalDisk WHERE DeviceId='{diskIndex}'");

                foreach (var obj in searcher.Get())
                {
                    var busType = Convert.ToInt32(obj["BusType"] ?? 0);
                    var mediaType = Convert.ToInt32(obj["MediaType"] ?? 0);

                    if (busType == 17)
                        return SystemStorageType.NVMe;

                    if (mediaType == 4)
                        return SystemStorageType.SSD;

                    if (mediaType == 3)
                        return SystemStorageType.HDD;
                }
            }
            catch
            {
                return SystemStorageType.Unknown;
            }

            return SystemStorageType.Unknown;
        }

        #endregion

        #region PInvoke

        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        #endregion
    }
}
