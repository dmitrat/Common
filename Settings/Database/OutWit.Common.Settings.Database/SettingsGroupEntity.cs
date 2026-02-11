namespace OutWit.Common.Settings.Database
{
    /// <summary>
    /// EF Core entity for group metadata (priority, display name).
    /// </summary>
    public sealed class SettingsGroupEntity
    {
        #region Properties

        /// <summary>
        /// Gets or sets the database primary key.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the group name.
        /// </summary>
        public string Group { get; set; } = "";

        /// <summary>
        /// Gets or sets the display name for the group.
        /// </summary>
        public string DisplayName { get; set; } = "";

        /// <summary>
        /// Gets or sets the display priority (lower values appear first).
        /// </summary>
        public int Priority { get; set; }

        #endregion
    }
}
