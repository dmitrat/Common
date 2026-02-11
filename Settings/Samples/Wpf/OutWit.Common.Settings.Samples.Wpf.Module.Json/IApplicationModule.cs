using OutWit.Common.Settings.Interfaces;

namespace OutWit.Common.Settings.Samples.Wpf.Module.Json
{
    /// <summary>
    /// Application settings module that stores settings in JSON format.
    /// Includes AppSettings and Notifications groups.
    /// </summary>
    public interface IApplicationModule
    {
        /// <summary>
        /// Gets the settings manager with all loaded collections.
        /// </summary>
        ISettingsManager Manager { get; }

        /// <summary>
        /// Initializes the module, resolving paths via <see cref="Common.Settings.Configuration.SettingsPathResolver"/>.
        /// </summary>
        void Initialize();
    }
}
