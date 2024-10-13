using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OutWit.Common.Configuration;
using OutWit.Common.Interfaces;
using OutWit.Common.Settings.Adapters;
using OutWit.Common.Settings.Configuration;
using OutWit.Common.Settings.Elements;
using OutWit.Common.Settings.Interfaces;
using OutWit.Common.Settings.Sections;

namespace OutWit.Common.Settings
{
	public class SettingsCollection : IEnumerable<ISettingsValue>
	{
		#region Fields

        private readonly Dictionary<string, ISettingsValue> m_values;

		#endregion

        #region Constructors

		public SettingsCollection(IResources resources, ConfigurationManager configurationManager, string collectionName, int priority = 0)
		{
			m_values = new Dictionary<string, ISettingsValue>();

			ConfigurationManager = configurationManager;
			Resources = resources;
			CollectionName = collectionName;
			Priority = priority;

            Rebuild();
		} 

		#endregion

		#region Initialization

		public void Rebuild()
		{
			m_values.Clear();

			var defaultSection = (SettingsConfigurationSection)ConfigurationManager.Default[CollectionName];

			foreach (var element in defaultSection.AllSettings.OfType<SettingsElement>())
			{
				m_values.Add(element.Key, ConfigurationManager.GetValue(CollectionName, element));
			}

			Reset();
		}

		#endregion

		#region Functions

        public bool ContainsKey(string key)
        {
            return m_values.ContainsKey(key);
        }

		public void Reset()
		{
			foreach (var value in m_values.Values)
			{
				value.Reset(Resources);
			}
		}

		public void Update()
		{
			foreach (var value in m_values.Values)
			{
				value.Update(Resources);
			}

			ConfigurationManager.Save();
		} 

		#endregion

		#region IEnumerable

		public IEnumerator<ISettingsValue> GetEnumerator()
		{
			return m_values.Values.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		} 

		#endregion

		#region Properties

		public ISettingsValue this[string key] => m_values[key];

        private string CollectionName { get; }
		public string Name => Resources[CollectionName];

		private IResources Resources { get; }
		private ConfigurationManager ConfigurationManager { get; } 

		public int Priority { get; }

		#endregion

	}
}
