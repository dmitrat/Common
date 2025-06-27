using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;
using OutWit.Common.Controls.Converters;
using OutWit.Common.Controls.Output;
using OutWit.Common.Controls.Special;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Menu
{
    public class ContextMenuItem : MenuItem
    {
        #region DependencyProperties

        public static readonly DependencyProperty KindProperty = BindingUtils.Register<ContextMenuItem, PackIconKind?>(nameof(Kind));

        public static readonly DependencyProperty IconHeightProperty = BindingUtils.Register<ContextMenuItem, double>(nameof(IconHeight));

        public static readonly DependencyProperty IconWidthProperty = BindingUtils.Register<ContextMenuItem, double>(nameof(IconWidth));

        public static readonly DependencyProperty IconPositionProperty = BindingUtils.Register<ContextMenuItem, IconPositions>(nameof(IconPosition));

        public static readonly DependencyProperty TextProperty = BindingUtils.Register<ContextMenuItem, string>(nameof(Text));

        public static readonly DependencyProperty TextKeyProperty = BindingUtils.Register<ContextMenuItem, string>(nameof(TextKey));

        public static readonly DependencyProperty ToolTipKeyProperty = BindingUtils.Register<ContextMenuItem, string>(nameof(ToolTipKey));

        public static readonly DependencyProperty TextFormatProperty = BindingUtils.Register<ContextMenuItem, string>(nameof(TextFormat));

        public static readonly DependencyProperty TextFormatKeyProperty = BindingUtils.Register<ContextMenuItem, string>(nameof(TextFormatKey));

        public static readonly DependencyProperty TextConverterProperty = BindingUtils.Register<ContextMenuItem, StringToResourceConverterBase>(nameof(TextConverter));

        #endregion

        #region Constructors

        static ContextMenuItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ContextMenuItem),
                new FrameworkPropertyMetadata(typeof(ContextMenuItem)));
        }

        #endregion

        #region Properties

        [Bindable]
        public PackIconKind? Kind { get; set; }

        [Bindable]
        public double IconHeight { get; set; }

        [Bindable]
        public double IconWidth { get; set; }

        [Bindable]
        public IconPositions IconPosition { get; set; }

        [Bindable]
        public string Text { get; set; }

        [Bindable]
        public string TextKey { get; set; }

        [Bindable]
        public string ToolTipKey { get; set; }

        [Bindable]
        public string TextFormat { get; set; }

        [Bindable]
        public string TextFormatKey { get; set; }

        [Bindable]
        public StringToResourceConverterBase TextConverter { get; set; }
        #endregion
    }
}
