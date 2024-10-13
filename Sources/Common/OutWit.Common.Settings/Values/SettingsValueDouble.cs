using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OutWit.Common.Abstract;
using OutWit.Common.Interfaces;
using OutWit.Common.Settings.Elements;

namespace OutWit.Common.Settings.Values
{
	public class SettingsValueDouble : SettingsValueBase<double>
	{
        public SettingsValueDouble(SettingsElementBase defaultElement, SettingsElementBase userElement) : 
			base(defaultElement, userElement)
		{
		}

		public override void Reset(IResources resources)
		{
            DefaultValue = double.Parse(DefaultElement.Value, CultureInfo.InvariantCulture);

			if (UserElement != null)
                UserValue = double.Parse(UserElement.Value, CultureInfo.InvariantCulture);

            if (resources != null)
				Name = resources[Key];
		}

        public override void Update(IResources resources)
        {
            DefaultElement.Value = $"{DefaultValue.ToString(CultureInfo.InvariantCulture)}";

			if (UserElement != null)
                UserElement.Value = $"{UserValue.ToString(CultureInfo.InvariantCulture)}";
        }

        protected override void CheckValues()
        {
            IsDefault = Math.Abs(UserValue - DefaultValue) < ModelBase.DEFAULT_TOLERANCE;
        }

        public override ModelBase Clone()
        {
            return new SettingsValueDouble(DefaultElement, UserElement);
        }
    }
}
