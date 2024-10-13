using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Windows.Markup;
using OutWit.Common.MVVM.Collections;
using OutWit.Common.Search;

namespace OutWit.Common.MVVM.Utils
{
    public static class CollectionUtils
    {
        public static NotifyCollectionChangedEventArgs ItemsAdded<TKey, TValue>(this SortedCollection<TKey, TValue> me, TValue values)
        {
            return new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Add, 
                new TValue[] { values });
        }

        public static NotifyCollectionChangedEventArgs ItemsAdded<TKey, TValue>(this SortedCollection<TKey, TValue> me, TValue[] values)
        {
            return new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Add,
                values);
        }

        public static NotifyCollectionChangedEventArgs ItemsRemoved<TKey, TValue>(this SortedCollection<TKey, TValue> me, TValue value)
        {
            return new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Remove,
                new [] { value },  me.IndexOfValue(value));
        }

        public static NotifyCollectionChangedEventArgs ItemsRemoved<TKey, TValue>(this SortedCollection<TKey, TValue> me, TValue[] values)
        {
            return new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Add,
                values, me.IndexOfValue(values[0]));
        }

        public static NotifyCollectionChangedEventArgs CollectionReset<TKey, TValue>(this SortedCollection<TKey, TValue> me)
        {
            return new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Reset);
        }

        public static TValue FindClosestValue<TValue>(this SortedCollection<int, TValue> me, int key)
        {
            var index = me.Keys.FindClosestValueIndex(key);

            return index == -1 ? default(TValue) : me.Values[index];
        }

        public static IList<TValue> FindValuesInRange<TValue>(this SortedCollection<int, TValue> me, int from, int to)
        {
            var fromIndex = me.Keys.FindGreaterOrEqualValueIndex(from);
            var toIndex = me.Keys.FindLessOrEqualValueIndex(to);

            if(fromIndex >= 0 && toIndex >= 0)
                return me.Values.Skip(fromIndex).Take(toIndex - fromIndex + 1).ToArray();

            if (fromIndex == -1 && toIndex == -1)
                return me.Values.ToArray();

            if (fromIndex == -1 && me.Keys[toIndex] > from)
                return me.Values.Take(toIndex).ToArray();

            if (toIndex == -1 && me.Keys[fromIndex] < to)
                return me.Values.Skip(fromIndex).ToArray();

            return new List<TValue>();
        }
    }
}
