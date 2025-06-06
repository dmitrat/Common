using OutWit.Common.Abstract;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Common.Collections
{
    public static class CollectionUtils
    {
        #region Enumeration

        public static void ForEach<T>(this IEnumerable<T> me, Action<T, int> action)
        {
            int index = 0;
            foreach (T item in me)
                action(item, index++);
        }

        #endregion

        #region Comparison

        public static bool Check(this IDictionary me, IDictionary dictionary)
        {
            if (me == null && dictionary == null)
                return true;

            if (me == null || dictionary == null)
                return false;

            if (ReferenceEquals(me, dictionary))
                return true;

            if (me.Count != dictionary.Count)
                return false;

            foreach (var key in me.Keys)
            {
                if (!dictionary.Contains(key))
                    return false;
                
                if (!me[key].Check(dictionary[key]))
                    return false;
            }

            return true;
        }

        public static bool Is<TKey, TValue>(this IDictionary<TKey, TValue> me, IDictionary<TKey, TValue> dictionary)
        {
            if (me == null && dictionary == null)
                return true;

            if (me == null || dictionary == null)
                return false;

            if (ReferenceEquals(me, dictionary))
                return true;

            if (me.Count != dictionary.Count)
                return false;

            foreach (var pair in me)
            {
                if (!dictionary.TryGetValue(pair.Key, out var value) || !pair.Value.Check(value))
                    return false;
            }

            return true;
        }

        public static bool Check(this ICollection me, ICollection collection)
        {
            if (me == null && collection == null)
                return true;

            if (me == null || collection == null)
                return false;
            
            if (ReferenceEquals(me, collection))
                return true;

            if (me.Count != collection.Count)
                return false;

            return me.Cast<object>().Is(collection.Cast<object>());
        }

        public static bool Is<TSource>(this IEnumerable<TSource> me, IEnumerable<TSource> collection)
        {
            if (me == null && collection == null)
                return true;

            if (me == null || collection == null)
                return false;

            if (ReferenceEquals(me, collection))
                return true;

            using (var enumerator1 = me.GetEnumerator())
            {
                using (var enumerator2 = collection.GetEnumerator())
                {
                    while (enumerator1.MoveNext())
                    {
                        if (!enumerator2.MoveNext() || !enumerator1.Current.Check(enumerator2.Current))
                            return false;
                    }

                    return !enumerator2.MoveNext();
                }
            }
        }

        public static bool Is<TSource>(this IEnumerable<TSource> me, params TSource[] array)
        {
            return me.Is(array?.AsEnumerable());
        }

        #endregion

        #region Split

        public static IEnumerable<T[]> Split<T>(this T[] me, int chunksCount)
        {
            if (chunksCount <= 0)
                return null;

            if (me.Length == 0)
                return Enumerable.Empty<T[]>();

            var chunkSize = (int)Math.Ceiling(me.Length / (double)chunksCount);

#if NET6_0_OR_GREATER

            return me.Chunk(chunkSize);

#else
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

#endif
        }

        public static IEnumerable<T[]> SplitParallel<T>(this T[] me, int chunksCount, int maxThreads = -1)
        {
            if (chunksCount <= 0)
                return null;

            if (me.Length == 0)
                return Enumerable.Empty<T[]>();

            var chunkSize = (int)Math.Ceiling(me.Length / (double)chunksCount);

#if NET6_0_OR_GREATER

            return me.AsParallel().Chunk(chunkSize);
#else

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
#endif
        }

        #endregion

        #region TryGetValue
        
        public static TValue TryGetValue<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> me, TKey key, TValue defaultValue = default(TValue))
        {
            return me.TryGetValue(key, out var val) ? val : defaultValue;
        }

        #endregion

        #region Add

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

        #endregion

        #region Find

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

        #endregion

        public static ObservableCollection<T> ToObservable<T>(this IEnumerable<T> me)
        {
            return me == null ? new ObservableCollection<T>() : new ObservableCollection<T>(me);
        }
        

    }
}
