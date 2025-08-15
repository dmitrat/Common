using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using MaterialDesignThemes.Wpf;
using OutWit.Common.Controls.Converters;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Buttons
{
    public class ToolBarToggleButton : ToggleButton
    {
        #region DependencyProperties
        
        public static readonly DependencyProperty KindProperty = BindingUtils.Register<ToolBarToggleButton, PackIconKind?>(nameof(Kind), OnKindChanged);

        public static readonly DependencyProperty IconHeightProperty = BindingUtils.Register<ToolBarToggleButton, double>(nameof(IconHeight));

        public static readonly DependencyProperty IconWidthProperty = BindingUtils.Register<ToolBarToggleButton, double>(nameof(IconWidth));

        public static readonly DependencyProperty ToolTipKeyProperty = BindingUtils.Register<ToolBarToggleButton, string>(nameof(ToolTipKey), OnToolTipTextChanged);

        public static readonly DependencyProperty TextConverterProperty = BindingUtils.Register<ToolBarToggleButton, StringToResourceConverterBase>(nameof(TextConverter), OnTextConverterChanged);

        #endregion

        #region Constructors

        static ToolBarToggleButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ToolBarToggleButton),
                new FrameworkPropertyMetadata(typeof(ToolBarToggleButton)));
        }

        public ToolBarToggleButton()
        {

        }

        #endregion

        #region Functions

        private void ResetToolTip()
        {
            if (TextConverter == null || string.IsNullOrEmpty(ToolTipKey))
                return;

            SetBinding(ToolTipProperty, new Binding { Converter = TextConverter, ConverterParameter = ToolTipKey });
        }

        private void ResetContent()
        {
            if (Kind == null)
                Content = null;
            else
                Content = new PackIcon {Kind = Kind.Value};

        }

        #endregion

        #region Events Handlers

        private static void OnToolTipTextChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var button = (ToolBarToggleButton)source;
            button.ResetToolTip();
        }

        private static void OnTextConverterChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var button = (ToolBarToggleButton)source;
            button.ResetToolTip();
        }

        private static void OnKindChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            //var button = (ToolBarButton)source;
            //button.ResetContent();
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
        public string ToolTipKey { get; set; }

        [Bindable]
        public StringToResourceConverterBase TextConverter { get; set; }

        #endregion
    }
}
