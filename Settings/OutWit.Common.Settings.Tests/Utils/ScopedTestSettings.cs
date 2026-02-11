using OutWit.Common.Settings.Aspects;
using OutWit.Common.Settings.Configuration;
using OutWit.Common.Settings.Interfaces;

namespace OutWit.Common.Settings.Tests.Utils
{
    /// <summary>
    /// Test container with all three scope types for scope-related tests.
    /// </summary>
    public class ScopedTestSettings : SettingsContainer
    {
        #region Constructors

        public ScopedTestSettings(ISettingsManager manager)
            : base(manager)
        {
        }

        #endregion

        #region Properties

        [Setting("General")]
        public virtual string UserSetting { get; set; } = null!;

        [Setting("General", SettingsScope.Global)]
        public virtual string GlobalSetting { get; set; } = null!;

        [Setting("General", SettingsScope.Default)]
        public virtual string DefaultSetting { get; set; } = null!;

        #endregion
    }
}
