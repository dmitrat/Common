using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
#if NET9_0_OR_GREATER
using System.Threading;
#endif
using OutWit.Common.MVVM.Interfaces;

namespace OutWit.Common.MVVM.Collections
{
    public class SortedCollection<TKey, TValue> : ISortedCollection<TValue>, INotifyPropertyChanged
        where TKey : notnull
        where TValue : notnull
    {
        #region Events

        public event PropertyChangedEventHandler? PropertyChanged;

        public event SortedCollectionEventHandler<TValue>? ItemsAdded;
        public event SortedCollectionEventHandler<TValue>? ItemsRemoved;
        public event SortedCollectionEventHandler<TValue>? CollectionClear;
        public event SortedCollectionEventHandler<TValue>? CollectionReset;

        #endregion

        #region Fields

        private readonly Func<TValue, TKey> m_keyGetter;

#if NET9_0_OR_GREATER
        private readonly Lock m_lock = new Lock();
#else
        private readonly object m_lock = new object();
#endif

        private SortedList<TKey, TValue> m_inner;
        private int m_count;

        #endregion

        #region Constructors

        public SortedCollection(Func<TValue, TKey> keyGetter)
        {
            m_keyGetter = keyGetter ?? throw new ArgumentNullException(nameof(keyGetter));
            m_inner = new SortedList<TKey, TValue>();
        }

        public SortedCollection(Func<TValue, TKey> keyGetter, IEnumerable<TValue> values)
        {
            m_keyGetter = keyGetter ?? throw new ArgumentNullException(nameof(keyGetter));
            m_inner = new SortedList<TKey, TValue>();
            
            if (values != null)
                Reset(values);
        }

        #endregion

        #region Functions

        public void Reset(IEnumerable<TValue> values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            lock (m_lock)
            {
                var valuesList = values.ToList();
                m_inner = new SortedList<TKey, TValue>(valuesList.ToDictionary(value => m_keyGetter(value), value => value));
                Count = m_inner.Count;
            }

            CollectionReset?.Invoke(this, Values.ToList());
        }

        public virtual void Add(TValue item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            var key = m_keyGetter(item);

            lock (m_lock)
            {
                m_inner.Add(key, item);
                Count = m_inner.Count;
            }

            ItemsAdded?.Invoke(this, new[] { item });
        }

        public virtual void Add(IReadOnlyCollection<TValue> items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            if (items.Count == 0)
                return;

            lock (m_lock)
            {
                foreach (var item in items)
                {
                    if (item == null)
                        throw new ArgumentException("Item cannot be null", nameof(items));

                    var key = m_keyGetter(item);
                    m_inner.Add(key, item);
                }

                Count = m_inner.Count;
            }

            ItemsAdded?.Invoke(this, items);
        }

        public virtual void Clear()
        {
            List<TValue> oldValues;
            
            lock (m_lock)
            {
                oldValues = m_inner.Values.ToList();
                m_inner.Clear();
                Count = 0;
            }

            CollectionClear?.Invoke(this, oldValues);
        }

        public bool Contains(TKey key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            lock (m_lock)
            {
                return m_inner.ContainsKey(key);
            }
        }

        public bool Contains(TValue value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            lock (m_lock)
            {
                return m_inner.ContainsValue(value);
            }
        }

        public void CopyTo(TValue[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            lock (m_lock)
            {
                m_inner.Values.CopyTo(array, arrayIndex);
            }
        }

        public virtual void Remove(IReadOnlyCollection<TValue> values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            if (values.Count == 0)
                return;

            lock (m_lock)
            {
                var keys = values.Select(x => m_keyGetter(x)).ToList();
                foreach (var key in keys)
                    m_inner.Remove(key);

                Count = m_inner.Count;
            }

            ItemsRemoved?.Invoke(this, values);
        }

        public virtual TValue? Remove(TKey key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            TValue? item;
            
            lock (m_lock)
            {
                if (!m_inner.TryGetValue(key, out item))
                    return default;

                m_inner.Remove(key);
                Count = m_inner.Count;
            }

            if (item != null)
                ItemsRemoved?.Invoke(this, new[] { item });

            return item;
        }

        public virtual TValue RemoveAt(int index)
        {
            TValue item;
            
            lock (m_lock)
            {
                if (index < 0 || index >= m_inner.Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                item = m_inner.Values[index];
                m_inner.RemoveAt(index);
                Count = m_inner.Count;
            }

            ItemsRemoved?.Invoke(this, new[] { item });

            return item;
        }

        public int IndexOfKey(TKey key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            lock (m_lock)
            {
                return m_inner.IndexOfKey(key);
            }
        }

        public int IndexOfValue(TValue value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            lock (m_lock)
            {
                return m_inner.IndexOfValue(value);
            }
        }

        public TValue? GetValueByKey(TKey key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            lock (m_lock)
            {
                return m_inner.TryGetValue(key, out var value) ? value : default;
            }
        }

        public TValue GetValueByIndex(int index)
        {
            lock (m_lock)
            {
                if (index < 0 || index >= m_inner.Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                return m_inner.Values[index];
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region IEnumerable

        public IEnumerator<TValue> GetEnumerator()
        {
            lock (m_lock)
            {
                return m_inner.Values.ToList().GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region Properties

        public IList<TKey> Keys
        {
            get
            {
                lock (m_lock)
                {
                    return m_inner.Keys.ToList();
                }
            }
        }

        public IList<TValue> Values
        {
            get
            {
                lock (m_lock)
                {
                    return m_inner.Values.ToList();
                }
            }
        }

        public int Count
        {
            get => m_count;
            private set
            {
                if (m_count == value)
                    return;

                m_count = value;
                OnPropertyChanged();
            }
        }

        #endregion
    }

    public delegate void SortedCollectionEventHandler<TValue>(object? sender, IReadOnlyCollection<TValue>? items)
        where TValue : notnull;
}
