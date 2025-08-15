using System.Collections.Generic;
using System.Configuration;
using System.Xml;
using OutWit.Common.Settings.Collections;
using OutWit.Common.Settings.Elements;

namespace OutWit.Common.Settings.Sections
{
	public class SettingsConfigurationSection : ConfigurationSection
	{
		private const string SETTINGS_SECTION = "Settings";


		[ConfigurationProperty(SETTINGS_SECTION)]
		public SettingsElementCollection AllSettings => (SettingsElementCollection)base[SETTINGS_SECTION];


	}
}
