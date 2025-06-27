using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Commands;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Grids
{
    public class TileGrid : DataGrid
    {
        #region DependencyProperties

        public static readonly DependencyProperty TileBackgroundProperty = BindingUtils.Register<TileGrid, Brush>(nameof(TileBackground));
        public static readonly DependencyProperty TileBorderBrushProperty = BindingUtils.Register<TileGrid, Brush>(nameof(TileBorderBrush)); 
        public static readonly DependencyProperty SelectRowCommandProperty = BindingUtils.Register<TileGrid, ICommand>(nameof(SelectRowCommand));

        #endregion

        #region Constructors

        static TileGrid()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TileGrid),
                new FrameworkPropertyMetadata(typeof(TileGrid)));
        }

        public TileGrid()
        {
            InitEvents();
        }

        #endregion

        #region Initialization

        private void InitEvents()
        {
            SelectRowCommand = new DelegateCommand(x => SelectRow(x as DataGridRow));
        }

        #endregion

        #region Functions

        private void SelectRow(DataGridRow row)
        {
            if (row == null)
                return;

            //this.SelectedItems.Clear();
            row.IsSelected = true;
        }

        #endregion

        #region Properties

        [Bindable]
        public Brush TileBackground { get; set; }

        [Bindable]
        public Brush TileBorderBrush { get; set; }

        [Bindable]
        public ICommand SelectRowCommand { get; set; }
        #endregion
    }
}
