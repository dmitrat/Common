using MemoryPack;
using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Common.Settings.Samples.Serializers.Types
{
    /// <summary>
    /// An RGB color value with byte components [0..255].
    /// Useful for accent color, highlight, or theme settings.
    /// </summary>
    [MemoryPackable]
    public sealed partial class ColorRgb : ModelBase
    {
        #region Functions

        public override string ToString()
        {
            return $"({R}, {G}, {B})";
        }

        #endregion

        #region ModelBase

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not ColorRgb other)
                return false;

            return R.Is(other.R) &&
                   G.Is(other.G) &&
                   B.Is(other.B);
        }

        public override ModelBase Clone()
        {
            return new ColorRgb
            {
                R = R,
                G = G,
                B = B
            };
        }

        #endregion

        #region Properties

        public byte R { get; set; }

        public byte G { get; set; }

        public byte B { get; set; }

        #endregion
    }
}
