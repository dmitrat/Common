using MemoryPack;
using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Common.Settings.Samples.Serializers.Types
{
    /// <summary>
    /// An integer value constrained to a [Min, Max] range.
    /// Useful for settings like volume, gain, or threshold.
    /// </summary>
    [MemoryPackable]
    public sealed partial class BoundedInt : ModelBase
    {
        #region Functions

        public override string ToString()
        {
            return $"{Value} [{Min}..{Max}]";
        }

        #endregion

        #region ModelBase

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not BoundedInt other)
                return false;

            return Value.Is(other.Value) &&
                   Min.Is(other.Min) &&
                   Max.Is(other.Max);
        }

        public override ModelBase Clone()
        {
            return new BoundedInt
            {
                Value = Value,
                Min = Min,
                Max = Max
            };
        }

        #endregion

        #region Properties

        public int Value { get; set; }

        public int Min { get; set; }

        public int Max { get; set; }

        #endregion
    }
}
