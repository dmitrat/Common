using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using OutWit.Common.Settings.Configuration;
using OutWit.Common.Settings.Interfaces;
using OutWit.Common.Settings.Providers;

namespace OutWit.Common.Settings.Database
{
    /// <summary>
    /// EF Core settings provider for shared-database scenarios.
    /// All scopes live in one database with separate tables per scope.
    /// User scope tables include a <c>UserId</c> column for per-user isolation.
    /// </summary>
    public sealed class DatabaseScopedSettingsProvider : ISettingsProvider, ISettingsGroupInfoProvider
    {
        #region Fields

        private readonly object m_initLock = new();
        private readonly Func<SettingsScopedDbContext> m_contextFactory;
        private readonly SettingsScope m_scope;
        private readonly string? m_userId;
        private bool m_initialized;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a scoped provider that manages its own <see cref="SettingsScopedDbContext"/>.
        /// Tables are created automatically via <c>EnsureCreated</c> / <c>CreateTables</c>.
        /// </summary>
        /// <param name="configure">Action to configure the DbContext (e.g. UseSqlite, UseNpgsql).</param>
        /// <param name="scope">The scope this provider serves.</param>
        /// <param name="userId">User identifier for User scope. Ignored for other scopes.</param>
        /// <param name="isReadOnly">Whether writes should be suppressed.</param>
        public DatabaseScopedSettingsProvider(
            Action<DbContextOptionsBuilder> configure,
            SettingsScope scope,
            string? userId,
            bool isReadOnly = false)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            m_scope = scope;
            m_userId = userId;

            m_contextFactory = () =>
            {
                var builder = new DbContextOptionsBuilder<SettingsScopedDbContext>();
                configure(builder);
                builder.ReplaceService<IModelCacheKeyFactory, SettingsScopedModelCacheKeyFactory>();

                var context = new SettingsScopedDbContext(builder.Options, scope, userId);

                if (!m_initialized)
                {
                    lock (m_initLock)
                    {
                        if (!m_initialized)
                        {
                            EnsureSchemaCreated(context);
                            m_initialized = true;
                        }
                    }
                }

                return context;
            };

            IsReadOnly = isReadOnly;
        }

        /// <summary>
        /// Creates a scoped provider using an external <see cref="DbContext"/> factory.
        /// The caller is responsible for schema management (migrations).
        /// The context must have scoped settings tables configured via
        /// <see cref="ModelBuilderScopedSettingsExtensions.ApplyScopedSettingsConfiguration"/>.
        /// </summary>
        /// <param name="contextFactory">Factory that returns a configured <see cref="SettingsScopedDbContext"/>.</param>
        /// <param name="scope">The scope this provider serves.</param>
        /// <param name="userId">User identifier for User scope. Ignored for other scopes.</param>
        /// <param name="isReadOnly">Whether writes should be suppressed.</param>
        public DatabaseScopedSettingsProvider(
            Func<SettingsScopedDbContext> contextFactory,
            SettingsScope scope,
            string? userId,
            bool isReadOnly = false)
        {
            m_contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
            m_scope = scope;
            m_userId = userId;
            IsReadOnly = isReadOnly;
        }

        #endregion

        #region ISettingsProvider

        /// <summary>
        /// Reads all settings entries for the specified group.
        /// For User scope, only entries matching the current <c>UserId</c> are returned.
        /// </summary>
        /// <param name="group">The group name.</param>
        /// <returns>List of entries ordered by Id.</returns>
        public IReadOnlyList<SettingsEntry> Read(string group)
        {
            using var context = m_contextFactory();

            return context.Set<SettingsEntryEntity>()
                .Where(e => e.Group == group)
                .OrderBy(e => e.Id)
                .Select(e => new SettingsEntry
                {
                    Group = e.Group,
                    Key = e.Key,
                    Value = e.Value,
                    ValueKind = e.ValueKind,
                    Tag = e.Tag,
                    Hidden = e.Hidden
                })
                .ToList();
        }

        /// <summary>
        /// Writes settings entries for the specified group.
        /// Replaces all existing entries for the group (and current user, for User scope).
        /// </summary>
        /// <param name="group">The group name.</param>
        /// <param name="entries">The entries to write.</param>
        public void Write(string group, IReadOnlyList<SettingsEntry> entries)
        {
            if (IsReadOnly)
                return;

            using var context = m_contextFactory();

            var existing = context.Set<SettingsEntryEntity>().Where(e => e.Group == group);
            context.Set<SettingsEntryEntity>().RemoveRange(existing);

            foreach (var entry in entries)
            {
                var entity = new SettingsEntryEntity
                {
                    Group = group,
                    Key = entry.Key,
                    Value = entry.Value,
                    ValueKind = entry.ValueKind,
                    Tag = entry.Tag,
                    Hidden = entry.Hidden
                };

                context.Set<SettingsEntryEntity>().Add(entity);

                if (m_scope == SettingsScope.User)
                    context.Entry(entity).Property("UserId").CurrentValue = m_userId;
            }

            context.SaveChanges();
        }

        /// <summary>
        /// Returns the names of all groups stored for this scope (and current user, for User scope).
        /// </summary>
        /// <returns>Sorted list of group names.</returns>
        public IReadOnlyList<string> GetGroups()
        {
            using var context = m_contextFactory();

            return context.Set<SettingsEntryEntity>()
                .Select(e => e.Group)
                .Distinct()
                .OrderBy(g => g)
                .ToList();
        }

        /// <inheritdoc />
        public void Delete()
        {
            if (IsReadOnly)
                return;

            using var context = m_contextFactory();

            try
            {
                var entries = context.Set<SettingsEntryEntity>().ToList();
                context.Set<SettingsEntryEntity>().RemoveRange(entries);

                var groups = context.Set<SettingsGroupEntity>().ToList();
                context.Set<SettingsGroupEntity>().RemoveRange(groups);

                context.SaveChanges();
            }
            catch (Exception)
            {
                // Table may not exist yet
            }
        }

        #endregion

        #region ISettingsGroupInfoProvider

        /// <summary>
        /// Reads group metadata for this scope (and current user, for User scope).
        /// Returns an empty list if the table is empty or unavailable.
        /// </summary>
        public IReadOnlyList<SettingsGroupInfo> ReadGroupInfo()
        {
            using var context = m_contextFactory();

            try
            {
                return context.Set<SettingsGroupEntity>()
                    .Select(e => new SettingsGroupInfo
                    {
                        Group = e.Group,
                        DisplayName = e.DisplayName,
                        Priority = e.Priority
                    })
                    .ToList();
            }
            catch (Exception)
            {
                // Table may not exist in shared context without migration
                return Array.Empty<SettingsGroupInfo>();
            }
        }

        /// <summary>
        /// Writes group metadata for this scope (and current user, for User scope).
        /// Replaces all existing group metadata.
        /// </summary>
        /// <param name="groups">The group metadata to write.</param>
        public void WriteGroupInfo(IReadOnlyList<SettingsGroupInfo> groups)
        {
            if (IsReadOnly)
                return;

            using var context = m_contextFactory();

            try
            {
                var existing = context.Set<SettingsGroupEntity>().ToList();
                context.Set<SettingsGroupEntity>().RemoveRange(existing);

                foreach (var info in groups)
                {
                    var entity = new SettingsGroupEntity
                    {
                        Group = info.Group,
                        DisplayName = info.DisplayName,
                        Priority = info.Priority
                    };

                    context.Set<SettingsGroupEntity>().Add(entity);

                    if (m_scope == SettingsScope.User)
                        context.Entry(entity).Property("UserId").CurrentValue = m_userId;
                }

                context.SaveChanges();
            }
            catch (Exception)
            {
                // Table may not exist in shared context without migration
            }
        }

        #endregion

        #region Tools

        private static void EnsureSchemaCreated(SettingsScopedDbContext context)
        {
            if (context.Database.EnsureCreated())
                return;

            // Database already existed. Our scope tables might not exist yet.
            try
            {
                var sp = ((IInfrastructure<IServiceProvider>)context.Database).Instance;
                var creator = sp.GetService(typeof(IRelationalDatabaseCreator)) as IRelationalDatabaseCreator;
                creator?.CreateTables();
            }
            catch (Exception)
            {
                // Tables likely already exist — safe to ignore
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether this provider is read-only.
        /// </summary>
        public bool IsReadOnly { get; }

        #endregion
    }
}
