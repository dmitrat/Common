using OutWit.Common.Platform.Internal;

namespace OutWit.Common.Platform
{
    /// <summary>
    /// Detects the current runtime platform. Android is checked before Linux because
    /// the Android runtime also reports as Linux from <c>RuntimeInformation</c>.
    /// </summary>
    public static class PlatformDetector
    {
        #region Functions

        /// <summary>
        /// Returns the platform kind for the current runtime environment.
        /// </summary>
        public static PlatformKind GetCurrentPlatform()
        {
            if (OsPlatform.IsAndroid())
                return PlatformKind.Android;

            if (OsPlatform.IsWindows())
                return PlatformKind.Windows;

            if (OsPlatform.IsLinux())
                return PlatformKind.Linux;

            if (OsPlatform.IsMacOS())
                return PlatformKind.MacOS;

            return PlatformKind.Unknown;
        }

        #endregion
    }
}
