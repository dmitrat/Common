using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessagePack;
using OutWit.Common.Abstract;
using OutWit.Common.Aspects;
using OutWit.Common.Interfaces;
using OutWit.Common.MessagePack.Ranges;

namespace OutWit.Common.MessagePack.Values
{
    [MessagePackObject]
    public class ValueInSet<TValue> : ModelBase, INotifyPropertyChanged
        where TValue : struct, IComparable<TValue>
    {
        #region Events

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        #endregion

        #region Constructors

        private ValueInSet()
        {
        }

        public ValueInSet(TValue value, params TValue[] valuesSet) : 
            this(value, valuesSet.ToList().AsReadOnly())
        {

        }

        [SerializationConstructor]
        public ValueInSet(TValue value, IReadOnlyList<TValue> valuesSet)
        {
            Value = value;
            ValuesSet = valuesSet;
        }

        #endregion

        #region Functions

        public bool Reset(TValue value)
        {
            if (!ValuesSet.Contains(value))
                return false;

            Value = value;

            return true;
        }

        public override string ToString()
        {
            return $"{Value} [{string.Join(",", ValuesSet)}]";
        }

        #endregion

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (!(modelBase is ValueInSet<TValue> valueInSet))
                return false;

            if (valueInSet.Value.CompareTo(Value) != 0)
                return false;

            if (valueInSet.ValuesSet.Count != ValuesSet.Count)
                return false;

            for (int i = 0; i < ValuesSet.Count; i++)
            {
                if (valueInSet[i].CompareTo(ValuesSet[i]) != 0)
                    return false;
            }

            return true;
        }

        public override ModelBase Clone()
        {
            return new ValueInSet<TValue>(Value, ValuesSet.ToList().AsReadOnly());
        }

        #endregion

        #region Properties

        [IgnoreMember]
        public TValue this[int index] => ValuesSet[index];

        [IgnoreMember]
        public int Count => ValuesSet.Count;

        [Key(0)]
        [Notify]
        public TValue Value { get; set; }

        [Key(1)]
        [Notify]
        public IReadOnlyList<TValue> ValuesSet { get;  }

        #endregion
    }
}
