using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OutWit.Common.Configuration;
using OutWit.Common.Settings.Configuration;
using OutWit.Common.Settings.Elements;

namespace OutWit.Common.Settings.Interfaces
{
	public interface ISettingsValueAdapter
	{
		ISettingsValue GetValue(ConfigurationManager configurationManager, SettingsElementBase defaultElement, SettingsElementBase userElement);

		string Type { get; }
	}
}
