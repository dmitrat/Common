using OutWit.Common.Settings.Interfaces;

namespace OutWit.Common.Settings.Samples.Wpf.Module.Csv
{
    /// <summary>
    /// Network settings module that stores settings in CSV format.
    /// </summary>
    public interface INetworkModule
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
