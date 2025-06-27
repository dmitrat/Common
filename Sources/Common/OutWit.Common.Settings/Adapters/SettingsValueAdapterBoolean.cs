using System.Diagnostics.CodeAnalysis;
using OutWit.Common.Configuration;
using OutWit.Common.Settings.Configuration;
using OutWit.Common.Settings.Elements;
using OutWit.Common.Settings.Values;

namespace OutWit.Common.Settings.Adapters
{
	public class SettingsValueAdapterBoolean : SettingsValueAdapterBase<bool, SettingsValueBoolean>
	{
		protected override SettingsValueBoolean GetValue(ConfigurationManager configurationManager, SettingsElementBase defaultElement, SettingsElementBase userElement)
		{
			return new SettingsValueBoolean(defaultElement, userElement);
		}

		public override string Type => "Boolean";
	}
}
