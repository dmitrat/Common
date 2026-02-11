using OutWit.Common.Settings.Aspects;
using OutWit.Common.Settings.Configuration;
using OutWit.Common.Settings.Interfaces;

namespace OutWit.Common.Settings.Samples.Wpf.Module.SharedDatabase
{
    /// <summary>
    /// Typed access to SharedSettings group via aspect-injected properties.
    /// Demonstrates mixed Global and User scoped settings in a single database.
    /// </summary>
    public class SharedSettings : SettingsContainer
    {
        #region Constructors

        /// <summary>
        /// Creates a new instance bound to the specified settings manager.
        /// </summary>
        /// <param name="settingsManager">The manager providing setting values.</param>
        public SharedSettings(ISettingsManager settingsManager)
            : base(settingsManager)
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the maximum number of concurrent connections (shared by all users).
        /// </summary>
        [Setting("SharedSettings", SettingsScope.Global)]
        public virtual int MaxConnections { get; set; }

        /// <summary>
        /// Gets or sets whether the server is in maintenance mode (shared by all users).
        /// </summary>
        [Setting("SharedSettings", SettingsScope.Global)]
        public virtual bool MaintenanceMode { get; set; }

        /// <summary>
        /// Gets or sets the preferred language for the current user.
        /// </summary>
        [Setting("SharedSettings", SettingsScope.User)]
        public virtual string Language { get; set; } = null!;

        /// <summary>
        /// Gets or sets the page size for the current user.
        /// </summary>
        [Setting("SharedSettings", SettingsScope.User)]
        public virtual int PageSize { get; set; }

        /// <summary>
        /// Gets or sets whether notifications are enabled for the current user.
        /// </summary>
        [Setting("SharedSettings", SettingsScope.User)]
        public virtual bool NotificationsEnabled { get; set; }

        #endregion
    }
}
