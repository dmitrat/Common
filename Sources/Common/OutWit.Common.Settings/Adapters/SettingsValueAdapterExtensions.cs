using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OutWit.Common.Configuration;
using OutWit.Common.Settings.Configuration;
using OutWit.Common.Settings.Elements;
using OutWit.Common.Settings.Interfaces;
using OutWit.Common.Settings.Sections;

namespace OutWit.Common.Settings.Adapters
{
	public static class SettingsValueAdapterExtensions
    {
        public static event EventHandler Loaded = delegate { };

        static SettingsValueAdapterExtensions()
		{
			Adapters = new Dictionary<string, ISettingsValueAdapter>();

			RegisterAdapter<SettingsValueAdapterInteger>();
			RegisterAdapter<SettingsValueAdapterDouble>();
			RegisterAdapter<SettingsValueAdapterBoolean>();
			RegisterAdapter<SettingsValueAdapterString>();
			RegisterAdapter<SettingsValueAdapterLanguage>();
			RegisterAdapter<SettingsValueAdapterServiceUrl>();
			RegisterAdapter<SettingsValueAdapterUrl>();
			RegisterAdapter<SettingsValueAdapterJob>();
			RegisterAdapter<SettingsValueAdapterFolder>();
			RegisterAdapter<SettingsValueAdapterPath>();
			RegisterAdapter<SettingsValueAdapterTimeSpan>();
			RegisterAdapter<SettingsValueAdapterStringList>();
			RegisterAdapter<SettingsValueAdapterDoubleList>();
			RegisterAdapter<SettingsValueAdapterEnum>();
			//RegisterAdapter<SettingsValueAdapterSensitivity>();
			//RegisterAdapter<SettingsValueAdapterBoundedInt>();
			//RegisterAdapter<SettingsValueAdapterBoundedRangeInt>();

            Loaded(null, EventArgs.Empty);
        }

		public static void RegisterAdapter<TValueAdapter>()
			where TValueAdapter : ISettingsValueAdapter, new()
		{
			var adapter = new TValueAdapter();
			Adapters.Add(adapter.Type, adapter);
		}

		#region ISettingsValueAdapter

		public static ISettingsValue GetValue(this ConfigurationManager me, string sectionKey, SettingsElement defaultElement)
        {
            return Adapters[defaultElement.Type].GetValue(me, defaultElement, me.User?.GetElement(sectionKey, defaultElement.Key));
        }

        public static SettingsElement GetElement(this IConfiguration me, string sectionKey, string elementKey)
        {
            return (me?[sectionKey] as SettingsConfigurationSection)?.AllSettings.FindElement(elementKey);
		}

        #endregion

		private static Dictionary<string, ISettingsValueAdapter> Adapters { get; }
	}
}
