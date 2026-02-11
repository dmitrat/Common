using System;
using System.Globalization;

namespace OutWit.Common.Settings.Serialization
{
    public sealed class SettingsSerializerDateTime : SettingsSerializerBase<DateTime>
    {
        #region Functions

        public override DateTime Parse(string value, string tag)
        {
            return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }

        public override string Format(DateTime value)
        {
            return value.ToString("o", CultureInfo.InvariantCulture);
        }

        #endregion

        #region Properties

        public override string ValueKind => "DateTime";

        #endregion
    }
}
