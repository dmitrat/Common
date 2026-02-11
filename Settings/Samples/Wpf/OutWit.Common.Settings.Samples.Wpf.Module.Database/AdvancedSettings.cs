using OutWit.Common.Settings.Aspects;
using OutWit.Common.Settings.Configuration;
using OutWit.Common.Settings.Interfaces;
using OutWit.Common.Settings.Samples.Serializers.Types;

namespace OutWit.Common.Settings.Samples.Wpf.Module.Database
{
    /// <summary>
    /// Typed access to AdvancedSettings group via aspect-injected properties.
    /// </summary>
    public class AdvancedSettings : SettingsContainer
    {
        #region Constructors

        /// <summary>
        /// Creates a new instance bound to the specified settings manager.
        /// </summary>
        /// <param name="settingsManager">The manager providing setting values.</param>
        public AdvancedSettings(ISettingsManager settingsManager)
            : base(settingsManager)
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the diagnostic log level.
        /// </summary>
        [Setting("AdvancedSettings")]
        public virtual LogLevel LogLevel { get; set; }

        /// <summary>
        /// Gets or sets the data storage folder path.
        /// </summary>
        [Setting("AdvancedSettings")]
        public virtual string DataPath { get; set; } = null!;

        /// <summary>
        /// Gets or sets whether diagnostics are enabled.
        /// </summary>
        [Setting("AdvancedSettings")]
        public virtual bool EnableDiagnostics { get; set; }

        /// <summary>
        /// Gets or sets the debug password (hidden from UI).
        /// </summary>
        [Setting("AdvancedSettings")]
        public virtual string DebugPassword { get; set; } = null!;

        /// <summary>
        /// Gets or sets the signal gain bounded integer.
        /// </summary>
        [Setting("AdvancedSettings")]
        public virtual BoundedInt SignalGain { get; set; } = null!;

        #endregion
    }
}
