using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using OutWit.Common.Controls.Converters;
using OutWit.Common.Controls.Output;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.DropDown
{
    public abstract class DropDownBase : Control
    {
        #region DependencyProperties

        public static readonly DependencyProperty OverlayBrushProperty = BindingUtils.Register<DropDownBase, Brush>(nameof(OverlayBrush));

        public static readonly DependencyProperty IconKindProperty = BindingUtils.Register<DropDownBase, PackIconKind?>(nameof(IconKind));
        public static readonly DependencyProperty IconPositionProperty = BindingUtils.Register<DropDownBase, IconPositions>(nameof(IconPosition));
        public static readonly DependencyProperty IconHeightProperty = BindingUtils.Register<DropDownBase, double>(nameof(IconHeight), 24);
        public static readonly DependencyProperty IconWidthProperty = BindingUtils.Register<DropDownBase, double>(nameof(IconWidth), 24);

        public static readonly DependencyProperty PopupHeightProperty = BindingUtils.Register<DropDownBase, double>(nameof(PopupHeight), double.NaN);
        public static readonly DependencyProperty PopupWidthProperty = BindingUtils.Register<DropDownBase, double>(nameof(PopupWidth), double.NaN);

        public static readonly DependencyProperty HeaderProperty = BindingUtils.Register<DropDownBase, string>(nameof(Header));
        public static readonly DependencyProperty HeaderKeyProperty = BindingUtils.Register<DropDownBase, string>(nameof(HeaderKey));
        public static readonly DependencyProperty ToolTipKeyProperty = BindingUtils.Register<DropDownBase, string>(nameof(ToolTipKey));

        public static readonly DependencyProperty TextConverterProperty = BindingUtils.Register<DropDownBase, StringToResourceConverterBase>(nameof(TextConverter));

        #endregion

        #region Bindable Properties

        [Bindable]
        public Brush OverlayBrush { get; set; }

        [Bindable]
        public PackIconKind? IconKind { get; set; }
        [Bindable]
        public IconPositions IconPosition { get; set; }
        [Bindable]
        public double IconHeight { get; set; }
        [Bindable]
        public double IconWidth { get; set; }

        [Bindable]
        public double PopupHeight { get; set; }
        [Bindable]
        public double PopupWidth { get; set; }

        [Bindable]
        public string Header { get; set; }
        [Bindable]
        public string HeaderKey { get; set; }
        [Bindable]
        public string ToolTipKey { get; set; }

        [Bindable]
        public StringToResourceConverterBase TextConverter { get; set; }
        #endregion

    }
}
