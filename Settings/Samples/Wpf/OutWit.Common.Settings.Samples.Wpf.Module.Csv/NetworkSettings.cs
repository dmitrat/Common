using System;
using OutWit.Common.Settings.Aspects;
using OutWit.Common.Settings.Configuration;
using OutWit.Common.Settings.Interfaces;

namespace OutWit.Common.Settings.Samples.Wpf.Module.Csv
{
    /// <summary>
    /// Typed access to NetworkSettings group via aspect-injected properties.
    /// </summary>
    public class NetworkSettings : SettingsContainer
    {
        #region Constructors

        /// <summary>
        /// Creates a new instance bound to the specified settings manager.
        /// </summary>
        /// <param name="settingsManager">The manager providing setting values.</param>
        public NetworkSettings(ISettingsManager settingsManager)
            : base(settingsManager)
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the API endpoint URL.
        /// </summary>
        [Setting("NetworkSettings")]
        public virtual string ApiEndpoint { get; set; } = null!;

        /// <summary>
        /// Gets or sets the proxy URL.
        /// </summary>
        [Setting("NetworkSettings")]
        public virtual string ProxyUrl { get; set; } = null!;

        /// <summary>
        /// Gets or sets the connection timeout.
        /// </summary>
        [Setting("NetworkSettings")]
        public virtual TimeSpan ConnectionTimeout { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of retries.
        /// </summary>
        [Setting("NetworkSettings")]
        public virtual int MaxRetries { get; set; }

        #endregion
    }
}
