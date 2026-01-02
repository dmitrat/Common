using System;
using OutWit.Common.Abstract;
using OutWit.Common.Aspects;
using OutWit.Common.Values;

namespace OutWit.Common.MVVM.Model
{
    public class StringHolder : ModelBase
    {
        #region Constructors

        public StringHolder()
        {

        }

        public StringHolder(string value)
        {
            Value = value;
        }

        #endregion

        #region ModelBase

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not StringHolder holder)
                return false;

            return holder.Value.Is(Value);
        }

#if NET6_0_OR_GREATER
        public override StringHolder Clone()
#else
        public override ModelBase Clone()
#endif
        {
            return new StringHolder { Value = Value };
        }

        #endregion

        #region Functions

        public override string ToString()
        {
            return $"{Value}";
        }

        public bool IsSameAs(StringHolder value)
        {
            return IsSameAs(value.Value);
        }

        public bool IsSameAs(string? value)
        {
            return Value?.Equals(value, StringComparison.InvariantCultureIgnoreCase) == true;
        }

        #endregion

        #region Operators

        public static implicit operator string?(StringHolder? holder)
        {
            return holder?.Value;
        }

        public static implicit operator StringHolder(string? value)
        {
            return new StringHolder { Value = value };
        }

        #endregion

        #region Properties

        [Notify]
        public string? Value { get; set; }

        #endregion
    }
}
