using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Input;
using OutWit.Common.Aspects.Utils;
using OutWit.Common.Controls.DropDown;
using OutWit.Common.Locker;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Commands;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Selectors
{
    public class DropDownGridSelector : DropDownBase
    {
        #region DependencyProperties

        public static readonly DependencyProperty RowsProperty = BindingUtils.Register<DropDownGridSelector, int>(nameof(Rows), OnRowsChanged);
        public static readonly DependencyProperty MaxRowsProperty = BindingUtils.Register<DropDownGridSelector, int>(nameof(MaxRows), OnMaxRowsChanged);

        public static readonly DependencyProperty ColumnsProperty = BindingUtils.Register<DropDownGridSelector, int>(nameof(Columns), OnColumnsChanged);
        public static readonly DependencyProperty MaxColumnsProperty = BindingUtils.Register<DropDownGridSelector, int>(nameof(MaxColumns), OnMaxColumnsChanged);

        public static readonly DependencyProperty ItemsSourceProperty = BindingUtils.Register<DropDownGridSelector, IReadOnlyList<DropDownGridSelectorItem>>(nameof(ItemsSource));

        public static readonly DependencyProperty ItemSelectedCmdProperty = BindingUtils.Register<DropDownGridSelector, ICommand>(nameof(ItemSelectedCmd));
        public static readonly DependencyProperty DropDownClosingCmdProperty = BindingUtils.Register<DropDownGridSelector, ICommand>(nameof(DropDownClosingCmd));

        public static readonly DependencyProperty PopupStatusChangedCmdProperty = BindingUtils.Register<DropDownGridSelector, ICommand>(nameof(PopupStatusChangedCmd));
        #endregion

        #region Constructors

        static DropDownGridSelector()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DropDownGridSelector),
                new FrameworkPropertyMetadata(typeof(DropDownGridSelector)));
        }

        public DropDownGridSelector()
        {
            InitEvents();
        }

        #endregion

        #region Initialization

        private void InitEvents()
        {
            ItemSelectedCmd = new DelegateCommand(x => UpdateSelection(x as DropDownGridSelectorItem));
            DropDownClosingCmd = new DelegateCommand(x=>ResetSelection(x as MouseButtonEventArgs));
            PopupStatusChangedCmd = new DelegateCommand(x=>PopupStatusChanged((bool)x));
        }

        #endregion

        #region Functions

        private void PopupStatusChanged(bool status)
        {
            if(status)
                UpdateSelection();
        }

        private void ResetSelection(MouseButtonEventArgs eventArgs)
        {
            if(LastItem == null || eventArgs == null)
                return;

            Rows = LastItem.Row + 1;
            Columns = LastItem.Column + 1;

            //LastItem.IsSelected = true;

            eventArgs.Handled = true;
        }

        private void UpdateSelection(DropDownGridSelectorItem item)
        {
            if(item == null)
                return;

            LastItem = item;

            UpdateSelection(item.Row + 1, item.Column + 1);

        }

        private void UpdateSelection(int rows, int columns)
        {
            if(ItemsSource == null)
                return;

            foreach (var item in ItemsSource)
                item.IsSelected = item.Row < rows && item.Column < columns;
        }

        private void UpdateSelection()
        {
            UpdateSelection(Rows, Columns);
        }

        private void Reset()
        {
            if (MaxRows == 0 || MaxColumns == 0)
                return;

            var items = new DropDownGridSelectorItem[MaxRows * MaxColumns];

            var index = 0;

            for (int i = 0; i < MaxRows; i++)
            for (int j = 0; j < MaxColumns; j++)
                items[index++] = new DropDownGridSelectorItem(i, j);

            ItemsSource = items;

        }

        #endregion

        #region EventHandlers

        private static void OnRowsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var selector = (DropDownGridSelector)d;
            selector.UpdateSelection();
        }

        private static void OnMaxRowsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var selector = (DropDownGridSelector)d;
            selector.Reset();
            selector.UpdateSelection();
        }

        private static void OnColumnsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var selector = (DropDownGridSelector)d;
            selector.UpdateSelection();
        }

        private static void OnMaxColumnsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var selector = (DropDownGridSelector) d;
            selector.Reset();
            selector.UpdateSelection();
        }

        #endregion

        #region Properties

        private DropDownGridSelectorItem LastItem { get; set; }

        #endregion

        #region Bindable Properties

        [Bindable]
        public int Rows { get; set; }

        [Bindable]
        public int MaxRows { get; set; }

        [Bindable]
        public int Columns { get; set; }

        [Bindable]
        public int MaxColumns { get; set; }

        [Bindable]
        public IReadOnlyList<DropDownGridSelectorItem> ItemsSource { get; set; }

        [Bindable]
        public ICommand ItemSelectedCmd { get; private set; }

        [Bindable]
        public ICommand DropDownClosingCmd { get; private set; }

        [Bindable]
        public ICommand PopupStatusChangedCmd { get; private set; }

        #endregion
    }
}
