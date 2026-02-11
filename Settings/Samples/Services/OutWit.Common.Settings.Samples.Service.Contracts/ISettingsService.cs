using System.Collections.Generic;
using System.Threading.Tasks;
using OutWit.Common.Settings.Interfaces;
using OutWit.Common.Settings.Providers;

namespace OutWit.Common.Settings.Samples.Service.Contracts
{
    /// <summary>
    /// RPC contract for remote settings management.
    /// </summary>
    public interface ISettingsService
    {
        /// <summary>
        /// Gets all settings group metadata.
        /// </summary>
        Task<IReadOnlyList<SettingsGroupInfo>> GetGroupsAsync();

        /// <summary>
        /// Gets all visible settings values for a group.
        /// </summary>
        /// <param name="group">The group name.</param>
        Task<IReadOnlyList<ISettingsValue>> GetValuesAsync(string group);

        /// <summary>
        /// Updates a single setting value.
        /// </summary>
        /// <param name="group">The group name.</param>
        /// <param name="key">The setting key.</param>
        /// <param name="value">The new serialized string value.</param>
        Task UpdateValueAsync(string group, string key, string value);

        /// <summary>
        /// Resets a single setting to its default value.
        /// </summary>
        /// <param name="group">The group name.</param>
        /// <param name="key">The setting key.</param>
        Task ResetValueAsync(string group, string key);

        /// <summary>
        /// Resets all settings in a group to defaults.
        /// </summary>
        /// <param name="group">The group name.</param>
        Task ResetGroupAsync(string group);

        /// <summary>
        /// Saves all pending changes to providers.
        /// </summary>
        Task SaveAsync();
    }
}
