using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using OutWit.Common.Settings.Elements;

namespace OutWit.Common.Settings.Collections
{
	public class SettingsElementCollectionBase<TElement> : ConfigurationElementCollection
		where TElement : SettingsElementBase, new()

	{
		protected override ConfigurationElement CreateNewElement()
		{
			return new TElement();
		}

		protected override object GetElementKey(ConfigurationElement element)
		{
			return ((TElement)element).Key;
		}

        public TElement FindElement(string key) => (TElement) BaseGet(key);

		public TElement this[int index] => (TElement)BaseGet(index);

        public IEnumerable<string> Keys => this.Cast<TElement>().Select(x => x.Key);
		public IEnumerable<string> Values => this.Cast<TElement>().Select(x => x.Value);

	}
}
