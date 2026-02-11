using OutWit.Common.Settings.Providers;

namespace OutWit.Common.Settings.Database
{
    /// <summary>
    /// EF Core entity for settings entries. Inherits all properties from <see cref="SettingsEntry"/>
    /// and adds the database primary key.
    /// </summary>
    public sealed class SettingsEntryEntity : SettingsEntry
    {
        #region Properties

        /// <summary>
        /// Gets or sets the database primary key.
        /// </summary>
        public int Id { get; set; }

        #endregion
    }
}
