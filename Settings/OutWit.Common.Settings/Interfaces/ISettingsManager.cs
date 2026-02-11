using System.Collections.Generic;
using OutWit.Common.Settings.Collections;

namespace OutWit.Common.Settings.Interfaces
{
    public interface ISettingsManager
    {
        /// <summary>
        /// Loads settings from all registered providers.
        /// Default provider defines the schema; User/Global override values.
        /// </summary>
        void Load();

        /// <summary>
        /// Saves current user values to the writable provider.
        /// </summary>
        void Save();

        /// <summary>
        /// Merges Default schema into User storage:
        /// adds new keys, removes obsolete ones, preserves existing user values.
        /// </summary>
        void Merge();

        /// <summary>
        /// Gets a collection by group name.
        /// </summary>
        SettingsCollection this[string group] { get; }

        /// <summary>
        /// All loaded settings collections.
        /// </summary>
        IReadOnlyList<SettingsCollection> Collections { get; }
    }
}
