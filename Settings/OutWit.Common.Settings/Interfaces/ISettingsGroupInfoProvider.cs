using System.Collections.Generic;
using OutWit.Common.Settings.Providers;

namespace OutWit.Common.Settings.Interfaces
{
    /// <summary>
    /// Optional interface for providers that support group metadata (priority, display name).
    /// Providers implement this alongside <see cref="ISettingsProvider"/>.
    /// </summary>
    public interface ISettingsGroupInfoProvider
    {
        /// <summary>
        /// Reads group metadata from the storage.
        /// Returns an empty list if no metadata is available.
        /// </summary>
        IReadOnlyList<SettingsGroupInfo> ReadGroupInfo();

        /// <summary>
        /// Writes group metadata to the storage.
        /// </summary>
        void WriteGroupInfo(IReadOnlyList<SettingsGroupInfo> groups);
    }
}
