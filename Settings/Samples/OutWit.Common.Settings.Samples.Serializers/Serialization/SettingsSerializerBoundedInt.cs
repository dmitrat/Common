using System.Globalization;
using OutWit.Common.Settings.Serialization;
using OutWit.Common.Settings.Samples.Serializers.Types;

namespace OutWit.Common.Settings.Samples.Serializers.Serialization
{
    /// <summary>
    /// Serializes <see cref="BoundedInt"/> as "value|min|max" (e.g. "5|0|100").
    /// </summary>
    public sealed class SettingsSerializerBoundedInt : SettingsSerializerBase<BoundedInt>
    {
        #region Functions

        /// <inheritdoc />
        public override BoundedInt Parse(string value, string tag)
        {
            var parts = value.Split('|');
            return new BoundedInt
            {
                Value = int.Parse(parts[0], CultureInfo.InvariantCulture),
                Min = int.Parse(parts[1], CultureInfo.InvariantCulture),
                Max = int.Parse(parts[2], CultureInfo.InvariantCulture)
            };
        }

        /// <inheritdoc />
        public override string Format(BoundedInt value)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}|{1}|{2}",
                value.Value, value.Min, value.Max);
        }

        /// <inheritdoc />
        public override bool AreEqual(BoundedInt a, BoundedInt b)
        {
            return a.Is(b);
        }

        #endregion

        #region Properties

        /// <inheritdoc />
        public override string ValueKind => "BoundedInt";

        #endregion
    }
}
