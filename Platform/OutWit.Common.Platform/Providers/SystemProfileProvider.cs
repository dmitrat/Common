using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using OutWit.Common.Platform.Interfaces;
using OutWit.Common.Platform.Internal;
using OutWit.Common.Platform.Models.SystemInfo;

namespace OutWit.Common.Platform.Providers
{
    /// <summary>
    /// Collects a <see cref="SystemProfile"/> for the current machine by
    /// orchestrating calls into a per-OS <c>IPlatformProbe</c>. The actual
    /// OS-specific reading lives in <c>Internal/PlatformProbe*</c>.
    /// </summary>
    public sealed class SystemProfileProvider : ISystemProfileProvider
    {
        #region Fields

        private readonly IPlatformProbe m_probe;

        #endregion

        #region Constructors

        public SystemProfileProvider()
            : this(PlatformProbeFactory.ForCurrentPlatform())
        {
        }

        internal SystemProfileProvider(IPlatformProbe probe)
        {
            m_probe = probe;
        }

        #endregion

        #region ISystemProfileProvider

        public Task<SystemProfile> CollectAsync()
        {
            return Task.Run(() => new SystemProfile
            {
                Os = CollectOs(),
                Cpu = CollectCpu(),
                Memory = CollectMemory(),
                Gpus = m_probe.GetGpus(),
                TempStorage = CollectStorage()
            });
        }

        #endregion

        #region Tools

        private SystemOsInfo CollectOs()
        {
            return new SystemOsInfo
            {
                Platform = m_probe.Kind,
                Version = System.Environment.OSVersion.Version.ToString()
            };
        }

        private SystemCpuInfo CollectCpu()
        {
            return new SystemCpuInfo
            {
                Architecture = RuntimeInformation.ProcessArchitecture,
                LogicalCoreCount = System.Environment.ProcessorCount,
                ModelName = m_probe.GetCpuModelName()
            };
        }

        private static SystemMemoryInfo CollectMemory()
        {
            var gcMemoryInfo = System.GC.GetGCMemoryInfo();

            return new SystemMemoryInfo
            {
                TotalRamMb = gcMemoryInfo.TotalAvailableMemoryBytes / (1024L * 1024L)
            };
        }

        private SystemStorageInfo CollectStorage()
        {
            var tempPath = Path.GetTempPath();
            var rootPath = Path.GetPathRoot(tempPath);
            if (string.IsNullOrWhiteSpace(rootPath))
                return new SystemStorageInfo();

            try
            {
                var driveInfo = new DriveInfo(rootPath!);
                return new SystemStorageInfo
                {
                    AvailableSpaceMb = driveInfo.AvailableFreeSpace / (1024L * 1024L),
                    StorageType = m_probe.GetStorageType(rootPath!)
                };
            }
            catch
            {
                return new SystemStorageInfo();
            }
        }

        #endregion
    }
}
