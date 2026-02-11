using System;
using System.Collections.Generic;
using System.Linq;
using OutWit.Common.Settings.Interfaces;
using OutWit.Common.Settings.Providers;

namespace OutWit.Common.Settings.Configuration
{
    /// <summary>
    /// Merges settings schema from one provider into another.
    /// </summary>
    public static class SettingsMerger
    {
        #region Functions

        /// <summary>
        /// Merges the default provider schema into the target provider.
        /// New keys from default are added with default values.
        /// Keys in target that are not in default are removed.
        /// Groups in target that are not in default are removed.
        /// Group metadata for existing groups is preserved in target;
        /// metadata for new groups is copied from default.
        /// Existing keys retain their target (user) values.
        /// </summary>
        /// <param name="defaultProvider">The provider defining the authoritative schema.</param>
        /// <param name="targetProvider">The provider to merge into (must be writable).</param>
        /// <param name="targetScope">The scope of the target provider.</param>
        /// <param name="scopeMap">
        /// Optional scope map from container registrations.
        /// When provided, only entries whose scope matches <paramref name="targetScope"/> are included.
        /// When <c>null</c>, all entries are included (backward compatibility).
        /// </param>
        public static void Merge(
            ISettingsProvider defaultProvider,
            ISettingsProvider targetProvider,
            SettingsScope targetScope = SettingsScope.User,
            Dictionary<(string Group, string Key), SettingsScope>? scopeMap = null)
        {
            if (targetProvider.IsReadOnly)
                return;

            var defaultGroups = new HashSet<string>(defaultProvider.GetGroups());

            foreach (var group in defaultGroups)
            {
                var defaultEntries = defaultProvider.Read(group);
                var targetEntries = targetProvider.Read(group)
                    .ToDictionary(e => e.Key);

                var result = new List<SettingsEntry>();

                foreach (var entry in defaultEntries)
                {
                    if (scopeMap != null)
                    {
                        if (!scopeMap.TryGetValue((group, entry.Key), out var entryScope) ||
                            entryScope != targetScope)
                            continue;
                    }

                    if (targetEntries.TryGetValue(entry.Key, out var existing))
                    {
                        result.Add(new SettingsEntry
                        {
                            Group = group,
                            Key = entry.Key,
                            Value = existing.Value,
                            ValueKind = entry.ValueKind,
                            Tag = entry.Tag,
                            Hidden = entry.Hidden
                        });
                    }
                    else
                    {
                        result.Add((SettingsEntry)entry.Clone());
                    }
                }

                if (result.Count > 0)
                    targetProvider.Write(group, result);
                else
                    targetProvider.Write(group, Array.Empty<SettingsEntry>());
            }

            RemoveStaleGroups(targetProvider, defaultGroups);
            MergeGroupInfo(defaultProvider, targetProvider, defaultGroups);
        }

        private static void RemoveStaleGroups(ISettingsProvider targetProvider, HashSet<string> defaultGroups)
        {
            foreach (var group in targetProvider.GetGroups())
            {
                if (!defaultGroups.Contains(group))
                    targetProvider.Write(group, Array.Empty<SettingsEntry>());
            }
        }

        private static void MergeGroupInfo(
            ISettingsProvider defaultProvider,
            ISettingsProvider targetProvider,
            HashSet<string> defaultGroups)
        {
            if (defaultProvider is not ISettingsGroupInfoProvider defaultMeta ||
                targetProvider is not ISettingsGroupInfoProvider targetMeta)
                return;

            var defaultInfos = defaultMeta.ReadGroupInfo()
                .Where(g => defaultGroups.Contains(g.Group))
                .ToDictionary(g => g.Group);

            var targetInfos = targetMeta.ReadGroupInfo()
                .ToDictionary(g => g.Group);

            var result = new List<SettingsGroupInfo>();

            foreach (var group in defaultGroups)
            {
                if (targetInfos.TryGetValue(group, out var userInfo))
                    result.Add((SettingsGroupInfo)userInfo.Clone());
                else if (defaultInfos.TryGetValue(group, out var defaultInfo))
                    result.Add((SettingsGroupInfo)defaultInfo.Clone());
            }

            targetMeta.WriteGroupInfo(result);
        }

        #endregion
    }
}
