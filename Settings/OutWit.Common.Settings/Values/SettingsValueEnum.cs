using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OutWit.Common.Abstract;
using OutWit.Common.Interfaces;
using OutWit.Common.Settings.Elements;
using OutWit.Common.Values;

namespace OutWit.Common.Settings.Values
{
	public class SettingsValueEnum : SettingsValueBase<Enum>
	{
        public SettingsValueEnum(SettingsElementBase defaultElement, SettingsElementBase userElement) : 
			base(defaultElement, userElement)
		{
		}

		public override void Reset(IResources resources)
        {
            DefaultValue = ParseEnum(DefaultTag, DefaultElement.Value);

            if (UserElement != null)
                UserValue = ParseEnum(UserTag, UserElement.Value);

            if (resources != null)
                Name = resources[Key];
        }

        private Enum ParseEnum(string tag, string value)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return null;

            var enumType = Type.GetType(tag);
            if (enumType == null)
                return null;

            if (Enum.TryParse(enumType, value, out var enumValue))
                return enumValue as Enum;

            return null;
        }

        public override void Update(IResources resources)
        {
            DefaultElement.Value = $"{DefaultValue}";

            if (UserElement != null)
                UserElement.Value = $"{UserValue}";
        }

        protected override void CheckValues()
        {
            IsDefault = DefaultValue.Is(UserValue);
        }

        public override ModelBase Clone()
        {
            return new SettingsValueEnum(DefaultElement, UserElement);
        }
        
    }
}
