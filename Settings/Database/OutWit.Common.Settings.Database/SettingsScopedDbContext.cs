using Microsoft.EntityFrameworkCore;
using OutWit.Common.Settings.Configuration;

namespace OutWit.Common.Settings.Database
{
    /// <summary>
    /// DbContext for shared-database settings storage.
    /// Configures table names based on <see cref="SettingsScope"/> and adds
    /// a <c>UserId</c> shadow property with global query filter for User scope.
    /// </summary>
    public class SettingsScopedDbContext : DbContext
    {
        #region Fields

        private readonly SettingsScope m_scope;
        private readonly string? m_userId;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new <see cref="SettingsScopedDbContext"/> with the specified options.
        /// </summary>
        /// <param name="options">The options for this context.</param>
        /// <param name="scope">The scope that determines table names and filtering.</param>
        /// <param name="userId">The user identifier for User scope filtering. Ignored for other scopes.</param>
        public SettingsScopedDbContext(
            DbContextOptions<SettingsScopedDbContext> options,
            SettingsScope scope,
            string? userId)
            : base(options)
        {
            m_scope = scope;
            m_userId = userId;
        }

        #endregion

        #region Functions

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var entriesTable = GetTableName("Settings");
            var groupsTable = GetTableName("SettingsGroups");

            modelBuilder.Entity<SettingsEntryEntity>(entity =>
            {
                entity.ToTable(entriesTable);
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Group).IsRequired().HasMaxLength(256);
                entity.Property(e => e.Key).IsRequired().HasMaxLength(256);
                entity.Property(e => e.Value).IsRequired().HasMaxLength(4096);
                entity.Property(e => e.ValueKind).IsRequired().HasMaxLength(64);
                entity.Property(e => e.Tag).IsRequired().HasMaxLength(512);

                if (m_scope == SettingsScope.User)
                {
                    entity.Property<string>("UserId").IsRequired().HasMaxLength(256);
                    entity.HasIndex("UserId", nameof(SettingsEntryEntity.Group), nameof(SettingsEntryEntity.Key)).IsUnique();
                    entity.HasQueryFilter(e => EF.Property<string>(e, "UserId") == m_userId);
                }
                else
                {
                    entity.HasIndex(e => new { e.Group, e.Key }).IsUnique();
                }
            });

            modelBuilder.Entity<SettingsGroupEntity>(entity =>
            {
                entity.ToTable(groupsTable);
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Group).IsRequired().HasMaxLength(256);
                entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(256);

                if (m_scope == SettingsScope.User)
                {
                    entity.Property<string>("UserId").IsRequired().HasMaxLength(256);
                    entity.HasIndex("UserId", nameof(SettingsGroupEntity.Group)).IsUnique();
                    entity.HasQueryFilter(e => EF.Property<string>(e, "UserId") == m_userId);
                }
                else
                {
                    entity.HasIndex(e => e.Group).IsUnique();
                }
            });
        }

        #endregion

        #region Tools

        private string GetTableName(string baseName)
        {
            return m_scope switch
            {
                SettingsScope.Default => baseName,
                SettingsScope.Global => $"{baseName}_Global",
                SettingsScope.User => $"{baseName}_User",
                _ => baseName
            };
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the scope key used for EF Core model caching.
        /// Different scopes produce different table configurations.
        /// </summary>
        internal string ScopeKey => m_scope.ToString();

        /// <summary>
        /// Gets the scope this context is configured for.
        /// </summary>
        internal SettingsScope Scope => m_scope;

        /// <summary>
        /// Gets the user identifier for User scope, or null for other scopes.
        /// </summary>
        internal string? UserId => m_userId;

        #endregion
    }
}
