using System;
using System.Collections.Generic;
using OutWit.Common.Platform.Models.SystemInfo;

namespace OutWit.Common.Platform.Internal
{
    /// <summary>
    /// No-op platform probe for <see cref="PlatformKind.Unknown"/> hosts.
    /// Returns empty/default answers so the public providers still produce
    /// a well-formed (if unhelpful) profile / health snapshot without any
    /// OS-specific calls.
    /// </summary>
    internal sealed class PlatformProbeNull : IPlatformProbe
    {
        #region IPlatformProbe

        public PlatformKind Kind => PlatformKind.Unknown;

        public string GetCpuModelName() => string.Empty;

        public IReadOnlyList<SystemGpuInfo> GetGpus() => Array.Empty<SystemGpuInfo>();

        public SystemStorageType GetStorageType(string rootPath) => SystemStorageType.Unknown;

        public double GetCpuLoadPercent() => 0.0;

        public long GetAvailableRamMb() => 0;

        public bool IsUserActive() => true;

        public string? GetRawMachineIdentity() => null;

        #endregion

        #region IDisposable

        public void Dispose()
        {
        }

        #endregion
    }
}
