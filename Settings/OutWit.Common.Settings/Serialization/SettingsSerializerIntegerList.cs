using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace OutWit.Common.Settings.Serialization
{
    public sealed class SettingsSerializerIntegerList : SettingsSerializerBase<IReadOnlyList<int>>
    {
        #region Functions

        public override IReadOnlyList<int> Parse(string value, string tag)
        {
            if (string.IsNullOrEmpty(value))
                return Array.Empty<int>();

            return value.Split(',')
                .Select(s => int.Parse(s.Trim(), CultureInfo.InvariantCulture))
                .ToList();
        }

        public override string Format(IReadOnlyList<int> value)
        {
            return string.Join(", ", value.Select(i => i.ToString(CultureInfo.InvariantCulture)));
        }

        public override bool AreEqual(IReadOnlyList<int> a, IReadOnlyList<int> b)
        {
            if (ReferenceEquals(a, b))
                return true;

            if (a is null || b is null)
                return false;

            return a.SequenceEqual(b);
        }

        #endregion

        #region Properties

        public override string ValueKind => "IntegerList";

        #endregion
    }
}
