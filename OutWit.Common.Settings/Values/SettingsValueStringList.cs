using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using OutWit.Common.Abstract;
using OutWit.Common.Collections;
using OutWit.Common.Interfaces;
using OutWit.Common.Settings.Elements;
using OutWit.Common.Utils;

namespace OutWit.Common.Settings.Values
{
	public class SettingsValueStringList : SettingsValueBase<IReadOnlyList<string>>
	{
        #region Constants

        private const char SEPARATOR = ',';

        #endregion

        public SettingsValueStringList(SettingsElementBase defaultElement, SettingsElementBase userElement) : 
			base(defaultElement, userElement)
		{
		}

		public override void Reset(IResources resources)
		{
            DefaultValue = Split(DefaultElement.Value);

			if (UserElement != null)
                UserValue = Split(UserElement.Value);

            if (resources != null)
				Name = resources[Key];
		}

		public override void Update(IResources resources)
		{
            DefaultElement.Value = Combine(DefaultValue);

			if (UserElement != null)
                UserElement.Value = Combine(UserValue);
        }

        protected override void CheckValues()
        {
            IsDefault = UserValue.Is(DefaultValue);
        }

        private static IReadOnlyList<string> Split(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return new string[] { };

            if (!value.Contains(SEPARATOR))
                return new[] {value};

            try
            {
                return value.Split(SEPARATOR).Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

            }
            catch (Exception e)
            {
                return new string[] { };
            }
        }

        private static string Combine(IReadOnlyList<string> values)
        {
            if (values == null || values.Count == 0)
                return "";

            var str = "";
            foreach (var value in values.Where(x => !string.IsNullOrWhiteSpace(x)))
                str += $"{value}, ";

            return str.TrimEnd(2);
        }

        public override ModelBase Clone()
        {
            return new SettingsValueString(DefaultElement, UserElement);
        }
	}
}
