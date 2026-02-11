using System.Globalization;

namespace OutWit.Common.Settings.Serialization
{
    public sealed class SettingsSerializerInteger : SettingsSerializerBase<int>
    {
        #region Functions

        public override int Parse(string value, string tag)
        {
            return int.Parse(value, CultureInfo.InvariantCulture);
        }

        public override string Format(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        #endregion

        #region Properties

        public override string ValueKind => "Integer";

        #endregion
    }
}
