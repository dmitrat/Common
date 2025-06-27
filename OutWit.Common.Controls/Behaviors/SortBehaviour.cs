using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using OutWit.Common.Controls.Interfaces;
using OutWit.Common.Exceptions;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Behaviors
{
    public class SortBehaviour
    {
        #region DependencyProperties

        public static readonly DependencyProperty CustomSorterProperty =
            DependencyProperty.RegisterAttached("CustomSorter", typeof(IColumnSorter), typeof(SortBehaviour));

        public static readonly DependencyProperty AllowCustomSortProperty =
            DependencyProperty.RegisterAttached("AllowCustomSort", typeof(bool),
                typeof(SortBehaviour), new UIPropertyMetadata(false, OnAllowCustomSortChanged));

        #endregion

        #region Functions

        public static IColumnSorter GetCustomSorter(DataGridColumn gridColumn)
        {
            return (IColumnSorter)gridColumn.GetValue(CustomSorterProperty);
        }
        public static void SetCustomSorter(DataGridColumn gridColumn, IColumnSorter value)
        {
            gridColumn.SetValue(CustomSorterProperty, value);
        }


        public static bool GetAllowCustomSort(DataGrid grid)
        {
            return (bool)grid.GetValue(AllowCustomSortProperty);
        }
        public static void SetAllowCustomSort(DataGrid grid, bool value)
        {
            grid.SetValue(AllowCustomSortProperty, value);
        } 

        #endregion

        #region EventHandlers

        private static void OnAllowCustomSortChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is DataGrid grid)) return;

            if ((bool)e.NewValue)
                grid.Sorting += OnSorting;

            else
                grid.Sorting -= OnSorting;

        }

        private static void OnSorting(object sender, DataGridSortingEventArgs e)
        {
            if (!(sender is DataGrid dataGrid) || !GetAllowCustomSort(dataGrid)) return;

            if (!(dataGrid.ItemsSource is ListCollectionView view))
                throw new ExceptionOf<SortBehaviour>("The DataGrid's ItemsSource property must be of type, ListCollectionView");

            var sorter = GetCustomSorter(e.Column);
            if (sorter == null)
                return;

            e.Handled = true;

            var direction = (e.Column.SortDirection != ListSortDirection.Ascending)
                                ? ListSortDirection.Ascending
                                : ListSortDirection.Descending;

            e.Column.SortDirection = sorter.SortDirection = direction;

            view.CustomSort = sorter;
        } 

        #endregion
    }
}
