using System.Collections.Generic;
using OutWit.Common.Settings.Providers;

namespace OutWit.Common.Settings.Interfaces
{
    public interface ISettingsProvider
    {
        /// <summary>
        /// Reads all entries for the specified group.
        /// </summary>
        IReadOnlyList<SettingsEntry> Read(string group);

        /// <summary>
        /// Writes all entries for the specified group (full replacement).
        /// </summary>
        void Write(string group, IReadOnlyList<SettingsEntry> entries);

        /// <summary>
        /// Returns the list of available groups.
        /// </summary>
        IReadOnlyList<string> GetGroups();

        /// <summary>
        /// Removes all persisted data for this provider.
        /// For file-based providers, deletes the file.
        /// Called during Merge when no settings use this provider's scope.
        /// </summary>
        void Delete();

        /// <summary>
        /// Indicates whether this provider is read-only.
        /// </summary>
        bool IsReadOnly { get; }
    }
}
