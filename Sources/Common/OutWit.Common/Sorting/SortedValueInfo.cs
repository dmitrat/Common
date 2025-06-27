using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Common.Sorting
{
    public abstract class SortedValueInfo<TSource, TValue>
    {
        protected SortedValueInfo(TSource source, TValue value, int index)
        {
            Source = source;
            Value = value;
            Index = index;
        }

        public TSource Source { get; }
        public TValue Value { get; }
        public int Index { get; }
    }
}
