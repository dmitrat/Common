using System;
using System.Windows;
using System.Windows.Media;
using OutWit.Common.Controls.Output;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Special
{
    public class DropDownIconBox : IconOutput
    {
        #region DependencyProperties

        public static readonly DependencyProperty OverlayBrushProperty = BindingUtils.Register<DropDownIconBox, Brush>(nameof(OverlayBrush), Brushes.White);

        #endregion

        #region Constructors

        static DropDownIconBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DropDownIconBox),
                new FrameworkPropertyMetadata(typeof(DropDownIconBox)));
        }

        public DropDownIconBox()
        {

        }

        #endregion

        #region Properties

        [Bindable]
        public Brush OverlayBrush { get; set; }

        #endregion
    }
}
