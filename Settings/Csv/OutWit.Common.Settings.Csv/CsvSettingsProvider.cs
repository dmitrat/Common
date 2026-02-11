using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using OutWit.Common.Settings.Interfaces;
using OutWit.Common.Settings.Providers;

namespace OutWit.Common.Settings.Csv
{
    /// <summary>
    /// CSV file-based settings provider.
    /// Stores settings entries in a CSV file, one row per entry.
    /// Group metadata is stored in an optional companion file (<c>{name}.groups{ext}</c>).
    /// </summary>
    public sealed class CsvSettingsProvider : ISettingsProvider, ISettingsGroupInfoProvider
    {
        #region Constants

        private static readonly CsvConfiguration CSV_CONFIGURATION = new(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = CsvHelper.Configuration.TrimOptions.Trim
        };

        #endregion

        #region Fields

        private readonly object m_lock = new();

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new CSV settings provider.
        /// </summary>
        /// <param name="filePath">Path to the CSV file.</param>
        /// <param name="isReadOnly">When <c>true</c>, write operations are silently ignored.</param>
        public CsvSettingsProvider(string filePath, bool isReadOnly = false)
        {
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            IsReadOnly = isReadOnly;
            GroupFilePath = BuildGroupFilePath(filePath);
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
                var all = ReadAllEntries();
                return all.Where(e => e.Group == group).ToList();
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
                var all = ReadAllEntries();
                all.RemoveAll(e => e.Group == group);
                all.AddRange(entries);

                WriteAllEntries(all);
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
                var all = ReadAllEntries();
                return all.Select(e => e.Group).Distinct().OrderBy(g => g, StringComparer.Ordinal).ToList();
            }
        }

        #endregion

        #region ISettingsGroupInfoProvider

        /// <summary>
        /// Reads group metadata from the companion file.
        /// Returns an empty list if the companion file does not exist.
        /// </summary>
        public IReadOnlyList<SettingsGroupInfo> ReadGroupInfo()
        {
            lock (m_lock)
            {
                if (!File.Exists(GroupFilePath))
                    return Array.Empty<SettingsGroupInfo>();

                using var reader = new StreamReader(GroupFilePath);
                using var csv = new CsvReader(reader, CSV_CONFIGURATION);

                csv.Context.RegisterClassMap<CsvSettingsGroupMap>();

                return csv.GetRecords<CsvSettingsGroupRecord>()
                    .Select(r => new SettingsGroupInfo
                    {
                        Group = r.Group ?? "",
                        DisplayName = r.DisplayName ?? "",
                        Priority = r.Priority
                    })
                    .ToList();
            }
        }

        /// <summary>
        /// Writes group metadata to the companion file.
        /// </summary>
        /// <param name="groups">The group metadata to write.</param>
        public void WriteGroupInfo(IReadOnlyList<SettingsGroupInfo> groups)
        {
            if (IsReadOnly)
                return;

            lock (m_lock)
            {
                var directory = Path.GetDirectoryName(GroupFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                using var writer = new StreamWriter(GroupFilePath);
                using var csv = new CsvWriter(writer, CSV_CONFIGURATION);

                csv.Context.RegisterClassMap<CsvSettingsGroupMap>();

                csv.WriteRecords(groups.Select(g => new CsvSettingsGroupRecord
                {
                    Group = g.Group,
                    DisplayName = g.DisplayName,
                    Priority = g.Priority
                }));
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

                if (File.Exists(GroupFilePath))
                    File.Delete(GroupFilePath);
            }
        }

        private List<SettingsEntry> ReadAllEntries()
        {
            if (!File.Exists(FilePath))
                return new List<SettingsEntry>();

            using var reader = new StreamReader(FilePath);
            using var csv = new CsvReader(reader, CSV_CONFIGURATION);

            csv.Context.RegisterClassMap<CsvSettingsEntryMap>();

            return csv.GetRecords<SettingsEntry>()
                .ToList();
        }

        private void WriteAllEntries(List<SettingsEntry> entries)
        {
            var directory = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var sorted = entries.OrderBy(e => e.Group, StringComparer.Ordinal).ToList();

            using var writer = new StreamWriter(FilePath);
            using var csv = new CsvWriter(writer, CSV_CONFIGURATION);

            csv.Context.RegisterClassMap<CsvSettingsEntryMap>();

            csv.WriteRecords(sorted);
        }

        private static string BuildGroupFilePath(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath) ?? "";
            var name = Path.GetFileNameWithoutExtension(filePath);
            var ext = Path.GetExtension(filePath);
            return Path.Combine(directory, $"{name}.groups{ext}");
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the file path of the main CSV settings file.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// Gets the file path of the companion group metadata CSV file.
        /// </summary>
        public string GroupFilePath { get; }

        /// <summary>
        /// Gets whether this provider is read-only.
        /// </summary>
        public bool IsReadOnly { get; }

        #endregion
    }
}
