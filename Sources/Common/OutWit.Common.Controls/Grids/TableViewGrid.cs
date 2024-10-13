using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Table;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Grids
{
    public class TableViewGrid : DataGrid
    {
        #region DependencyProperties

        public static readonly DependencyProperty TableProperty = BindingUtils.Register<TableViewGrid, TableView>(nameof(Table), OnTableChanged);

        public static readonly DependencyProperty PageIndexProperty = BindingUtils.Register<TableViewGrid, int>(nameof(PageIndex), OnPageIndexChanged);

        public static readonly DependencyProperty RowDoubleClickCommandProperty = BindingUtils.Register<TableViewGrid, ICommand>(nameof(RowDoubleClickCommand));

        public static readonly DependencyProperty RowClickCommandProperty = BindingUtils.Register<TableViewGrid, ICommand>(nameof(RowClickCommand));

        #endregion

        #region Constructors

        static TableViewGrid()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TableViewGrid),
                new FrameworkPropertyMetadata(typeof(TableViewGrid)));
        }

        public TableViewGrid()
        {
            InitDefaults();
            InitEvents();
        }

        #endregion

        #region Initialization

        private void InitDefaults()
        {
            //AutoGenerateColumns = false;
            //IsReadOnly = true;
            //CanUserReorderColumns = false;
            //CanUserSortColumns = false;

            //RowHeaderWidth = 0;

            //SelectionMode = DataGridSelectionMode.Extended;
            //SelectionUnit = DataGridSelectionUnit.Cell;

            //this.ItemContainerStyle.Setters.Add(new EventSetter(DataGridRow.MouseDoubleClickEvent, Control_OnPreviewMouseDoubleClick));
            
        }

        private void InitEvents()
        {
        }

        #endregion

        #region Functions

        private void ResetColumns()
        {
            this.Columns.Clear();

            if(Table == null)
                return;

            for (int i = 0; i < Table.ColumnsCount; i++)
            {
                var column = new DataGridTextColumn()
                {
                    Binding = new Binding($"Values[{i}].Text"),
                    Header = new TextBlock
                    {
                        Text = Table.HeaderRow.Values[i].Text,
                        LayoutTransform = new RotateTransform(270),
                        FontWeight = FontWeights.Bold,
                        TextWrapping = TextWrapping.Wrap
                    }
                };

                Columns.Add(column);
            }

            ResetColumnWidth();
        }

        private void ResetPage()
        {
            ItemsSource = Table?.Pages[PageIndex];
        }

        private void ResetColumnWidth()
        {
            if (Width > 0 && Columns.Count > 0)
                foreach (var column in Columns)
                {
                    column.Width = new DataGridLength(Width / Columns.Count);
                }
        }

        #endregion

        #region Event Handlers

        private void OnRowMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(sender is DataGridRow row) 
                RowDoubleClickCommand?.Execute(row.Item);
        }

        private void OnRowMouseClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGridRow row)
                RowClickCommand?.Execute(row.Item);
        }

        protected override void OnLoadingRow(DataGridRowEventArgs e)
        {
            e.Row.MouseDoubleClick += OnRowMouseDoubleClick;
            e.Row.MouseLeftButtonUp += OnRowMouseClick;
            base.OnLoadingRow(e);
        }

        protected override void OnUnloadingRow(DataGridRowEventArgs e)
        {
            e.Row.MouseDoubleClick -= OnRowMouseDoubleClick;
            e.Row.MouseLeftButtonUp -= OnRowMouseClick;
            base.OnUnloadingRow(e);
        }

        private static void OnPageIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var grid = (TableViewGrid) d;
            grid.ResetPage();
        } 

        private static void OnTableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var grid = (TableViewGrid) d;
            grid.ResetColumns();
            grid.ResetPage();
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.IsProperty((TableViewGrid g) => g.Width))
                ResetColumnWidth();

            base.OnPropertyChanged(e);
        }

        #endregion

        #region Properties

        [Bindable]
        public ICommand RowDoubleClickCommand { get; set; }

        [Bindable]
        public ICommand RowClickCommand { get; set; }

        [Bindable]
        public TableView Table { get; set; }

        [Bindable]
        public int PageIndex { get; set; }

        #endregion
    }
}
