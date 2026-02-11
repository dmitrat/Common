using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using OutWit.Common.Settings.Interfaces;
using OutWit.Common.Settings.Providers;

namespace OutWit.Common.Settings.Json
{
    /// <summary>
    /// JSON file-based settings provider.
    /// Stores settings entries as arrays keyed by group name.
    /// Group metadata is stored in an optional <c>__groups__</c> object section.
    /// </summary>
    public sealed class JsonSettingsProvider : ISettingsProvider, ISettingsGroupInfoProvider
    {
        #region Constants

        private const string GROUPS_SECTION = "__groups__";

        private static readonly JsonDocumentOptions DOCUMENT_OPTIONS = new()
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        #endregion

        #region Fields

        private readonly object m_lock = new();

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new JSON settings provider.
        /// </summary>
        /// <param name="filePath">Path to the JSON file.</param>
        /// <param name="isReadOnly">When <c>true</c>, write operations are silently ignored.</param>
        public JsonSettingsProvider(string filePath, bool isReadOnly = false)
        {
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            IsReadOnly = isReadOnly;
        }

        #endregion

        #region ISettingsProvider

        /// <summary>
        /// Reads all settings entries for the specified group.
        /// </summary>
        /// <param name="group">The group name.</param>
        /// <returns>List of entries, or empty if the group does not exist.</returns>
        public IReadOnlyList<SettingsEntry> Read(string group)
        {
            lock (m_lock)
            {
                if (!File.Exists(FilePath))
                    return Array.Empty<SettingsEntry>();

                var json = File.ReadAllText(FilePath);
                if (string.IsNullOrWhiteSpace(json))
                    return Array.Empty<SettingsEntry>();

                using var document = JsonDocument.Parse(json, DOCUMENT_OPTIONS);

                if (!document.RootElement.TryGetProperty(group, out var groupElement))
                    return Array.Empty<SettingsEntry>();

                if (groupElement.ValueKind != JsonValueKind.Array)
                    return Array.Empty<SettingsEntry>();

                var entries = new List<SettingsEntry>();

                foreach (var item in groupElement.EnumerateArray())
                {
                    entries.Add(new SettingsEntry
                    {
                        Group = group,
                        Key = GetStringProperty(item, "key"),
                        Value = GetStringProperty(item, "value"),
                        ValueKind = GetStringProperty(item, "valueKind"),
                        Tag = GetStringProperty(item, "tag"),
                        Hidden = GetBoolProperty(item, "hidden")
                    });
                }

                return entries;
            }
        }

        /// <summary>
        /// Writes settings entries for the specified group.
        /// </summary>
        /// <param name="group">The group name.</param>
        /// <param name="entries">The entries to write.</param>
        public void Write(string group, IReadOnlyList<SettingsEntry> entries)
        {
            if (IsReadOnly)
                return;

            lock (m_lock)
            {
                var data = ReadAllGroups();
                data.Entries[group] = entries.ToList();

                WriteAll(data);
            }
        }

        /// <summary>
        /// Returns the names of all groups stored in the file, ordered alphabetically.
        /// </summary>
        /// <returns>Sorted list of group names.</returns>
        public IReadOnlyList<string> GetGroups()
        {
            lock (m_lock)
            {
                if (!File.Exists(FilePath))
                    return Array.Empty<string>();

                var json = File.ReadAllText(FilePath);
                if (string.IsNullOrWhiteSpace(json))
                    return Array.Empty<string>();

                using var document = JsonDocument.Parse(json, DOCUMENT_OPTIONS);

                var groups = new List<string>();
                foreach (var property in document.RootElement.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.Array)
                        groups.Add(property.Name);
                }

                groups.Sort(StringComparer.Ordinal);
                return groups;
            }
        }

        #endregion

        #region ISettingsGroupInfoProvider

        /// <summary>
        /// Reads group metadata from the <c>__groups__</c> section.
        /// Returns an empty list if the section is absent.
        /// </summary>
        public IReadOnlyList<SettingsGroupInfo> ReadGroupInfo()
        {
            lock (m_lock)
            {
                if (!File.Exists(FilePath))
                    return Array.Empty<SettingsGroupInfo>();

                var json = File.ReadAllText(FilePath);
                if (string.IsNullOrWhiteSpace(json))
                    return Array.Empty<SettingsGroupInfo>();

                using var document = JsonDocument.Parse(json, DOCUMENT_OPTIONS);

                if (!document.RootElement.TryGetProperty(GROUPS_SECTION, out var section))
                    return Array.Empty<SettingsGroupInfo>();

                if (section.ValueKind != JsonValueKind.Object)
                    return Array.Empty<SettingsGroupInfo>();

                var result = new List<SettingsGroupInfo>();

                foreach (var property in section.EnumerateObject())
                {
                    if (property.Value.ValueKind != JsonValueKind.Object)
                        continue;

                    result.Add(new SettingsGroupInfo
                    {
                        Group = property.Name,
                        DisplayName = GetStringProperty(property.Value, "displayName"),
                        Priority = GetIntProperty(property.Value, "priority")
                    });
                }

                return result;
            }
        }

        /// <summary>
        /// Writes group metadata to the <c>__groups__</c> section.
        /// </summary>
        /// <param name="groups">The group metadata to write.</param>
        public void WriteGroupInfo(IReadOnlyList<SettingsGroupInfo> groups)
        {
            if (IsReadOnly)
                return;

            lock (m_lock)
            {
                var data = ReadAllGroups();
                data.GroupInfos = groups.ToList();

                WriteAll(data);
            }
        }

        #endregion

        #region Functions

        /// <inheritdoc />
        public void Delete()
        {
            if (IsReadOnly)
                return;

            lock (m_lock)
            {
                if (File.Exists(FilePath))
                    File.Delete(FilePath);
            }
        }

        private JsonFileData ReadAllGroups()
        {
            var data = new JsonFileData();

            if (!File.Exists(FilePath))
                return data;

            var json = File.ReadAllText(FilePath);
            if (string.IsNullOrWhiteSpace(json))
                return data;

            using var document = JsonDocument.Parse(json, DOCUMENT_OPTIONS);

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (property.Name == GROUPS_SECTION)
                {
                    if (property.Value.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var groupProp in property.Value.EnumerateObject())
                        {
                            if (groupProp.Value.ValueKind != JsonValueKind.Object)
                                continue;

                            data.GroupInfos.Add(new SettingsGroupInfo
                            {
                                Group = groupProp.Name,
                                DisplayName = GetStringProperty(groupProp.Value, "displayName"),
                                Priority = GetIntProperty(groupProp.Value, "priority")
                            });
                        }
                    }

                    continue;
                }

                if (property.Value.ValueKind != JsonValueKind.Array)
                    continue;

                var entries = new List<SettingsEntry>();
                foreach (var item in property.Value.EnumerateArray())
                {
                    entries.Add(new SettingsEntry
                    {
                        Group = property.Name,
                        Key = GetStringProperty(item, "key"),
                        Value = GetStringProperty(item, "value"),
                        ValueKind = GetStringProperty(item, "valueKind"),
                        Tag = GetStringProperty(item, "tag"),
                        Hidden = GetBoolProperty(item, "hidden")
                    });
                }

                data.Entries[property.Name] = entries;
            }

            return data;
        }

        private void WriteAll(JsonFileData data)
        {
            var directory = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            using var stream = File.Create(FilePath);
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

            writer.WriteStartObject();

            if (data.GroupInfos.Count > 0)
            {
                writer.WritePropertyName(GROUPS_SECTION);
                writer.WriteStartObject();

                foreach (var info in data.GroupInfos)
                {
                    writer.WritePropertyName(info.Group);
                    writer.WriteStartObject();
                    writer.WriteString("displayName", info.DisplayName);
                    writer.WriteNumber("priority", info.Priority);
                    writer.WriteEndObject();
                }

                writer.WriteEndObject();
            }

            foreach (var pair in data.Entries)
            {
                if (pair.Value.Count == 0)
                    continue;

                writer.WritePropertyName(pair.Key);
                writer.WriteStartArray();

                foreach (var entry in pair.Value)
                {
                    writer.WriteStartObject();
                    writer.WriteString("key", entry.Key);
                    writer.WriteString("value", entry.Value);
                    writer.WriteString("valueKind", entry.ValueKind);

                    if (!string.IsNullOrEmpty(entry.Tag))
                        writer.WriteString("tag", entry.Tag);

                    if (entry.Hidden)
                        writer.WriteBoolean("hidden", true);

                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }

        private static string GetStringProperty(JsonElement element, string name)
        {
            return element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString() ?? ""
                : "";
        }

        private static bool GetBoolProperty(JsonElement element, string name)
        {
            if (!element.TryGetProperty(name, out var value))
                return false;

            return value.ValueKind == JsonValueKind.True;
        }

        private static int GetIntProperty(JsonElement element, string name)
        {
            if (!element.TryGetProperty(name, out var value))
                return 0;

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var result))
                return result;

            return 0;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the file path of the JSON settings file.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// Gets whether this provider is read-only.
        /// </summary>
        public bool IsReadOnly { get; }

        #endregion

        #region Nested Types

        private sealed class JsonFileData
        {
            public Dictionary<string, List<SettingsEntry>> Entries { get; } = new();
            public List<SettingsGroupInfo> GroupInfos { get; set; } = new();
        }

        #endregion
    }
}
