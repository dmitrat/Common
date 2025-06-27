using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using OutWit.Common.Interfaces;

namespace OutWit.Common.Collections
{
    public class ObservableCollectionEx<TItem> : ObservableCollection<TItem>, INotifyCollectionContentChanged
        where TItem : class, INotifyPropertyChanged
    {
        #region Events

        public event NotifyCollectionContentChangedEventHandler CollectionContentChanged = delegate { };

        #endregion

        #region Constructors

        public ObservableCollectionEx() :
            base()
        {

        }

        public ObservableCollectionEx(IEnumerable<TItem> items) :
            base(items)
        {

        }

        #endregion

        #region Functions

        protected override void InsertItem(int index, TItem item)
        {
            if(item != null)
                item.PropertyChanged += OnPropertyChanged;

            base.InsertItem(index, item);
        }

        protected override void RemoveItem(int index)
        {
            if (this[index] != null)
                this[index].PropertyChanged -= OnPropertyChanged;

            base.RemoveItem(index);
        }

        protected override void SetItem(int index, TItem item)
        {
            if (this[index] != null)
                this[index].PropertyChanged -= OnPropertyChanged;

            if (item != null)
                item.PropertyChanged += OnPropertyChanged;

            base.SetItem(index, item);
        }

        protected override void ClearItems()
        {
            foreach (var item in this)
                item.PropertyChanged -= OnPropertyChanged;

            base.ClearItems();
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
