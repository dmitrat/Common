using System;
using System.Collections.Generic;
using System.ComponentModel;
#if NET9_0_OR_GREATER
using System.Threading;
#endif
using OutWit.Common.Interfaces;

namespace OutWit.Common.MVVM.Collections
{
    /// <summary>
    /// Sorted collection that observes property changes of its items.
    /// When any item's property changes, CollectionContentChanged event is raised.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the collection</typeparam>
    /// <typeparam name="TValue">The type of values in the collection (must implement INotifyPropertyChanged)</typeparam>
    public class ObservableSortedCollection<TKey, TValue> : SortedCollection<TKey, TValue>, INotifyCollectionContentChanged
        where TKey : notnull
        where TValue : class, INotifyPropertyChanged
    {
        #region Events
        
        public event NotifyCollectionContentChangedEventHandler? CollectionContentChanged;

        #endregion

        #region Fields

#if NET9_0_OR_GREATER
        private readonly Lock m_subscriptionLock = new Lock();
#else
        private readonly object m_subscriptionLock = new object();
#endif

        #endregion

        #region Constructors

        public ObservableSortedCollection(Func<TValue, TKey> keyGetter) : base(keyGetter)
        {
        }

        public ObservableSortedCollection(Func<TValue, TKey> keyGetter, IEnumerable<TValue> values) : 
            base(keyGetter)
        {
            if (values != null)
            {
                SubscribeToItems(values);
                Reset(values);
            }
        }

        #endregion

        #region Functions

        public override void Add(TValue value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            lock (m_subscriptionLock)
            {
                value.PropertyChanged += OnItemPropertyChanged;
            }

            base.Add(value);
        }

        public override void Add(IReadOnlyCollection<TValue> items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            lock (m_subscriptionLock)
            {
                foreach (var value in items)
                {
                    if (value == null)
                        throw new ArgumentException("Item cannot be null", nameof(items));

                    value.PropertyChanged += OnItemPropertyChanged;
                }
            }

            base.Add(items);
        }

        public override void Clear()
        {
            var values = Values;

            lock (m_subscriptionLock)
            {
                foreach (var value in values)
                {
                    if (value != null)
                        value.PropertyChanged -= OnItemPropertyChanged;
                }
            }

            base.Clear();
        }

        public override TValue? Remove(TKey key)
        {
            var value = base.Remove(key);

            if (value != null)
            {
                lock (m_subscriptionLock)
                {
                    value.PropertyChanged -= OnItemPropertyChanged;
                }
            }

            return value;
        }

        public override TValue RemoveAt(int index)
        {
            var value = base.RemoveAt(index);

            if (value != null)
            {
                lock (m_subscriptionLock)
                {
                    value.PropertyChanged -= OnItemPropertyChanged;
                }
            }

            return value;
        }

        public override void Remove(IReadOnlyCollection<TValue> values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            lock (m_subscriptionLock)
            {
                foreach (var value in values)
                {
                    if (value != null)
                        value.PropertyChanged -= OnItemPropertyChanged;
                }
            }

            base.Remove(values);
        }

        private void SubscribeToItems(IEnumerable<TValue> items)
        {
            if (items == null)
                return;

            lock (m_subscriptionLock)
            {
                foreach (var item in items)
                {
                    if (item != null)
                        item.PropertyChanged += OnItemPropertyChanged;
                }
            }
        }

        #endregion

        #region Event Handlers

        private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            CollectionContentChanged?.Invoke(sender, e);
        }

        #endregion
    }
}
