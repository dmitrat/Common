using System.Diagnostics;

namespace OutWit.Common.Configuration.Tests
{
    /// <summary>
    /// Counts the handles the current process holds, so a test can assert that an
    /// operation does not pin operating-system resources (file watchers, inotify
    /// instances, directory handles) per call.
    /// </summary>
    internal static class ProcessHandles
    {
        #region Functions

        /// <summary>
        /// The number of handles (Windows) or open file descriptors (Linux) of the
        /// current process; -1 when the platform exposes neither.
        /// </summary>
        public static int Count()
        {
            if (OperatingSystem.IsWindows())
                return Process.GetCurrentProcess().HandleCount;

            if (Directory.Exists("/proc/self/fd"))
                return Directory.GetFileSystemEntries("/proc/self/fd").Length;

            return -1;
        }

        #endregion
    }
}
