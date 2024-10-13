using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OutWit.Common.Abstract;
using OutWit.Common.Interfaces;
using OutWit.Common.Settings.Elements;
using OutWit.Common.Utils;

namespace OutWit.Common.Settings.Values
{
	public class SettingsValueFolder : SettingsValueBase<string>
	{
        #region Constants

        private string APP_DATA_FOLDER_PATTERN = "%appdata%";
        private string PROGRAM_DATA_FOLDER_PATTERN = "%PROGRAMDATA%";

        #endregion

        public SettingsValueFolder(SettingsElementBase defaultElement, SettingsElementBase userElement) : 
			base(defaultElement, userElement)
		{
		}

		public override void Reset(IResources resources)
		{
            DefaultValue = CheckFolder(DefaultElement.Value);

			if (UserElement != null)
                UserValue = CheckFolder(UserElement.Value);

            if (resources != null)
				Name = resources[Key];
		}

        public override void Update(IResources resources)
        {
            DefaultElement.Value = DefaultValue;

			if (UserElement != null)
                UserElement.Value = UserValue;
        }

        private string CheckFolder(string value)
        {
            if (value.Contains(APP_DATA_FOLDER_PATTERN, StringComparison.InvariantCultureIgnoreCase))
                value = value.Replace(APP_DATA_FOLDER_PATTERN,
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), StringComparison.InvariantCultureIgnoreCase);

            if (value.Contains(PROGRAM_DATA_FOLDER_PATTERN, StringComparison.InvariantCultureIgnoreCase))
                value = value.Replace(PROGRAM_DATA_FOLDER_PATTERN,
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), StringComparison.InvariantCultureIgnoreCase);

            value.CheckFolder();

            return value;
        }

        protected override void CheckValues()
        {
            IsDefault = UserValue == DefaultValue;
        }

        public override ModelBase Clone()
        {
            return new SettingsValueFolder(DefaultElement, UserElement);
        }
    }
}
