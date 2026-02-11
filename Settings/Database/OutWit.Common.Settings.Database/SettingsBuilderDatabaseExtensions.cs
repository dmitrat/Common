using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using OutWit.Common.Settings.Configuration;

namespace OutWit.Common.Settings.Database
{
    public static class SettingsBuilderDatabaseExtensions
    {
        /// <summary>
        /// Configures database format using the conventional defaults path:
        /// <c>{AppContext.BaseDirectory}/Resources/settings.db</c>.
        /// </summary>
        /// <param name="builder">The settings builder.</param>
        /// <param name="configureFromPath">Factory: file path to DbContext configuration action.</param>
        /// <returns>The builder for chaining.</returns>
        public static SettingsBuilder UseDatabase(
            this SettingsBuilder builder,
            Func<string, Action<DbContextOptionsBuilder>> configureFromPath)
        {
            return builder.UseDatabase(SettingsPathResolver.GetDefaultsPath(".db"), configureFromPath);
        }

        /// <summary>
        /// Configures database format: registers the defaults database as read-only Default provider
        /// and sets a factory for auto-creating User/Global scope providers during Build().
        /// </summary>
        /// <param name="builder">The settings builder.</param>
        /// <param name="defaultsPath">Path to the defaults database file.</param>
        /// <param name="configureFromPath">Factory: file path to DbContext configuration action.</param>
        /// <returns>The builder for chaining.</returns>
        public static SettingsBuilder UseDatabase(
            this SettingsBuilder builder,
            string defaultsPath,
            Func<string, Action<DbContextOptionsBuilder>> configureFromPath)
        {
            builder.AddProvider(SettingsScope.Default,
                new DatabaseSettingsProvider(configureFromPath(defaultsPath), isReadOnly: true));

            builder.SetScopeProviderFactory(".db", path =>
            {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                return new DatabaseSettingsProvider(configureFromPath(path), isReadOnly: false);
            });

            return builder;
        }

        /// <summary>
        /// Adds a standalone database as a settings provider for the specified scope.
        /// The provider creates and manages its own <see cref="SettingsDbContext"/>.
        /// </summary>
        /// <param name="builder">The settings builder.</param>
        /// <param name="configure">Action to configure the DbContext (e.g. UseSqlite, UseNpgsql).</param>
        /// <param name="scope">The scope this provider serves (Default, Global, or User).</param>
        /// <returns>The builder for chaining.</returns>
        public static SettingsBuilder UseDatabase(
            this SettingsBuilder builder,
            Action<DbContextOptionsBuilder> configure,
            SettingsScope scope)
        {
            var isReadOnly = scope == SettingsScope.Default;
            var provider = new DatabaseSettingsProvider(configure, isReadOnly);

            return builder.AddProvider(scope, provider);
        }

        /// <summary>
        /// Adds a shared database context as a settings provider for the specified scope.
        /// The caller is responsible for schema management (migrations).
        /// The context must have <see cref="SettingsEntryEntity"/> configured via
        /// <see cref="ModelBuilderSettingsExtensions.ApplySettingsConfiguration"/>.
        /// </summary>
        /// <param name="builder">The settings builder.</param>
        /// <param name="contextFactory">Factory that returns a configured DbContext instance.</param>
        /// <param name="scope">The scope this provider serves (Default, Global, or User).</param>
        /// <returns>The builder for chaining.</returns>
        public static SettingsBuilder UseDatabase(
            this SettingsBuilder builder,
            Func<DbContext> contextFactory,
            SettingsScope scope)
        {
            var isReadOnly = scope == SettingsScope.Default;
            var provider = new DatabaseSettingsProvider(contextFactory, isReadOnly);

            return builder.AddProvider(scope, provider);
        }

        /// <summary>
        /// Configures a shared database for multi-scope settings storage.
        /// All scopes live in one database with separate tables:
        /// <c>Settings</c> (Default), <c>Settings_Global</c>, <c>Settings_User</c>.
        /// User scope entries are isolated by <c>UserId</c>.
        /// Tables are created automatically on first use.
        /// </summary>
        /// <param name="builder">The settings builder.</param>
        /// <param name="configure">Action to configure the DbContext (e.g. UseNpgsql, UseSqlite).</param>
        /// <param name="userId">
        /// User identifier for per-user isolation in the User scope table.
        /// Pass <c>null</c> to disable User scope support (only Default and Global).
        /// Typical values: <c>Environment.UserName</c> for desktop apps,
        /// authenticated user ID for web apps.
        /// </param>
        /// <returns>The builder for chaining.</returns>
        public static SettingsBuilder UseSharedDatabase(
            this SettingsBuilder builder,
            Action<DbContextOptionsBuilder> configure,
            string? userId = null)
        {
            builder.AddProvider(SettingsScope.Default,
                new DatabaseScopedSettingsProvider(configure, SettingsScope.Default, userId, isReadOnly: true));

            builder.SetScopeProviderFactory(scope =>
            {
                if (scope == SettingsScope.User && string.IsNullOrEmpty(userId))
                    throw new InvalidOperationException(
                        "User scope requires a userId. Provide userId in UseSharedDatabase().");

                return new DatabaseScopedSettingsProvider(configure, scope, userId, isReadOnly: false);
            });

            return builder;
        }
    }
}
