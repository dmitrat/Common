using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using OutWit.Common.Aspects;
using OutWit.Common.MVVM.Interfaces;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.MVVM.Collections
{
    public class SortedCollection<TKey, TValue> : ISortedCollection<TValue>, INotifyPropertyChanged
    {
        #region Events

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public event SortedCollectionEventHandler<TValue> ItemsAdded = delegate { };
        public event SortedCollectionEventHandler<TValue> ItemsRemoved = delegate { };
        public event SortedCollectionEventHandler<TValue> CollectionClear = delegate { };
        public event SortedCollectionEventHandler<TValue> CollectionReset = delegate { };

        #endregion

        #region Constructors

        public SortedCollection(Func<TValue, TKey> keyGetter)
        {
            KeyGetter = keyGetter;
            Inner = new SortedList<TKey, TValue>();
        }

        public SortedCollection(Func<TValue, TKey> keyGetter, IEnumerable<TValue> values)
        {
            KeyGetter = keyGetter;

            Reset(values);
        }

        #endregion

        #region Functions

        public void Reset(IEnumerable<TValue> values)
        {
            Inner = new SortedList<TKey, TValue>(values.ToDictionary(value => KeyGetter(value), value => value));

            Count = Inner.Count;

            CollectionReset(this, Values.ToList());
        }

        public virtual void Add(TValue item)
        {
            Inner.Add(KeyGetter(item), item);

            Count = Inner.Count;

            ItemsAdded(this, new[] {item});
        }

        public virtual void Add(IReadOnlyCollection<TValue> items)
        {
            foreach (var item in items)
                Inner.Add(KeyGetter(item), item);

            Count = Inner.Count;

            ItemsAdded(this, items);
        }

        public virtual void Clear()
        {
            Inner.Clear();
            Count = 0;

            CollectionClear(this, null);
        }

        public bool Contains(TKey key)
        {
            return Inner.ContainsKey(key);
        }

        public bool Contains(TValue value)
        {
            return Inner.ContainsValue(value);
        }

        public void CopyTo(TValue[] array, int arrayIndex)
        {
            Inner.Values.CopyTo(array, arrayIndex);
        }

        public virtual void Remove(IReadOnlyCollection<TValue> values)
        {
            var keys = values.Select(x => KeyGetter(x)).ToList();
            foreach (var key in keys)
                Inner.Remove(key);

            Count = Inner.Count;

            ItemsRemoved(this, values);
        }

        public virtual TValue Remove(TKey key)
        {
            var item = GetValueByKey(key);

            Inner.Remove(key);

            Count = Inner.Count;

            ItemsRemoved(this, new[] {item});

            return item;
        }

        public virtual TValue RemoveAt(int index)
        {
            var item = GetValueByIndex(index);

            Inner.RemoveAt(index);

            Count = Inner.Count;

            ItemsRemoved(this, new[] { item });

            return item;
        }

        public int IndexOfKey(TKey key)
        {
            return Inner.IndexOfKey(key);
        }

        public int IndexOfValue(TValue value)
        {
            return Inner.IndexOfValue(value);
        }

        public TValue GetValueByKey(TKey key)
        {
            return Inner[key];
        }

        public TValue GetValueByIndex(int index)
        {
            return Inner.Values[index];
        }
        #endregion

        #region IEnumerable

        public IEnumerator<TValue> GetEnumerator()
        {
            return Inner.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region Properties

        private Func<TValue, TKey> KeyGetter { get; }

        private SortedList<TKey, TValue> Inner { get; set; }


        public IList<TKey> Keys => Inner.Keys;

        public IList<TValue> Values => Inner.Values;


        [Notify]
        public int Count { get; private set; }

        #endregion
    }

    public delegate void SortedCollectionEventHandler<TValue>(object sender, IReadOnlyCollection<TValue> items);
}
