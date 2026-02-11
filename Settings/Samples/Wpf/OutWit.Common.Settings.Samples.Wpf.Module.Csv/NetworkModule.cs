using OutWit.Common.Settings.Configuration;
using OutWit.Common.Settings.Csv;
using OutWit.Common.Settings.Interfaces;

namespace OutWit.Common.Settings.Samples.Wpf.Module.Csv
{
    /// <summary>
    /// Network settings module backed by CSV files.
    /// Group: NetworkSettings (Network).
    /// </summary>
    public sealed class NetworkModule : INetworkModule
    {
        #region Functions

        /// <inheritdoc />
        public void Initialize()
        {
            Manager = new SettingsBuilder()
                .UseCsv()
                .RegisterContainer<NetworkSettings>()
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
