namespace OutWit.Common.Platform.Internal
{
    /// <summary>
    /// Selects the <see cref="IPlatformProbe"/> implementation that matches the
    /// current runtime platform. Tests can also pin an explicit
    /// <see cref="PlatformKind"/> via <see cref="ForPlatform"/>.
    /// </summary>
    internal static class PlatformProbeFactory
    {
        #region Functions

        /// <summary>
        /// Returns a fresh probe for the current runtime platform.
        /// </summary>
        public static IPlatformProbe ForCurrentPlatform()
        {
            return ForPlatform(PlatformDetector.GetCurrentPlatform());
        }

        /// <summary>
        /// Returns a fresh probe for the requested <paramref name="kind"/>.
        /// Used by tests to exercise platform-specific code paths from a host
        /// that is not actually running that OS.
        /// </summary>
        public static IPlatformProbe ForPlatform(PlatformKind kind)
        {
            return kind switch
            {
                PlatformKind.Windows => new PlatformProbeWindows(),
                PlatformKind.Linux   => new PlatformProbeLinux(),
                PlatformKind.MacOS   => new PlatformProbeMacOS(),
                PlatformKind.Android => new PlatformProbeAndroid(),
                _ => new PlatformProbeNull(),
            };
        }

        #endregion
    }
}
