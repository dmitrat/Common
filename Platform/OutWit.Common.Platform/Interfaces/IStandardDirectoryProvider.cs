namespace OutWit.Common.Platform.Interfaces
{
    /// <summary>
    /// Resolves semantic standard directories for a specific application or product scope.
    /// </summary>
    public interface IStandardDirectoryProvider
    {
        /// <summary>
        /// Returns the per-user application data directory.
        /// </summary>
        string GetUserDataDirectory(params string[] segments);

        /// <summary>
        /// Returns the machine-wide shared application data directory.
        /// </summary>
        string GetSharedDataDirectory(params string[] segments);

        /// <summary>
        /// Returns the per-user cache directory.
        /// </summary>
        string GetCacheDirectory(params string[] segments);

        /// <summary>
        /// Returns the per-user logs directory.
        /// </summary>
        string GetLogsDirectory(params string[] segments);

        /// <summary>
        /// Returns the per-user configuration directory.
        /// </summary>
        string GetConfigDirectory(params string[] segments);

        /// <summary>
        /// Returns the temporary working directory.
        /// </summary>
        string GetTempDirectory(params string[] segments);
    }
}
