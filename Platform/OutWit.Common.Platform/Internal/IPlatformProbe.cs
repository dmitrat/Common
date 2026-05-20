using System;
using System.Collections.Generic;
using OutWit.Common.Platform.Models.SystemInfo;

namespace OutWit.Common.Platform.Internal
{
    /// <summary>
    /// Per-OS strategy contract for the platform-specific probes that back the
    /// public providers. Splits the per-OS code out of the orchestrators so each
    /// OS lives in its own file and new platforms (e.g. Android, iOS) can be
    /// added by writing one class.
    /// <para>
    /// The probe is allowed to hold OS resources (e.g. Windows performance
    /// counters); for that reason it is <see cref="IDisposable"/> and the
    /// owning provider is responsible for disposing it.
    /// </para>
    /// </summary>
    internal interface IPlatformProbe : IDisposable
    {
        /// <summary>
        /// The platform this probe targets. Always matches one of the
        /// <see cref="PlatformKind"/> values.
        /// </summary>
        PlatformKind Kind { get; }

        // --------------- SystemProfile inputs ---------------

        /// <summary>
        /// Returns the CPU model name as reported by the OS, or empty if the
        /// probe cannot determine it.
        /// </summary>
        string GetCpuModelName();

        /// <summary>
        /// Returns the list of GPUs the OS exposes. Empty list is a valid
        /// answer (e.g. headless server, or platform without a native API).
        /// </summary>
        IReadOnlyList<SystemGpuInfo> GetGpus();

        /// <summary>
        /// Returns the storage type backing <paramref name="rootPath"/>, or
        /// <see cref="SystemStorageType.Unknown"/> if it cannot be determined.
        /// </summary>
        SystemStorageType GetStorageType(string rootPath);

        // --------------- SystemHealth inputs ---------------

        /// <summary>
        /// Returns current CPU load as a percentage [0..100], or 0 if not available.
        /// </summary>
        double GetCpuLoadPercent();

        /// <summary>
        /// Returns currently available RAM in megabytes.
        /// </summary>
        long GetAvailableRamMb();

        /// <summary>
        /// Returns true if the user is considered active on the device.
        /// On platforms where this cannot be reliably detected the probe
        /// returns true (the safest default: don't preempt jobs).
        /// </summary>
        bool IsUserActive();

        // --------------- MachineIdentity inputs ---------------

        /// <summary>
        /// Returns the raw OS-level machine identity string used to derive a
        /// stable hashed device ID, or <c>null</c> if the OS does not expose one.
        /// </summary>
        string? GetRawMachineIdentity();
    }
}
