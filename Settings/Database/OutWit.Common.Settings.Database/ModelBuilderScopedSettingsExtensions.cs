using Microsoft.EntityFrameworkCore;
using OutWit.Common.Settings.Configuration;

namespace OutWit.Common.Settings.Database
{
    /// <summary>
    /// Extension methods for configuring scoped settings tables in a shared <see cref="DbContext"/>.
    /// Use when integrating multi-scope settings into an existing database alongside other entities.
    /// </summary>
    public static class ModelBuilderScopedSettingsExtensions
    {
        /// <summary>
        /// Applies the scoped settings table configuration to the model builder.
        /// Configures table names based on scope and adds <c>UserId</c> column for User scope.
        /// Call this once per scope in your <see cref="DbContext.OnModelCreating"/>.
        /// </summary>
        /// <param name="modelBuilder">The model builder.</param>
        /// <param name="scope">The scope to configure tables for.</param>
        /// <param name="userId">The user identifier for User scope query filtering. Ignored for other scopes.</param>
        /// <returns>The model builder for chaining.</returns>
        public static ModelBuilder ApplyScopedSettingsConfiguration(
            this ModelBuilder modelBuilder,
            SettingsScope scope,
            string? userId = null)
        {
            var entriesTable = GetTableName("Settings", scope);
            var groupsTable = GetTableName("SettingsGroups", scope);

            modelBuilder.Entity<SettingsEntryEntity>(entity =>
            {
                entity.ToTable(entriesTable);
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.Group, e.Key }).IsUnique();

                entity.Property(e => e.Group).IsRequired().HasMaxLength(256);
                entity.Property(e => e.Key).IsRequired().HasMaxLength(256);
                entity.Property(e => e.Value).IsRequired().HasMaxLength(4096);
                entity.Property(e => e.ValueKind).IsRequired().HasMaxLength(64);
                entity.Property(e => e.Tag).IsRequired().HasMaxLength(512);

                if (scope == SettingsScope.User)
                {
                    entity.Property<string>("UserId").IsRequired().HasMaxLength(256);
                    entity.HasIndex("UserId", nameof(SettingsEntryEntity.Group), nameof(SettingsEntryEntity.Key)).IsUnique();
                    entity.HasQueryFilter(e => EF.Property<string>(e, "UserId") == userId);
                }
            });

            modelBuilder.Entity<SettingsGroupEntity>(entity =>
            {
                entity.ToTable(groupsTable);
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Group).IsUnique();

                entity.Property(e => e.Group).IsRequired().HasMaxLength(256);
                entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(256);

                if (scope == SettingsScope.User)
                {
                    entity.Property<string>("UserId").IsRequired().HasMaxLength(256);
                    entity.HasIndex("UserId", nameof(SettingsGroupEntity.Group)).IsUnique();
                    entity.HasQueryFilter(e => EF.Property<string>(e, "UserId") == userId);
                }
            });

            return modelBuilder;
        }

        #region Tools

        private static string GetTableName(string baseName, SettingsScope scope)
        {
            return scope switch
            {
                SettingsScope.Default => baseName,
                SettingsScope.Global => $"{baseName}_Global",
                SettingsScope.User => $"{baseName}_User",
                _ => baseName
            };
        }

        #endregion
    }
}
