namespace OutWit.Common.Settings.Serialization
{
    public sealed class SettingsSerializerBoolean : SettingsSerializerBase<bool>
    {
        #region Functions

        public override bool Parse(string value, string tag)
        {
            return bool.Parse(value);
        }

        public override string Format(bool value)
        {
            return value.ToString();
        }

        #endregion

        #region Properties

        public override string ValueKind => "Boolean";

        #endregion
    }
}
