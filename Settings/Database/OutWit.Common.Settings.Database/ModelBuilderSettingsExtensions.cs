using Microsoft.EntityFrameworkCore;

namespace OutWit.Common.Settings.Database
{
    /// <summary>
    /// Extension methods for configuring settings tables in a shared <see cref="DbContext"/>.
    /// </summary>
    public static class ModelBuilderSettingsExtensions
    {
        /// <summary>
        /// Applies the settings table configuration to the model builder.
        /// Configures both the <c>Settings</c> and <c>SettingsGroups</c> tables.
        /// Use this method when integrating settings into an existing DbContext.
        /// </summary>
        /// <param name="modelBuilder">The model builder.</param>
        /// <returns>The model builder for chaining.</returns>
        public static ModelBuilder ApplySettingsConfiguration(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SettingsEntryEntity>(entity =>
            {
                entity.ToTable("Settings");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.Group, e.Key }).IsUnique();

                entity.Property(e => e.Group).IsRequired().HasMaxLength(256);
                entity.Property(e => e.Key).IsRequired().HasMaxLength(256);
                entity.Property(e => e.Value).IsRequired().HasMaxLength(4096);
                entity.Property(e => e.ValueKind).IsRequired().HasMaxLength(64);
                entity.Property(e => e.Tag).IsRequired().HasMaxLength(512);
            });

            modelBuilder.Entity<SettingsGroupEntity>(entity =>
            {
                entity.ToTable("SettingsGroups");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Group).IsUnique();

                entity.Property(e => e.Group).IsRequired().HasMaxLength(256);
                entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(256);
            });

            return modelBuilder;
        }
    }
}
