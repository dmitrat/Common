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
    public class ToolBarRepeatButton : RepeatButton
    {
        #region DependencyProperties
        
        public static readonly DependencyProperty KindProperty = BindingUtils.Register<ToolBarRepeatButton, PackIconKind?>(nameof(Kind), OnKindChanged);

        public static readonly DependencyProperty IconHeightProperty = BindingUtils.Register<ToolBarRepeatButton, double>(nameof(IconHeight));

        public static readonly DependencyProperty IconWidthProperty = BindingUtils.Register<ToolBarRepeatButton, double>(nameof(IconWidth));

        public static readonly DependencyProperty ToolTipKeyProperty = BindingUtils.Register<ToolBarRepeatButton, string>(nameof(ToolTipKey), OnToolTipTextChanged);

        public static readonly DependencyProperty TextConverterProperty = BindingUtils.Register<ToolBarRepeatButton, StringToResourceConverterBase>(nameof(TextConverter), OnTextConverterChanged);

        #endregion

        #region Constructors

        static ToolBarRepeatButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ToolBarRepeatButton),
                new FrameworkPropertyMetadata(typeof(ToolBarRepeatButton)));
        }

        public ToolBarRepeatButton()
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
            var button = (ToolBarRepeatButton)source;
            button.ResetToolTip();
        }

        private static void OnTextConverterChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var button = (ToolBarRepeatButton)source;
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
