using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace OutWit.Common.Collections
{
    public class GroupedObservableCollection<TItem, TGroupValue> : ObservableCollectionEx<TItem>
        where TItem: class, INotifyPropertyChanged
    {
        #region Constructors

        public GroupedObservableCollection(Func<TItem, TGroupValue> groupValueGetter)
        {
            GroupValueGetter = groupValueGetter;
            

            InitDefaults();
            InitEvents();
        }

        #endregion

        #region Initialization

        private void InitDefaults()
        {
            GroupedItems = new Dictionary<TGroupValue, ObservableCollectionEx<TItem>>();
            Groups = new ObservableCollection<ObservableCollectionEx<TItem>>();
        }

        private void InitEvents()
        {
            CollectionContentChanged += OnCollectionContentChanged;
        }

        #endregion

        #region Functions

        protected override void InsertItem(int index, TItem item)
        {
            if(item !=null)
                AddItemToGroup(item, GroupValueGetter(item));

            base.InsertItem(index, item);
        }

        protected override void RemoveItem(int index)
        {
            if (this[index] != null)
                RemoveItemFromGroup(this[index], GroupValueGetter(this[index]));

            base.RemoveItem(index);
        }

        protected override void SetItem(int index, TItem item)
        {
            if (this[index] != null)
                RemoveItemFromGroup(this[index], GroupValueGetter(this[index]));

            if (item != null)
                AddItemToGroup(item, GroupValueGetter(item));

            base.SetItem(index, item);
        }

        protected override void ClearItems()
        {
            GroupedItems.Clear();
            Groups.Clear();
            base.ClearItems();
        }

        private void AddItemToGroup(TItem item, TGroupValue value)
        {
            var group = GetGroup(value);

            if (!group.Contains(item))
                group.Add(item);
        }

        private void RemoveItemFromGroup(TItem item, TGroupValue value)
        {
            var group = GetGroup(value);
            if (group.Contains(item))
                group.Remove(item);

            CheckEmptyGroup(value);
        }

        private ObservableCollectionEx<TItem> GetGroup(TGroupValue value)
        {
            if (GroupedItems.ContainsKey(value))
                return GroupedItems[value];

            var group = new ObservableCollectionEx<TItem>();
            Groups.Add(group);
            GroupedItems.Add(value, group);

            return group;
        }

        private bool CheckGroups()
        {
            foreach (var groupItems in GroupedItems)
            {
                var groupValue = groupItems.Key;
                var group = groupItems.Value;

                var item = group.FirstOrDefault(x => !GroupValueGetter(x).Equals(groupValue));
                if(item == null)
                    continue;

                group.Remove(item);

                CheckEmptyGroup(groupValue);

                return true;
            }

            return false;

        }

        private void CheckEmptyGroup(TGroupValue value)
        {
            var group = GetGroup(value);

            if (group.Count > 0)
                return;

            GroupedItems.Remove(value);
            Groups.Remove(group);
        }

        #endregion

        #region Events

        private void OnCollectionContentChanged(object sender, PropertyChangedEventArgs e)
        {
            var item = sender as TItem;
            if(item == null)
                return;

            if (CheckGroups())
                AddItemToGroup(item, GroupValueGetter(item));

        }

        #endregion

        #region Properties

        public ObservableCollection<ObservableCollectionEx<TItem>> Groups { get; private set; }

        private Dictionary<TGroupValue, ObservableCollectionEx<TItem>> GroupedItems { get; set; }

        private Func<TItem, TGroupValue> GroupValueGetter { get; }

        #endregion


    }
}
