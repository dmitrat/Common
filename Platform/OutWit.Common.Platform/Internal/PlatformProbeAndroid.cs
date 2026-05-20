using System;
using System.Collections.Generic;
using System.IO;
using OutWit.Common.Platform.Models.SystemInfo;

namespace OutWit.Common.Platform.Internal
{
    /// <summary>
    /// Android-specific platform probe. Android is Linux-flavoured so the
    /// Linux probe is reused for the bits that work the same way
    /// (<c>/proc/cpuinfo</c>, <c>/proc/meminfo</c>, <c>/proc/stat</c>); only
    /// the OS-specific shortcuts (Hardware string from build properties,
    /// pre-known SoC class, no <c>lspci</c>) are overridden here.
    /// <para>
    /// This probe deliberately stays in pure .NET — it does NOT depend on
    /// the <c>Android.OS</c> SDK namespace, so the package can target
    /// <c>net10.0</c> (and below) without an <c>android</c> TFM. Bridging
    /// real Android APIs (Build, PowerManager, ActivityManager) is an
    /// app-level concern and can be done in the consuming client.
    /// </para>
    /// </summary>
    internal sealed class PlatformProbeAndroid : PlatformProbeLinux
    {
        #region Constants

        // System property names exposed by Bionic; some are restricted on
        // Android 10+ but readable from regular apps.
        private const string BUILD_PROP_PATH = "/system/build.prop";

        #endregion

        #region IPlatformProbe

        public override PlatformKind Kind => PlatformKind.Android;

        public override string GetCpuModelName()
        {
            // /proc/cpuinfo on Android often omits "model name" and instead
            // exposes "Hardware" (board / SoC name). Prefer Hardware if present;
            // fall back to the Linux-style model name otherwise.
            try
            {
                if (File.Exists("/proc/cpuinfo"))
                {
                    foreach (var line in File.ReadLines("/proc/cpuinfo"))
                    {
                        if (line.StartsWith("Hardware", StringComparison.OrdinalIgnoreCase)
                            || line.StartsWith("model name", StringComparison.OrdinalIgnoreCase))
                        {
                            var separator = line.IndexOf(':');
                            if (separator < 0)
                                continue;
                            var value = line.Substring(separator + 1).Trim();
                            if (!string.IsNullOrEmpty(value))
                                return value;
                        }
                    }
                }
            }
            catch
            {
                // Fall through to build.prop probe.
            }

            // build.prop often has ro.hardware / ro.board.platform with the SoC.
            var board = ReadBuildProperty("ro.board.platform")
                        ?? ReadBuildProperty("ro.hardware");
            return board ?? string.Empty;
        }

        public override IReadOnlyList<SystemGpuInfo> GetGpus()
        {
            // No reliable headless way to enumerate the GPU from a library
            // process on Android — that needs EGL/Vulkan from the app context.
            // Best we can do without app-level bridging is infer the vendor
            // from build.prop hints.
            var hint = ReadBuildProperty("ro.hardware.egl")
                       ?? ReadBuildProperty("ro.hardware.vulkan")
                       ?? ReadBuildProperty("ro.opengles.version.string");

            if (string.IsNullOrEmpty(hint))
                return Array.Empty<SystemGpuInfo>();

            return new[]
            {
                new SystemGpuInfo
                {
                    ModelName = hint!,
                    VRamMb = 0,
                    GpuType = GpuClassifier.DetectType(hint!, hint!),
                    SupportedFeatures = GpuClassifier.DetectFeatures(hint!, hint!)
                }
            };
        }

        public override SystemStorageType GetStorageType(string rootPath)
        {
            // Mobile storage is virtually always eMMC or UFS — both are
            // solid-state. Reporting SSD is a safe non-Unknown default that
            // matches scheduler expectations for non-rotational media.
            // (UFS is closer to NVMe in performance, but we don't have a
            // separate enum value for it and SSD is the closest fit.)
            return SystemStorageType.SSD;
        }

        public override bool IsUserActive()
        {
            // Reliable detection needs PowerManager.isInteractive from the
            // Android SDK. From pure .NET we can only return a conservative
            // default. App-level integrators can subclass this probe and
            // override IsUserActive after wiring PowerManager.
            return true;
        }

        public override string? GetRawMachineIdentity()
        {
            // Settings.Secure.ANDROID_ID is the canonical stable identifier on
            // Android (per-app-signer + per-user since Android 8). It is NOT
            // readable from /proc, but ro.serialno is sometimes still present
            // in build.prop on older devices.
            var serial = ReadBuildProperty("ro.serialno");
            if (!string.IsNullOrWhiteSpace(serial) && serial != "unknown")
                return serial;

            // Fall back to Linux machine-id files (rare on Android but harmless).
            return base.GetRawMachineIdentity();
        }

        #endregion

        #region Tools

        private static string? ReadBuildProperty(string key)
        {
            try
            {
                if (!File.Exists(BUILD_PROP_PATH))
                    return null;

                foreach (var line in File.ReadLines(BUILD_PROP_PATH))
                {
                    var trimmed = line.TrimStart();
                    if (trimmed.StartsWith("#", StringComparison.Ordinal))
                        continue;

                    if (!trimmed.StartsWith(key, StringComparison.Ordinal))
                        continue;

                    var eq = trimmed.IndexOf('=');
                    if (eq < 0)
                        continue;

                    if (trimmed.Substring(0, eq).Trim() != key)
                        continue;

                    var value = trimmed.Substring(eq + 1).Trim();
                    return string.IsNullOrEmpty(value) ? null : value;
                }
            }
            catch
            {
                return null;
            }

            return null;
        }

        #endregion
    }
}
