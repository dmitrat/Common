using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using OutWit.Common.Settings.Interfaces;
using OutWit.Common.Settings.Providers;

namespace OutWit.Common.Settings.Database
{
    /// <summary>
    /// EF Core database settings provider.
    /// Supports both standalone mode (own DbContext) and shared mode (external DbContext).
    /// Group metadata is stored in a separate <c>SettingsGroups</c> table.
    /// </summary>
    public sealed class DatabaseSettingsProvider : ISettingsProvider, ISettingsGroupInfoProvider
    {
        #region Fields

        private readonly object m_initLock = new();
        private readonly Func<DbContext> m_contextFactory;
        private bool m_initialized;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a standalone provider that manages its own <see cref="SettingsDbContext"/>.
        /// The database schema is created automatically via <c>EnsureCreated()</c>.
        /// </summary>
        /// <param name="configure">Action to configure the DbContext (e.g. UseSqlite, UseNpgsql).</param>
        /// <param name="isReadOnly">Whether writes should be suppressed.</param>
        public DatabaseSettingsProvider(Action<DbContextOptionsBuilder> configure, bool isReadOnly = false)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            m_contextFactory = () =>
            {
                var builder = new DbContextOptionsBuilder<SettingsDbContext>();
                configure(builder);
                var context = new SettingsDbContext(builder.Options);

                if (!m_initialized)
                {
                    lock (m_initLock)
                    {
                        if (!m_initialized)
                        {
                            context.Database.EnsureCreated();
                            m_initialized = true;
                        }
                    }
                }

                return context;
            };

            IsReadOnly = isReadOnly;
        }

        /// <summary>
        /// Creates a shared-context provider that uses an existing <see cref="DbContext"/>.
        /// The caller is responsible for schema management (migrations).
        /// The context must have settings entities configured via
        /// <see cref="ModelBuilderSettingsExtensions.ApplySettingsConfiguration"/>.
        /// </summary>
        /// <param name="contextFactory">Factory that returns a configured DbContext instance.</param>
        /// <param name="isReadOnly">Whether writes should be suppressed.</param>
        public DatabaseSettingsProvider(Func<DbContext> contextFactory, bool isReadOnly = false)
        {
            m_contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
            IsReadOnly = isReadOnly;
        }

        #endregion

        #region ISettingsProvider

        /// <summary>
        /// Reads all settings entries for the specified group.
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
        /// Replaces all existing entries for the group.
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
                context.Set<SettingsEntryEntity>().Add(new SettingsEntryEntity
                {
                    Group = group,
                    Key = entry.Key,
                    Value = entry.Value,
                    ValueKind = entry.ValueKind,
                    Tag = entry.Tag,
                    Hidden = entry.Hidden
                });
            }

            context.SaveChanges();
        }

        /// <summary>
        /// Returns the names of all groups stored in the database, ordered alphabetically.
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

        #endregion

        #region ISettingsGroupInfoProvider

        /// <summary>
        /// Reads group metadata from the <c>SettingsGroups</c> table.
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
        /// Writes group metadata to the <c>SettingsGroups</c> table.
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
                    context.Set<SettingsGroupEntity>().Add(new SettingsGroupEntity
                    {
                        Group = info.Group,
                        DisplayName = info.DisplayName,
                        Priority = info.Priority
                    });
                }

                context.SaveChanges();
            }
            catch (Exception)
            {
                // Table may not exist in shared context without migration
            }
        }

        #endregion

        #region Functions

        /// <inheritdoc />
        public void Delete()
        {
            if (IsReadOnly)
                return;

            using var context = m_contextFactory();

            try
            {
                context.Set<SettingsEntryEntity>().RemoveRange(context.Set<SettingsEntryEntity>());
                context.Set<SettingsGroupEntity>().RemoveRange(context.Set<SettingsGroupEntity>());
                context.SaveChanges();
            }
            catch (Exception)
            {
                // Table may not exist in shared context without migration
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
