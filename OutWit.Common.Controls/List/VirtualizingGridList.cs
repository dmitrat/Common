using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Commands;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.List
{
    public class VirtualizingGridList : ListBox
    {
        #region DependencyProperties

        public static readonly DependencyProperty RowsProperty = BindingUtils.Register<VirtualizingGridList, int>(nameof(Rows));
        public static readonly DependencyProperty ColumnsProperty = BindingUtils.Register<VirtualizingGridList, int>(nameof(Columns));

        public static readonly DependencyProperty ScrollBarStyleProperty = BindingUtils.Register<VirtualizingGridList, Style>(nameof(ScrollBarStyle));

        public static readonly DependencyProperty ItemsPaddingProperty = BindingUtils.Register<VirtualizingGridList, Thickness>(nameof(ItemsPadding));

        #endregion

        #region Constructors

        static VirtualizingGridList()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(VirtualizingGridList),
                new FrameworkPropertyMetadata(typeof(VirtualizingGridList)));
        }

        public VirtualizingGridList()
        {
            InitDefaults();
            InitCommands();
        }

        #endregion

        #region Initialization

        private void InitDefaults()
        {

        }

        private void InitCommands()
        {
        }

        #endregion

        #region Functions

     

        #endregion

        #region Properties


        [Bindable]
        public int Rows { get; set; }

        [Bindable]
        public int Columns { get; set; }

        [Bindable]
        public Style ScrollBarStyle { get; set; }

        [Bindable]
        public Thickness ItemsPadding { get; set; }

        #endregion
    }
}
