using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.List
{
    public class VirtualizingGridScrollViewer : ScrollViewer
    {
        #region DependencyProperties

        public static readonly DependencyProperty ScrollBarStyleProperty = BindingUtils.Register<VirtualizingGridScrollViewer, Style>(nameof(ScrollBarStyle));

        #endregion

        #region Constructors

        static VirtualizingGridScrollViewer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(VirtualizingGridScrollViewer),
                new FrameworkPropertyMetadata(typeof(VirtualizingGridScrollViewer)));
        }

        public VirtualizingGridScrollViewer()
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

        #region Properties


        [Bindable]
        public Style ScrollBarStyle { get; set; }

        #endregion
    }
}
