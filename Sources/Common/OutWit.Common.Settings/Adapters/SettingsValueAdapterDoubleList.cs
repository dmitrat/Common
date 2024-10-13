using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OutWit.Common.Configuration;
using OutWit.Common.Settings.Configuration;
using OutWit.Common.Settings.Elements;
using OutWit.Common.Settings.Values;

namespace OutWit.Common.Settings.Adapters
{
	public class SettingsValueAdapterDoubleList : SettingsValueAdapterBase<IReadOnlyList<double>, SettingsValueDoubleList>
	{
		protected override SettingsValueDoubleList GetValue(ConfigurationManager configurationManager, SettingsElementBase defaultElement, SettingsElementBase userElement)
		{
			return new SettingsValueDoubleList(defaultElement, userElement);

		}

		public override string Type => "DoubleList";
	}
}
