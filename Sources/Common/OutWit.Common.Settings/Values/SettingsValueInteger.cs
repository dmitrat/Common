using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OutWit.Common.Abstract;
using OutWit.Common.Interfaces;
using OutWit.Common.Settings.Elements;

namespace OutWit.Common.Settings.Values
{
	public class SettingsValueInteger : SettingsValueBase<int>
	{
		public SettingsValueInteger(SettingsElementBase defaultElement, SettingsElementBase userElement) : 
			base(defaultElement, userElement)
		{
		}

		public override void Reset(IResources resources)
		{
            DefaultValue = int.Parse(DefaultElement.Value);

			if (UserElement != null)
                UserValue = int.Parse(UserElement.Value);

            if (resources != null)
				Name = resources[Key];
		}

        public override void Update(IResources resources)
        {
            DefaultElement.Value = $"{DefaultValue}";

			if (UserElement != null)
                UserElement.Value = $"{UserValue}";
        }

        protected override void CheckValues()
        {
            IsDefault = UserValue == DefaultValue;
        }

        public override ModelBase Clone()
        {
            return new SettingsValueInteger(DefaultElement, UserElement);
        }
    }
}
