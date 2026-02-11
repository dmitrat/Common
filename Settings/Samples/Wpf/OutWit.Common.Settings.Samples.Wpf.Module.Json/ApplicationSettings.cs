using OutWit.Common.Settings.Aspects;
using OutWit.Common.Settings.Configuration;
using OutWit.Common.Settings.Interfaces;
using OutWit.Common.Settings.Samples.Serializers.Types;

namespace OutWit.Common.Settings.Samples.Wpf.Module.Json
{
    /// <summary>
    /// Typed access to AppSettings group via aspect-injected properties.
    /// </summary>
    public class ApplicationSettings : SettingsContainer
    {
        #region Constructors

        /// <summary>
        /// Creates a new instance bound to the specified settings manager.
        /// </summary>
        /// <param name="settingsManager">The manager providing setting values.</param>
        public ApplicationSettings(ISettingsManager settingsManager)
            : base(settingsManager)
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the application theme.
        /// </summary>
        [Setting("AppSettings")]
        public virtual AppTheme Theme { get; set; }

        /// <summary>
        /// Gets or sets the UI language.
        /// </summary>
        [Setting("AppSettings")]
        public virtual string Language { get; set; } = null!;

        /// <summary>
        /// Gets or sets whether the application starts minimized.
        /// </summary>
        [Setting("AppSettings")]
        public virtual bool StartMinimized { get; set; }

        /// <summary>
        /// Gets or sets whether auto-save is enabled.
        /// </summary>
        [Setting("AppSettings")]
        public virtual bool AutoSave { get; set; }

        /// <summary>
        /// Gets or sets the auto-save interval in seconds.
        /// </summary>
        [Setting("AppSettings")]
        public virtual int AutoSaveInterval { get; set; }

        /// <summary>
        /// Gets or sets the accent color.
        /// </summary>
        [Setting("AppSettings")]
        public virtual ColorRgb AccentColor { get; set; } = null!;

        #endregion
    }
}
