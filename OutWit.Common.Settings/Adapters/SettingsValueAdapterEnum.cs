using System;
using System.Diagnostics.CodeAnalysis;
using OutWit.Common.Configuration;
using OutWit.Common.Settings.Configuration;
using OutWit.Common.Settings.Elements;
using OutWit.Common.Settings.Values;

namespace OutWit.Common.Settings.Adapters
{
	public class SettingsValueAdapterEnum : SettingsValueAdapterBase<Enum, SettingsValueEnum>
	{
		protected override SettingsValueEnum GetValue(ConfigurationManager configurationManager, SettingsElementBase defaultElement, SettingsElementBase userElement)
		{
			return new SettingsValueEnum(defaultElement, userElement);
		}

        public override string Type => "Enum";
    }
}
