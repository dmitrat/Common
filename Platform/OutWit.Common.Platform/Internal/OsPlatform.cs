using System;

namespace OutWit.Common.Platform.Internal
{
    /// <summary>
    /// Thin wrapper around <see cref="OperatingSystem"/> so the rest of the
    /// library uses a single uniform helper. All four checks are available
    /// on every supported target (net6.0+).
    /// </summary>
    internal static class OsPlatform
    {
        public static bool IsWindows() => OperatingSystem.IsWindows();
        public static bool IsLinux()   => OperatingSystem.IsLinux();
        public static bool IsMacOS()   => OperatingSystem.IsMacOS();
        public static bool IsAndroid() => OperatingSystem.IsAndroid();
    }
}
