using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using OutWit.Common.Aspects;
using OutWit.Common.Configuration;
using OutWit.Common.Settings.Aspects;
using OutWit.Common.Settings.Configuration;
using OutWit.Common.Settings.Tests.Properties;

namespace OutWit.Common.Settings.Tests.Utils
{
    public class TestSettings : ConfigurationManager
    {
        public TestSettings() :
            base(Assembly.GetExecutingAssembly(), true, 2, "Configuration")
        {
            General = new SettingsCollection(new ResourcesBase<Resources>(Assembly.GetExecutingAssembly()), this, "General");
        }


        [Setting(nameof(General))]
        public string ModuleFolder { get; }
       
        [Setting(nameof(General))]
        public bool UsePcSpeaker { get; }

        [Setting(nameof(General))]
        public bool ShowSplashScreen { get; }

        [Setting(nameof(General))]
        public bool HideWhileLoading { get; }

        [Setting(nameof(General))]
        public bool IsNavigationBarFixed { get; set; }

        [Setting(nameof(General))]
        public string CurrentLanguage { get; set; }

        [Setting(nameof(General))]
        public TestEnum TestEnum { get; set; }

        #region Properties

        public SettingsCollection General { get; }

        #endregion
    }
}
