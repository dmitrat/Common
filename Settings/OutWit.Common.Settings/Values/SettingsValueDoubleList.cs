using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using OutWit.Common.Abstract;
using OutWit.Common.Collections;
using OutWit.Common.Interfaces;
using OutWit.Common.Settings.Elements;
using OutWit.Common.Utils;

namespace OutWit.Common.Settings.Values
{
	public class SettingsValueDoubleList : SettingsValueBase<IReadOnlyList<double>>
	{
        #region Constants

        private const char SEPARATOR = ',';

        #endregion

        public SettingsValueDoubleList(SettingsElementBase defaultElement, SettingsElementBase userElement) : 
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

        private static IReadOnlyList<double> Split(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return new double[] { };

            try
            {
                if (!value.Contains(SEPARATOR))
                    return new[] { double.Parse(value, CultureInfo.InvariantCulture) };

                return value.Split(SEPARATOR).Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x=>double.Parse(x, CultureInfo.InvariantCulture)).ToArray();

            }
            catch (Exception e)
            {
                return new double[] { };
            }
        }

        private static string Combine(IReadOnlyList<double> values)
        {
            if (values == null || values.Count == 0)
                return "";

            var str = "";
            foreach (var value in values)
                str += $"{value.ToString(CultureInfo.InvariantCulture)}{SEPARATOR} ";

            return str.TrimEnd(2);
        }

        public override ModelBase Clone()
        {
            return new SettingsValueString(DefaultElement, UserElement);
        }
	}
}
