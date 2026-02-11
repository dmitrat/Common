using System.Globalization;

namespace OutWit.Common.Settings.Serialization
{
    public sealed class SettingsSerializerLong : SettingsSerializerBase<long>
    {
        #region Functions

        public override long Parse(string value, string tag)
        {
            return long.Parse(value, CultureInfo.InvariantCulture);
        }

        public override string Format(long value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        #endregion

        #region Properties

        public override string ValueKind => "Long";

        #endregion
    }
}
