using System;
using System.Collections.Generic;
using System.Linq;

namespace OutWit.Common.Settings.Serialization
{
    public sealed class SettingsSerializerEnumList : SettingsSerializerBase<IReadOnlyList<object>>
    {
        #region Functions

        public override IReadOnlyList<object> Parse(string value, string tag)
        {
            if (string.IsNullOrEmpty(value))
                return Array.Empty<object>();

            var type = ResolveEnumType(tag);

            return value.Split(',')
                .Select(s => Enum.Parse(type, s.Trim()))
                .ToList();
        }

        public override string Format(IReadOnlyList<object> value)
        {
            return string.Join(", ", value.Select(v => v.ToString()));
        }

        public override bool AreEqual(IReadOnlyList<object> a, IReadOnlyList<object> b)
        {
            if (ReferenceEquals(a, b))
                return true;

            if (a is null || b is null)
                return false;

            if (a.Count != b.Count)
                return false;

            return a.Zip(b, (x, y) => Equals(x, y)).All(eq => eq);
        }

        private static Type ResolveEnumType(string tag)
        {
            var type = Type.GetType(tag)
                       ?? throw new ArgumentException($"Cannot resolve enum type: {tag}");

            if (!type.IsEnum)
                throw new ArgumentException($"Type is not an enum: {tag}");

            return type;
        }

        #endregion

        #region Properties

        public override string ValueKind => "EnumList";

        #endregion
    }
}
