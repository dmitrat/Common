using OutWit.Common.Settings.Aspects;
using OutWit.Common.Settings.Configuration;
using OutWit.Common.Settings.Interfaces;

namespace OutWit.Common.Settings.Tests.Utils
{
    public class TestSettings : SettingsContainer, ITestSettings
    {
        #region Constructors

        public TestSettings(ISettingsManager manager)
            : base(manager)
        {
        }

        #endregion

        #region Properties

        [Setting("General")]
        public virtual string UserName { get; set; } = null!;

        [Setting("General")]
        public virtual bool DarkMode { get; set; }

        [Setting("General")]
        public virtual int MaxRetries { get; set; }

        [Setting("General", SettingsScope.Default)]
        public virtual string AppVersion { get; set; } = null!;

        #endregion
    }
}
