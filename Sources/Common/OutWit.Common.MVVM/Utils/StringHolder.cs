using System;
using System.ComponentModel;
using OutWit.Common.Abstract;
using OutWit.Common.Aspects;
using OutWit.Common.Values;

namespace OutWit.Common.MVVM.Utils
{
    public class StringHolder : ModelBase, INotifyPropertyChanged
    {
        #region Events

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        #endregion

        #region Constructors

        public StringHolder()
        {

        }

        public StringHolder(string value)
        {
            Value = value;
        }

        #endregion

        #region Functions

        public override string ToString()
        {
            return Value;
        }

        public bool AreSameAs(StringHolder value)
        {
            return AreSameAs(value.Value);
        }

        public bool AreSameAs(string value)
        {
            return Value.Equals(value, StringComparison.InvariantCultureIgnoreCase);
        }

        public static implicit operator string(StringHolder holder)
        {
            return holder?.Value;
        }

        public static implicit operator StringHolder(string value)
        {
            return new StringHolder {Value = value};
        }

        #endregion

        #region ModelBase

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (!(modelBase is StringHolder holder))
                return false;

            return holder.Value.Is(Value);
        }

        public override ModelBase Clone()
        {
            return new StringHolder {Value = Value};
        }

        #endregion

        #region Properties

        [Notify]
        public string Value { get; set; }

        #endregion
    }
}
