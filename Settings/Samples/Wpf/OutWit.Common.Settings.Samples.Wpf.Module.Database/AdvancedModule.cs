using OutWit.Common.Settings.Configuration;
using OutWit.Common.Settings.Database;
using OutWit.Common.Settings.Interfaces;
using OutWit.Database.EntityFramework.Extensions;
using OutWit.Common.Settings.Samples.Serializers;

namespace OutWit.Common.Settings.Samples.Wpf.Module.Database
{
    /// <summary>
    /// Advanced settings module backed by WitDatabase.
    /// Group: AdvancedSettings (Advanced).
    /// </summary>
    public sealed class AdvancedModule : IAdvancedModule
    {
        #region Functions

        /// <inheritdoc />
        public void Initialize()
        {
            DatabaseSeeder.EnsureDefaults(SettingsPathResolver.GetDefaultsPath(".db"));

            Manager = new SettingsBuilder()
                .AddCustomSerializers()
                .UseDatabase(path => o => o.UseWitDb($"Data Source={path}"))
                .RegisterContainer<AdvancedSettings>()
                .Build();

            Manager.Merge();
            Manager.Load();
        }

        #endregion

        #region Properties

        /// <inheritdoc />
        public ISettingsManager Manager { get; private set; } = null!;

        #endregion
    }
}
