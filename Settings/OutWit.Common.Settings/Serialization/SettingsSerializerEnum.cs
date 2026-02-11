using System;

namespace OutWit.Common.Settings.Serialization
{
    public sealed class SettingsSerializerEnum : SettingsSerializerBase<object>
    {
        #region Functions

        public override object Parse(string value, string tag)
        {
            var type = Type.GetType(tag)
                       ?? throw new ArgumentException($"Cannot resolve enum type: {tag}");

            if (!type.IsEnum)
                throw new ArgumentException($"Type is not an enum: {tag}");

            return Enum.Parse(type, value);
        }

        public override string Format(object value)
        {
            return value.ToString()!;
        }

        public override bool AreEqual(object a, object b)
        {
            return Equals(a, b);
        }

        #endregion

        #region Properties

        public override string ValueKind => "Enum";

        #endregion
    }
}
