using OutWit.Common.Settings.Interfaces;

namespace OutWit.Common.Settings.Samples.Wpf.Module.SharedDatabase
{
    /// <summary>
    /// Shared-database settings module that stores all scopes in a single database.
    /// Demonstrates <c>UseSharedDatabase</c> with per-user isolation via <c>UserId</c>.
    /// </summary>
    public interface ISharedDatabaseModule
    {
        /// <summary>
        /// Gets the settings manager with all loaded collections.
        /// </summary>
        ISettingsManager Manager { get; }

        /// <summary>
        /// Initializes the module, creating scope tables and loading settings.
        /// </summary>
        void Initialize();
    }
}
