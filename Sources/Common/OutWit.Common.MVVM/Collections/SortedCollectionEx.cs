using System;
using System.Collections.Generic;
using System.ComponentModel;
using OutWit.Common.Interfaces;

namespace OutWit.Common.MVVM.Collections
{
    public class SortedCollectionEx<TKey, TValue> : SortedCollection<TKey, TValue>, INotifyCollectionContentChanged
        where TValue : class, INotifyPropertyChanged
    {
        #region Events
        
        public event NotifyCollectionContentChangedEventHandler CollectionContentChanged = delegate { };

        #endregion

        #region Constructors

        public SortedCollectionEx(Func<TValue, TKey> keyGetter) : base(keyGetter)
        {
        }

        public SortedCollectionEx(Func<TValue, TKey> keyGetter, IEnumerable<TValue> values) : 
            base(keyGetter, values)
        {
        }

        #endregion

        #region Functions

        public override void Add(TValue value)
        {
            if (value != null)
                value.PropertyChanged += OnPropertyChanged;

            base.Add(value);
        }

        public override void Clear()
        {
            foreach (var value in this)
                value.PropertyChanged -= OnPropertyChanged;

            base.Clear();
        }

        public override TValue Remove(TKey key)
        {
            var value = base.Remove(key);

            if (value != null)
                value.PropertyChanged -= OnPropertyChanged;

            return value;

        }

        public override TValue RemoveAt(int index)
        {
            var value = base.RemoveAt(index);

            if (value != null)
                value.PropertyChanged -= OnPropertyChanged;

            return value;
        }

        #endregion

        #region Event Handlers

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            CollectionContentChanged(sender, e);
        }

        #endregion


    }
}
