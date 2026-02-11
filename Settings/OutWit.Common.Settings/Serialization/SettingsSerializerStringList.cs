using System;
using System.Collections.Generic;
using System.Linq;

namespace OutWit.Common.Settings.Serialization
{
    public sealed class SettingsSerializerStringList : SettingsSerializerBase<IReadOnlyList<string>>
    {
        #region Functions

        public override IReadOnlyList<string> Parse(string value, string tag)
        {
            if (string.IsNullOrEmpty(value))
                return Array.Empty<string>();

            return value.Split(',').Select(s => s.Trim()).ToList();
        }

        public override string Format(IReadOnlyList<string> value)
        {
            return string.Join(", ", value);
        }

        public override bool AreEqual(IReadOnlyList<string> a, IReadOnlyList<string> b)
        {
            if (ReferenceEquals(a, b))
                return true;

            if (a is null || b is null)
                return false;

            return a.SequenceEqual(b);
        }

        #endregion

        #region Properties

        public override string ValueKind => "StringList";

        #endregion
    }
}
