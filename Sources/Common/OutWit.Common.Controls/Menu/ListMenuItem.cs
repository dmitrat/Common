using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Menu
{
    public class ListMenuItem : TextMenuItem
    {
        #region DependencyProperties

        public static readonly DependencyProperty SelectedItemProperty = BindingUtils.Register<ListMenuItem, object>(nameof(SelectedItem));

        #endregion

        #region Constructors

        public ListMenuItem()
        {
            InitEvents();
        }

        #endregion

        #region Initialization

        private void InitEvents()
        {
            this.Click += OnItemClicked;
            this.ItemContainerGenerator.StatusChanged += OnStatusChanged;
        } 

        #endregion

        #region Functions

        private void ResetSelectedItem()
        {

            foreach (var item in this.Items)
            {
                var menuItem = ItemContainerGenerator.ContainerFromItem(item) as MenuItem;
                if (menuItem == null)
                    continue;

                menuItem.FontWeight = FontWeights.Normal;

                if (menuItem.DataContext.Equals(SelectedItem))
                    menuItem.FontWeight = FontWeights.Bold;
            }
        } 

        #endregion

        #region Event Handlers

        private void OnItemClicked(object sender, System.Windows.RoutedEventArgs e)
        {
            SelectedItem = (e.OriginalSource as MenuItem)?.DataContext;
            ResetSelectedItem();
        }
        private void OnStatusChanged(object sender, EventArgs e)
        {
            ResetSelectedItem();
        }

        #endregion

        #region Properties

        [Bindable]
        public object SelectedItem { get; set; }

        #endregion
    }
}
