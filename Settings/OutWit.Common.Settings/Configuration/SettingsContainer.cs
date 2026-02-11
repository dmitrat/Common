using System;
using OutWit.Common.Settings.Interfaces;

namespace OutWit.Common.Settings.Configuration
{
    /// <summary>
    /// Base class for typed settings containers.
    /// Properties marked with <see cref="Aspects.SettingAttribute"/> are intercepted
    /// by <see cref="Aspects.SettingAspect"/> to read/write settings values.
    /// </summary>
    public abstract class SettingsContainer
    {
        #region Constructors

        /// <summary>
        /// Creates a settings container bound to the specified settings manager.
        /// </summary>
        /// <param name="settingsManager">The manager providing settings values.</param>
        protected SettingsContainer(ISettingsManager settingsManager)
        {
            SettingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
        }

        #endregion

        #region Properties

        /// <summary>
        /// The settings manager that provides typed access to settings.
        /// </summary>
        public ISettingsManager SettingsManager { get; }

        #endregion
    }
}
