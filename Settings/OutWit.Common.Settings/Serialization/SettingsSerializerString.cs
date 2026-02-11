namespace OutWit.Common.Settings.Serialization
{
    public class SettingsSerializerString : SettingsSerializerBase<string>
    {
        #region Functions

        public override string Parse(string value, string tag)
        {
            return value;
        }

        public override string Format(string value)
        {
            return value;
        }

        #endregion

        #region Properties

        public override string ValueKind => "String";

        #endregion
    }
}
