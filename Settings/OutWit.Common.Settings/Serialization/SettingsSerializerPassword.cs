namespace OutWit.Common.Settings.Serialization
{
    public sealed class SettingsSerializerPassword : SettingsSerializerString
    {
        #region Properties

        public override string ValueKind => "Password";

        #endregion
    }
}
