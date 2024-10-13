using System;
using System.Windows;
using System.Windows.Media;
using OutWit.Common.Controls.Output;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Special
{
    public class IconBox : IconOutput
    {
        #region DependencyProperties

        public static readonly DependencyProperty OverlayBrushProperty = BindingUtils.Register<IconBox, Brush>(nameof(OverlayBrush), Brushes.White);

        #endregion

        #region Constructors

        static IconBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(IconBox),
                new FrameworkPropertyMetadata(typeof(IconBox)));
        }

        public IconBox()
        {

        }

        #endregion

        #region Properties

        [Bindable]
        public Brush OverlayBrush { get; set; }

        #endregion
    }
}
