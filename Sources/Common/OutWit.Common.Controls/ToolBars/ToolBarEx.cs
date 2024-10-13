using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.ToolBars
{
    public class ToolBarEx:ToolBar
    {
        #region Events

        public event ToolBarExEventHandler OverflowStatusChanged = delegate { };

        #endregion

        #region DependencyProperties

        public static readonly DependencyProperty IsOverflowProperty = BindingUtils.Register<ToolBarEx, bool>(nameof(IsOverflow));

        #endregion

        #region Constructors

        static ToolBarEx()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ToolBarEx),
                new FrameworkPropertyMetadata(typeof(ToolBarEx)));
        }

        public ToolBarEx()
        {
            InitEvents();
        }

        #endregion

        #region Initialization

        private void InitEvents()
        {
            this.SizeChanged += OnSizeChanged;
        }

        #endregion

        #region Event Handlers

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            IsOverflow = HasOverflowItems;

            OverflowStatusChanged(this);
        }

        #endregion

        #region Bindable Properties

        [Bindable]
        public bool IsOverflow { get; private set; }

        #endregion
    }

    public delegate void ToolBarExEventHandler(ToolBarEx sender);
}
