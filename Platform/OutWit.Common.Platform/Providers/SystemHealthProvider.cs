using System;
using System.Threading.Tasks;
using OutWit.Common.Platform.Interfaces;
using OutWit.Common.Platform.Internal;
using OutWit.Common.Platform.Models.SystemHealth;

namespace OutWit.Common.Platform.Providers
{
    /// <summary>
    /// Collects a <see cref="SystemHealthSnapshot"/> for the current machine
    /// by delegating to a per-OS <c>IPlatformProbe</c>. The provider owns the
    /// probe and disposes it on disposal, since some OS probes (Windows
    /// PerformanceCounter) hold native resources.
    /// </summary>
    public sealed class SystemHealthProvider : ISystemHealthProvider, IDisposable
    {
        #region Fields

        private readonly IPlatformProbe m_probe;
        private bool m_disposed;

        #endregion

        #region Constructors

        public SystemHealthProvider()
            : this(PlatformProbeFactory.ForCurrentPlatform())
        {
        }

        internal SystemHealthProvider(IPlatformProbe probe)
        {
            m_probe = probe;
        }

        #endregion

        #region ISystemHealthProvider

        public Task<SystemHealthSnapshot> CollectAsync()
        {
            return Task.Run(() => new SystemHealthSnapshot
            {
                TimestampUtc = DateTime.UtcNow,
                CpuLoadPercent = m_probe.GetCpuLoadPercent(),
                AvailableRamMb = m_probe.GetAvailableRamMb(),
                GpuLoadPercent = null,
                IsUserActive = m_probe.IsUserActive()
            });
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (m_disposed)
                return;

            m_probe.Dispose();
            m_disposed = true;
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
