using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using OutWit.Common.Settings.Configuration;
using OutWit.Common.Settings.Database;
using OutWit.Common.Settings.Providers;

namespace OutWit.Common.Settings.Samples.Wpf.Module.SharedDatabase
{
    /// <summary>
    /// Seeds the Default scope table in a shared database with initial SharedSettings entries.
    /// Uses <see cref="DatabaseScopedSettingsProvider"/> with <see cref="SettingsScope.Default"/>.
    /// </summary>
    public static class SharedDatabaseSeeder
    {
        #region Constants

        private const string GROUP = "SharedSettings";

        #endregion

        #region Functions

        /// <summary>
        /// Seeds the Default scope table with default entries if it is empty.
        /// </summary>
        /// <param name="configure">Action to configure the DbContext (same as passed to UseSharedDatabase).</param>
        public static void EnsureDefaults(Action<DbContextOptionsBuilder> configure)
        {
            var provider = new DatabaseScopedSettingsProvider(
                configure, SettingsScope.Default, userId: null, isReadOnly: false);

            if (provider.GetGroups().Count > 0)
                return;

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
                    Key = "MaxConnections",
                    Value = "100",
                    ValueKind = "Integer",
                    Tag = "",
                    Hidden = false
                },
                new SettingsEntry
                {
                    Group = GROUP,
                    Key = "MaintenanceMode",
                    Value = "False",
                    ValueKind = "Boolean",
                    Tag = "",
                    Hidden = false
                },
                new SettingsEntry
                {
                    Group = GROUP,
                    Key = "Language",
                    Value = "en",
                    ValueKind = "String",
                    Tag = "",
                    Hidden = false
                },
                new SettingsEntry
                {
                    Group = GROUP,
                    Key = "PageSize",
                    Value = "25",
                    ValueKind = "Integer",
                    Tag = "",
                    Hidden = false
                },
                new SettingsEntry
                {
                    Group = GROUP,
                    Key = "NotificationsEnabled",
                    Value = "True",
                    ValueKind = "Boolean",
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
                DisplayName = "Shared",
                Priority = 20
            };
        }

        #endregion
    }
}
