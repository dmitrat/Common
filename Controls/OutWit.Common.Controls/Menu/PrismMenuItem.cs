using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Menu
{
    public class PrismMenuItem : TextMenuItem
    {
        #region DependencyProperties

        public static readonly DependencyProperty DynamicItemsProperty = BindingUtils.Register<PrismMenuItem, INotifyCollectionChanged>(nameof(DynamicItems), OnDynamicItemsChanged);

        #endregion

        #region Functions

        private void ResetItems()
        {
            if (DynamicItems == null)
                return;

            DynamicItems.CollectionChanged += OnDynamicItemsCollectionChanged;

            UpdateItems();
            
        }

        private void UpdateItems()
        {
            Items.Clear();

            if (!(DynamicItems is IEnumerable source))
                return;

            foreach (var item in source)
                Items.Add(item);
        }


        #endregion

        #region EventHandlers

        private static void OnDynamicItemsChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var menu = (PrismMenuItem)source;
            menu.ResetItems();
        }

        private void OnDynamicItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateItems();
        }

        #endregion

        #region Properties

        [Bindable]
        public INotifyCollectionChanged DynamicItems { get; set; }

        #endregion
    }
}
