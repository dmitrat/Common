using OutWit.Common.Abstract;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OutWit.Common.Abstract;

namespace OutWit.Common.Collections
{
    public static class CollectionUtils
    {
        public static void Enumerate(this IEnumerable me, Action<object, int> action)
        {
            int index = 0;
            foreach (var item in me)
                action(item, index++);
        }

        public static bool Is<TKey, TValue>(this IDictionary<TKey, TValue> me, IDictionary<TKey, TValue> dictionary)
        {
            var meArray = me?.ToArray();
            var array = dictionary?.ToArray();

            if (meArray == null || array == null)
                return false;

            if (meArray.Length != array.Length)
                return false;

            for (int i = 0; i < array.Length; i++)
            {
                if (!Check(meArray[i].Key, array[i].Key) || !Check(meArray[i].Value, array[i].Value))
                    return false;
            }

            return true;
        }

        public static bool Is<TSource>(this IEnumerable<TSource> me, IEnumerable<TSource> collection)
        {
            if (me == null && collection == null)
                return true;

            if (me == null || collection == null)
                return false;

            var meArray = me.ToArray();
            var array = collection.ToArray();

            if (meArray.Length != array.Length)
                return false;

            return !meArray.Where((t, i) => !Check(t, array[i])).Any();
        }

        public static bool Is<TSource>(this IEnumerable<TSource> me, params TSource[] array)
        {
            return me.Is(array?.AsEnumerable());
        }


        //public static TValue TryGetValue<TKey, TValue>(this Dictionary<TKey, TValue> me, TKey key,
        //    TValue defaultValue = default(TValue))
        //{
        //    return me.TryGetValue(key, out var val) ? val : defaultValue;
        //}

        //public static TValue TryGetValue<TKey, TValue>(this IDictionary<TKey, TValue> me, TKey key,
        //    TValue defaultValue = default(TValue))
        //{
        //    return me.TryGetValue(key, out var val) ? val : defaultValue;
        //}

        public static TValue TryGetValue<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> me, TKey key,
            TValue defaultValue = default(TValue))
        {
            return me.TryGetValue(key, out var val) ? val : defaultValue;
        }

        public static bool Check(object first, object second)
        {
            if (first is ModelBase && second is ModelBase)
                return ((ModelBase)first).Is((ModelBase)second);

            return first.Equals(second);
        }

        public static T[][] Split<T>(this T[] me, int chunksCount)
        {
            if (chunksCount <= 0)
                return null;

            var chunkSize = (int)Math.Ceiling(me.Length / (double)chunksCount);
            var chunks = new T[chunksCount][];

            for (int i = 0; i < chunksCount; i++)
            {
                var start = i * chunkSize;
                var end = Math.Min(start + chunkSize, me.Length);

                var chunk = new T[end - start];
                Array.Copy(me, start, chunk, 0, chunk.Length);

                chunks[i] = chunk;
            }

            return chunks;
        }

        public static T[][] SplitParallel<T>(this T[] me, int chunksCount, int maxThreads = -1)
        {
            if (chunksCount <= 0)
                return null;

            var chunkSize = (int)Math.Ceiling(me.Length / (double)chunksCount);
            var chunks = new T[chunksCount][];

            var options = new ParallelOptions();
            if (maxThreads > 0)
                options.MaxDegreeOfParallelism = maxThreads;

            Parallel.For(0, chunksCount, options, i =>
            {
                var start = i * chunkSize;
                var end = Math.Min(start + chunkSize, me.Length);

                var chunk = new T[end - start];
                Array.Copy(me, start, chunk, 0, chunk.Length);

                chunks[i] = chunk;
            });

            return chunks;
        }

        public static ObservableCollection<T> ToObservable<T>(this IEnumerable<T> me)
        {
            return me == null ? new ObservableCollection<T>() : new ObservableCollection<T>(me);
        }

        public static TValue FindClosest<TValue>(this IEnumerable<TValue> me, int value, Func<TValue, int> getter)
        {
            var minimumValue = int.MaxValue;
            TValue minimumItem = default;

            var maximumValue = int.MinValue;
            TValue maximumItem = default;

            var closestValue = int.MaxValue;
            TValue closestItem = default;

            foreach (var item in me)
            {
                var itemValue = getter(item);

                if (itemValue < minimumValue)
                {
                    minimumValue = itemValue;
                    minimumItem = item;
                }

                if (itemValue > maximumValue)
                {
                    maximumValue = itemValue;
                    maximumItem = item;
                }

                if (Math.Abs(itemValue - value) < closestValue)
                {
                    closestValue = Math.Abs(itemValue - value);
                    closestItem = item;
                }
            }

            if (value >= maximumValue)
                return maximumItem;

            if (value <= minimumValue)
                return minimumItem;

            return closestItem;
        }

        public static void AddNotNull<TValue>(this ICollection<TValue> me, TValue value)
        {
            if (value != null)
                me.Add(value);
        }

        public static void AddOrUpdate<TKey1, TKey2, TDictionary, TValue>(this IDictionary<TKey1, TDictionary> me, TKey1 key1, TKey2 key2, TValue value)
            where TDictionary : IDictionary<TKey2, TValue>, new()
        {
            if (!me.ContainsKey(key1))
                me.Add(key1, new TDictionary());

            TDictionary dict = me[key1];
            if (!dict.ContainsKey(key2))
                dict.Add(key2, value);
            else
                dict[key2] = value;
        }

        public static void AddOrUpdate<TKey, TCollection, TValue>(this IDictionary<TKey, TCollection> me, TKey key, TValue value)
            where TCollection : ICollection<TValue>, new()
        {
            if (!me.ContainsKey(key))
                me.Add(key, new TCollection());

            me[key].Add(value);
        }

        public static void AddNotNullOrUpdate<TKey, TCollection, TValue>(this IDictionary<TKey, TCollection> me, TKey key, TValue value)
            where TCollection : ICollection<TValue>, new()
        {
            if (value != null)
                me.AddOrUpdate(key, value);
        }

        public static bool AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> me, TKey key, TValue value)
        {
            if (!me.ContainsKey(key))
            {
                me.Add(key, value);
                return true;
            }
            else
            {
                me[key] = value;
                return false;
            }
        }

        public static bool AddNotNullOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> me, TKey key, TValue value)
        {
            return value != null && me.AddOrUpdate(key, value);
        }

        public static void AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> me, IDictionary<TKey, TValue> values)
        {
            foreach (var pair in values)
                me.AddOrUpdate(pair.Key, pair.Value);
        }

        public static void AddNotNullOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> me, IDictionary<TKey, TValue> values)
        {
            foreach (var pair in values)
                me.AddNotNullOrUpdate(pair.Key, pair.Value);
        }

        public static void AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> me, (TKey key, TValue value) pair)
        {
            if (!me.ContainsKey(pair.key))
                me.Add(pair.key, pair.value);
            else
                me[pair.key] = pair.value;
        }

        public static void AddNotNullOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> me, (TKey key, TValue value) pair)
        {
            if (pair.value != null)
                me.AddOrUpdate((pair.key, pair.value));
        }
    }
}
