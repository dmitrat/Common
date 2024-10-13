using System.Configuration;
using OutWit.Common.Settings.Elements;

namespace OutWit.Common.Settings.Collections
{
	[ConfigurationCollection(typeof(SettingsElement), AddItemName="Item")]
	public class SettingsElementCollection : SettingsElementCollectionBase<SettingsElement>
	{
	}
}
