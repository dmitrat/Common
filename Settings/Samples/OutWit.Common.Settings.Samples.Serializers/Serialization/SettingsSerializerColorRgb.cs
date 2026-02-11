using System.Globalization;
using OutWit.Common.Settings.Serialization;
using OutWit.Common.Settings.Samples.Serializers.Types;

namespace OutWit.Common.Settings.Samples.Serializers.Serialization
{
    /// <summary>
    /// Serializes <see cref="ColorRgb"/> as "R,G,B" (e.g. "255,128,0").
    /// </summary>
    public sealed class SettingsSerializerColorRgb : SettingsSerializerBase<ColorRgb>
    {
        #region Functions

        /// <inheritdoc />
        public override ColorRgb Parse(string value, string tag)
        {
            var parts = value.Split(',');
            return new ColorRgb
            {
                R = byte.Parse(parts[0], CultureInfo.InvariantCulture),
                G = byte.Parse(parts[1], CultureInfo.InvariantCulture),
                B = byte.Parse(parts[2], CultureInfo.InvariantCulture)
            };
        }

        /// <inheritdoc />
        public override string Format(ColorRgb value)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0},{1},{2}",
                value.R, value.G, value.B);
        }

        /// <inheritdoc />
        public override bool AreEqual(ColorRgb a, ColorRgb b)
        {
            return a.Is(b);
        }

        #endregion

        #region Properties

        /// <inheritdoc />
        public override string ValueKind => "ColorRgb";

        #endregion
    }
}
