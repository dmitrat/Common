using System;
using System.Globalization;

namespace OutWit.Common.Settings.Serialization
{
    public sealed class SettingsSerializerDouble : SettingsSerializerBase<double>
    {
        #region Constants

        private const double TOLERANCE = 1e-10;

        #endregion

        #region Functions

        public override double Parse(string value, string tag)
        {
            return double.Parse(value, CultureInfo.InvariantCulture);
        }

        public override string Format(double value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public override bool AreEqual(double a, double b)
        {
            return Math.Abs(a - b) < TOLERANCE;
        }

        #endregion

        #region Properties

        public override string ValueKind => "Double";

        #endregion
    }
}
