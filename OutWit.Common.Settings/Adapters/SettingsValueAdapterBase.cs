using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OutWit.Common.Configuration;
using OutWit.Common.Settings.Configuration;
using OutWit.Common.Settings.Elements;
using OutWit.Common.Settings.Interfaces;
using OutWit.Common.Settings.Values;

namespace OutWit.Common.Settings.Adapters
{
	public abstract class SettingsValueAdapterBase<TValue, TSettingsValue> : ISettingsValueAdapter
		where TSettingsValue : SettingsValueBase<TValue>
	{
		protected abstract TSettingsValue GetValue(ConfigurationManager configurationManager, SettingsElementBase defaultElement, SettingsElementBase userElement);

		ISettingsValue ISettingsValueAdapter.GetValue(ConfigurationManager configurationManager, SettingsElementBase defaultElement, SettingsElementBase userElement)
		{
			return GetValue(configurationManager, defaultElement, userElement);
		}

        public virtual string Type => typeof(TValue).Name;
    }
}
