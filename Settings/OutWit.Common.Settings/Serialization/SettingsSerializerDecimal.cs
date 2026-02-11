using System.Globalization;

namespace OutWit.Common.Settings.Serialization
{
    public sealed class SettingsSerializerDecimal : SettingsSerializerBase<decimal>
    {
        #region Functions

        public override decimal Parse(string value, string tag)
        {
            return decimal.Parse(value, CultureInfo.InvariantCulture);
        }

        public override string Format(decimal value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        #endregion

        #region Properties

        public override string ValueKind => "Decimal";

        #endregion
    }
}
