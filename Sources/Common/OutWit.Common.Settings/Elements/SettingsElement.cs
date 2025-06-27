using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Common.Settings.Elements
{
	public class SettingsElement : SettingsElementBase
	{
		#region Constants
		private const string TYPE_ATTRIBUTE = "type";

		#endregion

		#region Functions

		public override string ToString()
		{
			return $"Key={Key}, Value={Value}, Type={Type}, Tag={Tag}";
		}

		#endregion

		#region Properties

		[ConfigurationProperty(TYPE_ATTRIBUTE, DefaultValue = "String", IsKey = false, IsRequired = true)]
		public string Type
		{
			get => (string)base[TYPE_ATTRIBUTE];
			set => base[TYPE_ATTRIBUTE] = value;
		}

		#endregion
	}


}
