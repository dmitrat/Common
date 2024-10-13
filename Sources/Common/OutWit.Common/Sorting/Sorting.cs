using System;
using System.Collections.Generic;
using System.Linq;

namespace OutWit.Common.Sorting
{
    public static class Sorting
    {
        private class SortedValueInfoWrapper<TSource, TValue> : SortedValueInfo<TSource, TValue>
        {
            public SortedValueInfoWrapper(TSource source, TValue value, int index) : base(source, value, index)
            {
            }
        }

        public static SortedValueInfo<TSource, TValue> MaxInfo<TSource, TValue>(this IReadOnlyList<TSource> me, Func<TSource, TValue> selector, IComparer<TValue> comparer = null)
        {
            if (me == null || me.Count == 0)
                return null;

            if (comparer == null)
                comparer = Comparer<TValue>.Default;

            var maxSource = me.First();
            var maxValue = selector(maxSource);
            var maxValueIndex = 0;

            for (int i = 1; i < me.Count; i++)
            {
                var source = me[i];
                var value = selector(source);
                if (comparer.Compare(value, maxValue) > 0)
                {
                    maxSource = source;
                    maxValue = value;
                    maxValueIndex = i;
                }
            }

            return new SortedValueInfoWrapper<TSource, TValue>(maxSource, maxValue, maxValueIndex);
        }
    }
}
