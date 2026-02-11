using System;

namespace OutWit.Common.Settings.Serialization
{
    public sealed class SettingsSerializerGuid : SettingsSerializerBase<Guid>
    {
        #region Functions

        public override Guid Parse(string value, string tag)
        {
            return Guid.Parse(value);
        }

        public override string Format(Guid value)
        {
            return value.ToString("D");
        }

        #endregion

        #region Properties

        public override string ValueKind => "Guid";

        #endregion
    }
}
