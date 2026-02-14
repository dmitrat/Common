using OutWit.Common.Settings.Aspects;
using OutWit.Common.Settings.Configuration;
using OutWit.Common.Settings.Interfaces;

namespace OutWit.Common.Settings.Tests.Utils
{
    /// <summary>
    /// Test container that uses implicit group (derived from class name).
    /// </summary>
    public class ImplicitGroupSettings : SettingsContainer
    {
        #region Constructors

        public ImplicitGroupSettings(ISettingsManager manager)
            : base(manager)
        {
        }

        #endregion

        #region Properties

        [Setting]
        public virtual string UserName { get; set; } = null!;

        [Setting(SettingsScope.Global)]
        public virtual string GlobalValue { get; set; } = null!;

        [Setting(SettingsScope.Default)]
        public virtual string DefaultValue { get; set; } = null!;

        #endregion
    }
}
