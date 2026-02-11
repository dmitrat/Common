using System;
using System.Globalization;

namespace OutWit.Common.Settings.Serialization
{
    public sealed class SettingsSerializerTimeSpan : SettingsSerializerBase<TimeSpan>
    {
        #region Functions

        public override TimeSpan Parse(string value, string tag)
        {
            return TimeSpan.Parse(value, CultureInfo.InvariantCulture);
        }

        public override string Format(TimeSpan value)
        {
            return value.ToString("c");
        }

        #endregion

        #region Properties

        public override string ValueKind => "TimeSpan";

        #endregion
    }
}
