using OutWit.Common.Platform.Interfaces;

namespace OutWit.Common.Platform.Providers
{
    /// <summary>
    /// Resolves semantic standard directories for the current platform and a configured application scope.
    /// </summary>
    public sealed class StandardDirectoryProvider : IStandardDirectoryProvider
    {
        #region Fields

        private readonly string[] m_baseSegments;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a directory provider for the specified application path scope.
        /// </summary>
        /// <param name="baseSegments">Path segments that identify the consuming application.</param>
        /// <exception cref="ArgumentException">Thrown when no valid path segments are provided.</exception>
        public StandardDirectoryProvider(params string[] baseSegments)
        {
            m_baseSegments = NormalizeSegments(baseSegments);

            if (m_baseSegments.Length == 0)
                throw new ArgumentException("At least one non-empty application path segment is required.", nameof(baseSegments));
        }

        #endregion

        #region IStandardDirectoryProvider

        /// <inheritdoc />
        public string GetUserDataDirectory(params string[] segments)
        {
            return Combine(GetUserDataRootDirectory(), segments);
        }

        /// <inheritdoc />
        public string GetSharedDataDirectory(params string[] segments)
        {
            return Combine(GetSharedDataRootDirectory(), segments);
        }

        /// <inheritdoc />
        public string GetCacheDirectory(params string[] segments)
        {
            return Combine(GetCacheRootDirectory(), segments);
        }

        /// <inheritdoc />
        public string GetLogsDirectory(params string[] segments)
        {
            return Combine(GetLogsRootDirectory(), segments);
        }

        /// <inheritdoc />
        public string GetConfigDirectory(params string[] segments)
        {
            return Combine(GetConfigRootDirectory(), segments);
        }

        /// <inheritdoc />
        public string GetTempDirectory(params string[] segments)
        {
            return Combine(Path.GetTempPath(), segments);
        }

        #endregion

        #region Functions

        private string GetUserDataRootDirectory()
        {
            if (OperatingSystem.IsWindows())
                return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            if (OperatingSystem.IsLinux())
                return GetEnvironmentPathOrDefault("XDG_DATA_HOME", ".local", "share");

            if (OperatingSystem.IsMacOS())
                return Path.Combine(GetHomeDirectory(), "Library", "Application Support");

            if (OperatingSystem.IsAndroid())
                return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }

        private string GetSharedDataRootDirectory()
        {
            if (OperatingSystem.IsWindows())
                return Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

            if (OperatingSystem.IsLinux())
                return Path.Combine(Path.DirectorySeparatorChar.ToString(), "var", "lib");

            if (OperatingSystem.IsMacOS())
                return Path.Combine(Path.DirectorySeparatorChar.ToString(), "Library", "Application Support");

            if (OperatingSystem.IsAndroid())
                return Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

            return Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        }

        private string GetCacheRootDirectory()
        {
            if (OperatingSystem.IsWindows())
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Cache");

            if (OperatingSystem.IsLinux())
                return GetEnvironmentPathOrDefault("XDG_CACHE_HOME", ".cache");

            if (OperatingSystem.IsMacOS())
                return Path.Combine(GetHomeDirectory(), "Library", "Caches");

            if (OperatingSystem.IsAndroid())
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "cache");

            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Cache");
        }

        private string GetLogsRootDirectory()
        {
            if (OperatingSystem.IsWindows())
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Logs");

            if (OperatingSystem.IsLinux())
                return GetEnvironmentPathOrDefault("XDG_STATE_HOME", ".local", "state", "logs");

            if (OperatingSystem.IsMacOS())
                return Path.Combine(GetHomeDirectory(), "Library", "Logs");

            if (OperatingSystem.IsAndroid())
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "logs");

            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Logs");
        }

        private string GetConfigRootDirectory()
        {
            if (OperatingSystem.IsWindows())
                return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            if (OperatingSystem.IsLinux())
                return GetEnvironmentPathOrDefault("XDG_CONFIG_HOME", ".config");

            if (OperatingSystem.IsMacOS())
                return Path.Combine(GetHomeDirectory(), "Library", "Preferences");

            if (OperatingSystem.IsAndroid())
                return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }

        private string Combine(string rootDirectory, params string[] segments)
        {
            var normalizedSegments = NormalizeSegments(segments);
            if (normalizedSegments.Length == 0)
                return Path.Combine([rootDirectory, .. m_baseSegments]);

            return Path.Combine([rootDirectory, .. m_baseSegments, .. normalizedSegments]);
        }

        private static string[] NormalizeSegments(string[]? segments)
        {
            if (segments == null || segments.Length == 0)
                return [];

            return segments
                .Where(static s => !string.IsNullOrWhiteSpace(s))
                .Select(static s => s.Trim())
                .ToArray();
        }

        private static string GetEnvironmentPathOrDefault(string variableName, params string[] fallbackRelativeSegments)
        {
            var value = Environment.GetEnvironmentVariable(variableName);
            if (!string.IsNullOrWhiteSpace(value))
                return value;

            return Path.Combine([GetHomeDirectory(), .. fallbackRelativeSegments]);
        }

        private static string GetHomeDirectory()
        {
            var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrWhiteSpace(homeDirectory))
                return homeDirectory;

            throw new InvalidOperationException("User profile directory could not be resolved for the current platform.");
        }

        #endregion
    }
}
