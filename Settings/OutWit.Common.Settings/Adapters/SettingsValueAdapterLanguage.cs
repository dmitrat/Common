using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using OutWit.Common.Configuration;
using OutWit.Common.Settings.Configuration;
using OutWit.Common.Settings.Elements;
using OutWit.Common.Settings.Sections;
using OutWit.Common.Settings.Values;

namespace OutWit.Common.Settings.Adapters
{
	public class SettingsValueAdapterLanguage : SettingsValueAdapterBase<string, SettingsValueLanguage>
	{
		protected override SettingsValueLanguage GetValue(ConfigurationManager configurationManager, SettingsElementBase defaultElement, SettingsElementBase userElement)
		{
            return new SettingsValueLanguage(defaultElement, userElement);

		}

		public override string Type => "Language";
	}
}
