using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using OutWit.Common.Settings.Aspects;
using OutWit.Common.Settings.Collections;
using OutWit.Common.Settings.Interfaces;
using OutWit.Common.Settings.Providers;
using OutWit.Common.Settings.Values;

namespace OutWit.Common.Settings.Configuration
{
    public sealed class SettingsManager : ISettingsManager
    {
        #region Fields

        private readonly object m_lock = new();
        private readonly Dictionary<SettingsScope, ISettingsProvider> m_providers = new();
        private readonly Dictionary<string, ISettingsSerializer> m_serializers = new();
        private readonly Dictionary<string, SettingsCollection> m_collections = new();
        private readonly List<SettingsGroupInfo> m_groupOverrides = new();
        private readonly Dictionary<(string Group, string Key), SettingsScope> m_scopeMap = new();
        private readonly ILogger? m_logger;
        private bool m_hasContainerRegistrations;

        #endregion

        #region Constructors

        internal SettingsManager(ILogger? logger = null)
        {
            m_logger = logger;
        }

        #endregion

        #region Functions

        /// <summary>
        /// Loads settings from all registered providers.
        /// When containers are registered, only entries with matching <see cref="SettingAttribute"/>
        /// are loaded. Scope is determined from the attribute, not from the storage file.
        /// </summary>
        public void Load()
        {
            lock (m_lock)
            {
                m_collections.Clear();

                if (!m_providers.TryGetValue(SettingsScope.Default, out var defaultProvider))
                    return;

                var groupInfos = ReadGroupInfoFromProvider(defaultProvider);
                var totalSettings = 0;

                foreach (var group in defaultProvider.GetGroups())
                {
                    var defaultEntries = defaultProvider.Read(group);

                    var userEntries = TryReadEntries(SettingsScope.User, group);
                    var globalEntries = TryReadEntries(SettingsScope.Global, group);

                    var info = groupInfos.ContainsKey(group) ? groupInfos[group] : null;
                    var collection = new SettingsCollection(group,
                        displayName: info?.DisplayName ?? "",
                        priority: info?.Priority ?? 0);

                    foreach (var entry in defaultEntries)
                    {
                        var scope = ResolveScope(group, entry.Key);

                        if (m_hasContainerRegistrations && scope == null)
                            continue;

                        var effectiveScope = scope ?? SettingsScope.User;

                        if (!m_serializers.TryGetValue(entry.ValueKind, out var serializer))
                            continue;

                        var defaultValue = serializer.Deserialize(entry.Value, entry.Tag);
                        var value = ResolveValue(
                            entry.Key, entry.Tag, serializer,
                            defaultValue, effectiveScope, userEntries, globalEntries);

                        var settingsValue = CreateSettingsValue(
                            serializer, entry, effectiveScope, defaultValue, value);

                        collection.Add(settingsValue);
                    }

                    if (collection.Count > 0)
                    {
                        m_collections[group] = collection;
                        totalSettings += collection.Count;
                    }
                }

                ApplyGroupOverrides();

                m_logger?.LogInformation("Settings loaded: {GroupCount} groups, {SettingCount} settings",
                    m_collections.Count, totalSettings);
            }
        }

        /// <summary>
        /// Saves settings to the appropriate scope providers.
        /// Default-scoped settings are never saved.
        /// User-scoped settings go to the User provider, Global-scoped to the Global provider.
        /// </summary>
        public void Save()
        {
            lock (m_lock)
            {
                var byScope = new Dictionary<SettingsScope, Dictionary<string, List<SettingsEntry>>>();

                foreach (var pair in m_collections)
                {
                    foreach (var value in pair.Value)
                    {
                        if (value.Scope == SettingsScope.Default)
                            continue;

                        if (!m_serializers.TryGetValue(value.ValueKind, out var serializer))
                            continue;

                        if (!byScope.TryGetValue(value.Scope, out var groups))
                        {
                            groups = new Dictionary<string, List<SettingsEntry>>();
                            byScope[value.Scope] = groups;
                        }

                        if (!groups.TryGetValue(pair.Key, out var entries))
                        {
                            entries = new List<SettingsEntry>();
                            groups[pair.Key] = entries;
                        }

                        entries.Add(new SettingsEntry
                        {
                            Group = pair.Key,
                            Key = value.Key,
                            Value = serializer.Serialize(value.Value),
                            ValueKind = value.ValueKind,
                            Tag = value.Tag,
                            Hidden = value.Hidden
                        });
                    }
                }

                foreach (var (scope, groups) in byScope)
                {
                    if (!m_providers.TryGetValue(scope, out var provider) || provider.IsReadOnly)
                        continue;

                    foreach (var (group, entries) in groups)
                        provider.Write(group, entries);

                    WriteGroupInfoToProvider(provider);

                    m_logger?.LogInformation("Settings saved to {Scope} scope: {GroupCount} groups",
                        scope, groups.Count);
                }
            }
        }

        /// <summary>
        /// Merges settings schema from the Default provider into writable providers.
        /// When containers are registered, only entries matching the target scope are merged.
        /// </summary>
        public void Merge()
        {
            lock (m_lock)
            {
                if (!m_providers.TryGetValue(SettingsScope.Default, out var defaultProvider))
                    return;

                var scopeMap = m_hasContainerRegistrations ? m_scopeMap : null;

                MergeScope(defaultProvider, SettingsScope.User, scopeMap);
                MergeScope(defaultProvider, SettingsScope.Global, scopeMap);
            }
        }

        private void MergeScope(
            ISettingsProvider defaultProvider,
            SettingsScope scope,
            Dictionary<(string Group, string Key), SettingsScope>? scopeMap)
        {
            if (!m_providers.TryGetValue(scope, out var provider) || provider.IsReadOnly)
                return;

            if (m_hasContainerRegistrations && !m_scopeMap.ContainsValue(scope))
            {
                try
                {
                    provider.Delete();
                    m_logger?.LogInformation("No settings for {Scope} scope, provider data deleted", scope);
                }
                catch (Exception ex)
                {
                    m_logger?.LogError(ex, "Failed to delete provider data for {Scope} scope", scope);
                }

                return;
            }

            SettingsMerger.Merge(defaultProvider, provider, scope, scopeMap);
            m_logger?.LogInformation("Settings merged for {Scope} scope", scope);
        }

        internal void AddProvider(SettingsScope scope, ISettingsProvider provider)
        {
            m_providers[scope] = provider;
        }

        internal void AddSerializer(ISettingsSerializer serializer)
        {
            m_serializers[serializer.ValueKind] = serializer;
        }

        internal void AddGroupOverride(SettingsGroupInfo groupInfo)
        {
            m_groupOverrides.Add(groupInfo);
        }

        /// <summary>
        /// Registers a container type and extracts scope information from its
        /// <see cref="SettingAttribute"/> properties.
        /// </summary>
        /// <param name="containerType">The type inheriting from <see cref="SettingsContainer"/>.</param>
        internal void RegisterContainerType(Type containerType)
        {
            m_hasContainerRegistrations = true;

            foreach (var property in containerType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var attribute = property.GetCustomAttribute<SettingAttribute>();
                if (attribute == null)
                    continue;

                var group = attribute.ResolveGroup(containerType);
                m_scopeMap[(group, property.Name)] = attribute.Scope;
            }
        }

        #endregion

        #region Tools

        private Dictionary<string, SettingsEntry> TryReadEntries(SettingsScope scope, string group)
        {
            if (!m_providers.TryGetValue(scope, out var provider))
                return new Dictionary<string, SettingsEntry>();

            return provider.Read(group).ToDictionary(e => e.Key);
        }

        private static Dictionary<string, SettingsGroupInfo> ReadGroupInfoFromProvider(ISettingsProvider provider)
        {
            if (provider is ISettingsGroupInfoProvider metaProvider)
            {
                var infos = metaProvider.ReadGroupInfo();
                return infos.ToDictionary(g => g.Group);
            }

            return new Dictionary<string, SettingsGroupInfo>();
        }

        private void WriteGroupInfoToProvider(ISettingsProvider provider)
        {
            if (provider is ISettingsGroupInfoProvider metaWriter)
            {
                var groupInfos = m_collections.Values.Select(c => new SettingsGroupInfo
                {
                    Group = c.Group,
                    DisplayName = c.DisplayName,
                    Priority = c.Priority
                }).ToList();

                metaWriter.WriteGroupInfo(groupInfos);
            }
        }

        private void ApplyGroupOverrides()
        {
            foreach (var cfg in m_groupOverrides)
            {
                if (!m_collections.ContainsKey(cfg.Group))
                    continue;

                var col = m_collections[cfg.Group];
                col.Priority = cfg.Priority;

                if (!string.IsNullOrEmpty(cfg.DisplayName))
                    col.DisplayName = cfg.DisplayName;
            }
        }

        private SettingsScope? ResolveScope(string group, string key)
        {
            if (m_scopeMap.TryGetValue((group, key), out var scope))
                return scope;

            return m_hasContainerRegistrations ? null : SettingsScope.User;
        }

        private static object ResolveValue(
            string key, string tag, ISettingsSerializer serializer,
            object defaultValue, SettingsScope scope,
            Dictionary<string, SettingsEntry> userEntries,
            Dictionary<string, SettingsEntry> globalEntries)
        {
            switch (scope)
            {
                case SettingsScope.Default:
                    return defaultValue;

                case SettingsScope.Global:
                    if (globalEntries.TryGetValue(key, out var globalEntry))
                        return serializer.Deserialize(globalEntry.Value, tag);
                    return defaultValue;

                case SettingsScope.User:
                default:
                    if (userEntries.TryGetValue(key, out var userEntry))
                        return serializer.Deserialize(userEntry.Value, tag);
                    if (globalEntries.TryGetValue(key, out var fallbackEntry))
                        return serializer.Deserialize(fallbackEntry.Value, tag);
                    return defaultValue;
            }
        }

        private static ISettingsValue CreateSettingsValue(
            ISettingsSerializer serializer, SettingsEntry entry,
            SettingsScope scope, object defaultValue, object value)
        {
            var type = typeof(SettingsValue<>).MakeGenericType(serializer.ValueType);

            return (ISettingsValue)Activator.CreateInstance(
                type, entry.Key, entry.ValueKind, entry.Tag, entry.Hidden,
                scope, serializer, defaultValue, value)!;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the settings collection for the specified group.
        /// </summary>
        /// <param name="group">The group name.</param>
        public SettingsCollection this[string group]
        {
            get
            {
                lock (m_lock)
                    return m_collections[group];
            }
        }

        /// <summary>
        /// Gets all settings collections ordered by priority.
        /// </summary>
        public IReadOnlyList<SettingsCollection> Collections
        {
            get
            {
                lock (m_lock)
                    return m_collections.Values
                        .OrderBy(c => c.Priority)
                        .ToList();
            }
        }

        #endregion
    }
}
