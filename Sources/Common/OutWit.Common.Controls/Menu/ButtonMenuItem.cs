using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using MaterialDesignThemes.Wpf;
using OutWit.Common.Controls.Buttons;
using OutWit.Common.Controls.Converters;
using OutWit.Common.Controls.Output;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Menu
{
    public class ButtonMenuItem : MenuItem
    {
        #region DependencyProperties

        public static readonly DependencyProperty KindProperty = BindingUtils.Register<ButtonMenuItem, PackIconKind?>(nameof(Kind));

        public static readonly DependencyProperty IconHeightProperty = BindingUtils.Register<ButtonMenuItem, double>(nameof(IconHeight));

        public static readonly DependencyProperty IconWidthProperty = BindingUtils.Register<ButtonMenuItem, double>(nameof(IconWidth));

        public static readonly DependencyProperty IconPositionProperty = BindingUtils.Register<ButtonMenuItem, IconPositions>(nameof(IconPosition));

        public static readonly DependencyProperty TextProperty = BindingUtils.Register<ButtonMenuItem, string>(nameof(Text));

        public static readonly DependencyProperty TextKeyProperty = BindingUtils.Register<ButtonMenuItem, string>(nameof(TextKey), OnTexKeyChanged);

        public static readonly DependencyProperty TextFormatProperty = BindingUtils.Register<ButtonMenuItem, string>(nameof(TextFormat), OnTextFormatChanged);
        public static readonly DependencyProperty TextFormatKeyProperty = BindingUtils.Register<ButtonMenuItem, string>(nameof(TextFormatKey), OnTextFormatKeyChanged);

        public static readonly DependencyProperty TextConverterProperty = BindingUtils.Register<ButtonMenuItem, StringToResourceConverterBase>(nameof(TextConverter), OnTextConverterChanged);

        #endregion

        #region Constructors
        static ButtonMenuItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ButtonMenuItem),
                new FrameworkPropertyMetadata(typeof(ButtonMenuItem)));
        }


        public ButtonMenuItem()
        {

        }

        #endregion

        #region Functions

        private void ResetText()
        {
            if (TextConverter == null || string.IsNullOrEmpty(TextKey))
                return;

            SetBinding(TextProperty, this.CreateBinding(x => x.TextKey, new FormatConverter(TextConverter), TextFormat));
        }

        private void ResetFormat()
        {
            if (TextConverter == null || string.IsNullOrEmpty(TextFormatKey))
                return;

            SetBinding(TextFormatProperty, this.CreateBinding(x => x.TextFormatKey, TextConverter));
        }

        #endregion

        #region Events Handlers

        private static void OnTextFormatKeyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (ButtonMenuItem)source;
            input.ResetFormat();
        }

        private static void OnTextFormatChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (ButtonMenuItem)source;
            input.ResetText();
        }

        private static void OnTexKeyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (ButtonMenuItem)source;
            input.ResetText();
        }

        private static void OnTextConverterChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (ButtonMenuItem)source;
            input.ResetFormat();
            input.ResetText();
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
        public string TextFormat { get; set; }

        [Bindable]
        public string TextFormatKey { get; set; }

        [Bindable]
        public StringToResourceConverterBase TextConverter { get; set; }
        #endregion
    }
}
