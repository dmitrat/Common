using Microsoft.EntityFrameworkCore;

namespace OutWit.Common.Settings.Database
{
    /// <summary>
    /// Standalone DbContext for settings storage.
    /// Configures Settings and SettingsGroups tables via <see cref="ModelBuilderSettingsExtensions"/>.
    /// </summary>
    public class SettingsDbContext : DbContext
    {
        #region Constructors

        /// <summary>
        /// Creates a new <see cref="SettingsDbContext"/> with the specified options.
        /// </summary>
        /// <param name="options">The options for this context.</param>
        public SettingsDbContext(DbContextOptions<SettingsDbContext> options)
            : base(options)
        {
        }

        #endregion

        #region Functions

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplySettingsConfiguration();
        }

        #endregion
    }
}
