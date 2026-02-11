using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace OutWit.Common.Settings.Serialization
{
    public sealed class SettingsSerializerDoubleList : SettingsSerializerBase<IReadOnlyList<double>>
    {
        #region Constants

        private const double TOLERANCE = 1e-10;

        #endregion

        #region Functions

        public override IReadOnlyList<double> Parse(string value, string tag)
        {
            if (string.IsNullOrEmpty(value))
                return Array.Empty<double>();

            return value.Split(',')
                .Select(s => double.Parse(s.Trim(), CultureInfo.InvariantCulture))
                .ToList();
        }

        public override string Format(IReadOnlyList<double> value)
        {
            return string.Join(", ", value.Select(d => d.ToString(CultureInfo.InvariantCulture)));
        }

        public override bool AreEqual(IReadOnlyList<double> a, IReadOnlyList<double> b)
        {
            if (ReferenceEquals(a, b))
                return true;

            if (a is null || b is null)
                return false;

            if (a.Count != b.Count)
                return false;

            return a.Zip(b, (x, y) => Math.Abs(x - y) < TOLERANCE).All(eq => eq);
        }

        #endregion

        #region Properties

        public override string ValueKind => "DoubleList";

        #endregion
    }
}
