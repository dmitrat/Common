using System.Collections.Generic;
using System.IO;
using OutWit.Common.Settings.Database;
using OutWit.Common.Settings.Providers;
using OutWit.Database.EntityFramework.Extensions;

namespace OutWit.Common.Settings.Samples.Wpf.Module.Database
{
    /// <summary>
    /// Seeds the defaults database with initial AdvancedSettings entries
    /// if the database does not yet exist.
    /// </summary>
    public static class DatabaseSeeder
    {
        #region Constants

        private const string GROUP = "AdvancedSettings";

        #endregion

        #region Functions

        /// <summary>
        /// Creates the defaults database and seeds it with default entries
        /// if the file does not exist.
        /// </summary>
        /// <param name="dbPath">Path to the defaults database file.</param>
        public static void EnsureDefaults(string dbPath)
        {
            if (File.Exists(dbPath))
                return;

            var directory = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var provider = new DatabaseSettingsProvider(
                o => o.UseWitDb($"Data Source={dbPath}"),
                isReadOnly: false);

            provider.Write(GROUP, GetDefaultEntries());
            provider.WriteGroupInfo(new[] { GetGroupInfo() });
        }

        private static IReadOnlyList<SettingsEntry> GetDefaultEntries()
        {
            return new List<SettingsEntry>
            {
                new SettingsEntry
                {
                    Group = GROUP,
                    Key = "LogLevel",
                    Value = "Info",
                    ValueKind = "Enum",
                    Tag = "OutWit.Common.Settings.Samples.Wpf.Module.Database.LogLevel, OutWit.Common.Settings.Samples.Wpf.Module.Database",
                    Hidden = false
                },
                new SettingsEntry
                {
                    Group = GROUP,
                    Key = "DataPath",
                    Value = "./data",
                    ValueKind = "Folder",
                    Tag = "",
                    Hidden = false
                },
                new SettingsEntry
                {
                    Group = GROUP,
                    Key = "EnableDiagnostics",
                    Value = "False",
                    ValueKind = "Boolean",
                    Tag = "",
                    Hidden = false
                },
                new SettingsEntry
                {
                    Group = GROUP,
                    Key = "DebugPassword",
                    Value = "",
                    ValueKind = "Password",
                    Tag = "",
                    Hidden = true
                },
                new SettingsEntry
                {
                    Group = GROUP,
                    Key = "SignalGain",
                    Value = "5|1|10",
                    ValueKind = "BoundedInt",
                    Tag = "",
                    Hidden = false
                }
            };
        }

        private static SettingsGroupInfo GetGroupInfo()
        {
            return new SettingsGroupInfo
            {
                Group = GROUP,
                DisplayName = "Advanced",
                Priority = 10
            };
        }

        #endregion
    }
}
