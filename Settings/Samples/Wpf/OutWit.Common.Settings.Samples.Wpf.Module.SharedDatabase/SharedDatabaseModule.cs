using System;
using Microsoft.EntityFrameworkCore;
using OutWit.Common.Settings.Configuration;
using OutWit.Common.Settings.Database;
using OutWit.Common.Settings.Interfaces;
using OutWit.Database.EntityFramework.Extensions;

namespace OutWit.Common.Settings.Samples.Wpf.Module.SharedDatabase
{
    /// <summary>
    /// Shared-database settings module backed by WitDatabase.
    /// All scopes (Default, Global, User) reside in a single database file
    /// with separate tables. User settings are isolated by <c>Environment.UserName</c>.
    /// </summary>
    public sealed class SharedDatabaseModule : ISharedDatabaseModule
    {
        #region Constants

        private const string DB_PATH = "shared-settings.witdb";

        #endregion

        #region Functions

        /// <inheritdoc />
        public void Initialize()
        {
            Action<DbContextOptionsBuilder> configure =
                o => o.UseWitDb($"Data Source={DB_PATH}");

            SharedDatabaseSeeder.EnsureDefaults(configure);

            Manager = new SettingsBuilder()
                .UseSharedDatabase(configure, userId: Environment.UserName)
                .RegisterContainer<SharedSettings>()
                .Build();

            Manager.Merge();
            Manager.Load();
        }

        #endregion

        #region Properties

        /// <inheritdoc />
        public ISettingsManager Manager { get; private set; } = null!;

        #endregion
    }
}
