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
	public class SettingsValueTimeSpan : SettingsValueBase<TimeSpan>
	{
		public SettingsValueTimeSpan(SettingsElementBase defaultElement, SettingsElementBase userElement) : 
			base(defaultElement, userElement)
		{
		}

		public override void Reset(IResources resources)
        {
            DefaultValue = TimeSpan.TryParse(DefaultElement.Value, out var defaultValue) ? defaultValue : TimeSpan.Zero;

			if (UserElement != null)
				UserValue = TimeSpan.TryParse(UserElement.Value, out var userValue) ? userValue : TimeSpan.Zero;

            if (resources != null)
				Name = resources[Key];
        }

		public override void Update(IResources resources)
		{
            DefaultElement.Value = DefaultValue.ToString("c");

			if (UserElement != null)
                UserElement.Value = UserValue.ToString("c");
        }

        protected override void CheckValues()
        {
            IsDefault = UserValue == DefaultValue;
        }

        public override ModelBase Clone()
        {
            return new SettingsValueTimeSpan(DefaultElement, UserElement);
        }
	}
}
