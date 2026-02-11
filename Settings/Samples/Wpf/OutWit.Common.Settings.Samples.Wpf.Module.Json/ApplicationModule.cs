using OutWit.Common.Settings.Configuration;
using OutWit.Common.Settings.Interfaces;
using OutWit.Common.Settings.Json;
using OutWit.Common.Settings.Samples.Serializers;

namespace OutWit.Common.Settings.Samples.Wpf.Module.Json
{
    /// <summary>
    /// Application settings module backed by JSON files.
    /// Groups: AppSettings (Application) and Notifications.
    /// </summary>
    public sealed class ApplicationModule : IApplicationModule
    {
        #region Functions

        /// <inheritdoc />
        public void Initialize()
        {
            Manager = new SettingsBuilder()
                .AddCustomSerializers()
                .UseJson()
                .RegisterContainer<ApplicationSettings>()
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
