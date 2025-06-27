using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Common.Settings.Elements
{
	public class SettingsElementBase : ConfigurationElement
	{
		#region Constants

		private const string KEY_ATTRIBUTE = "key";
		private const string VALUE_ATTRIBUTE = "value";
		private const string HIDDEN_ATTRIBUTE = "hidden";
		private const string TAG_ATTRIBUTE = "tag";

        #endregion

		#region Functions

		public override string ToString()
		{
			return $"Key={Key}, Value={Value}, Hidden={Hidden}, Tag={Tag}";
		}

		#endregion

		#region Properties

		[ConfigurationProperty(KEY_ATTRIBUTE, DefaultValue = "", IsKey = true, IsRequired = true)]
		public string Key
		{
			get => (string)base[KEY_ATTRIBUTE];
			set => base[KEY_ATTRIBUTE] = value;
		}

		[ConfigurationProperty(VALUE_ATTRIBUTE, DefaultValue = "", IsKey = false, IsRequired = true)]
		public string Value
		{
			get => (string)base[VALUE_ATTRIBUTE];
			set => base[VALUE_ATTRIBUTE] = value;
		}

		[ConfigurationProperty(HIDDEN_ATTRIBUTE, DefaultValue = false, IsKey = false, IsRequired = false)]
		public bool Hidden
		{
			get => (bool)base[HIDDEN_ATTRIBUTE];
			set => base[HIDDEN_ATTRIBUTE] = value;
		}

        [ConfigurationProperty(TAG_ATTRIBUTE, DefaultValue = "", IsKey = false, IsRequired = false)]
        public string Tag
        {
            get => (string)base[TAG_ATTRIBUTE];
            set => base[TAG_ATTRIBUTE] = value;
        }
		#endregion
	}


}
