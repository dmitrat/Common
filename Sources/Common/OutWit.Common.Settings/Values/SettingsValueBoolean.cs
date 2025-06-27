using System.Diagnostics.CodeAnalysis;
using OutWit.Common.Abstract;
using OutWit.Common.Interfaces;
using OutWit.Common.Settings.Elements;

namespace OutWit.Common.Settings.Values
{
	public class SettingsValueBoolean : SettingsValueBase<bool>
	{
		public SettingsValueBoolean(SettingsElementBase defaultElement, SettingsElementBase userElement) : 
			base(defaultElement, userElement)
		{
		}

		public override void Reset(IResources resources)
		{
            DefaultValue = bool.Parse(DefaultElement.Value);

			if (UserElement != null)
			    UserValue = bool.Parse(UserElement.Value);

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
            return new SettingsValueBoolean(DefaultElement, UserElement);
        }
    }
}
