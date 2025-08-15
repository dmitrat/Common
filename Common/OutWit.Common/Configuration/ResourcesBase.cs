using System.Globalization;
using System.Reflection;
using System.Resources;
using OutWit.Common.Interfaces;

namespace OutWit.Common.Configuration
{
	public class ResourcesBase<TResources> : IResources
		where TResources:class
	{
		#region Constants

		private const string DEFAULT_CULTURE = "en-US";

		#endregion

		#region Constructors

		public ResourcesBase(Assembly assembly)
		{
			Resources = new ResourceManager(typeof(TResources).FullName ?? "", assembly);
			Current = new CultureInfo(DEFAULT_CULTURE);
		} 

		#endregion

		#region Functions

		public override string ToString()
		{
			return $"{Resources}";
		}

		public void ResetCulture(string cultureName)
        {
			if(string.IsNullOrWhiteSpace(cultureName))
				return;
            Current = new CultureInfo(cultureName);
        }

        public bool HasStringFor(string key)
        {
			return Resources.GetString(key, Current) != null;
        }

        public bool HasStringFor(string key, CultureInfo culture)
        {
            return Resources.GetString(key, culture) != null;
        }

        private string GetResource(string key, CultureInfo culture)
		{
			return Resources.GetString(key, culture) ?? key;
		}

		#endregion

		#region Properties

		public string this[string key] => GetResource(key, Current);
		public string this[string key, CultureInfo culture] => GetResource(key, culture);

		private CultureInfo Current { get; set; }
		private ResourceManager Resources { get; } 

		#endregion
	}
}
