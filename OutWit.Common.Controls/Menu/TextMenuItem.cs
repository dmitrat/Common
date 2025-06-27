using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;
using OutWit.Common.Controls.Converters;
using OutWit.Common.Controls.Output;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Menu
{
    public class TextMenuItem : MenuItem
    {
        #region DependencyProperties
        
        public static readonly DependencyProperty TextKeyProperty = BindingUtils.Register<TextMenuItem, string>(nameof(TextKey), OnTexKeyChanged);

        public static readonly DependencyProperty TextFormatProperty = BindingUtils.Register<TextMenuItem, string>(nameof(TextFormat), OnTextFormatChanged);
        public static readonly DependencyProperty TextFormatKeyProperty = BindingUtils.Register<TextMenuItem, string>(nameof(TextFormatKey), OnTextFormatKeyChanged);

        public static readonly DependencyProperty KindProperty = BindingUtils.Register<TextMenuItem, PackIconKind?>(nameof(Kind), null, OnKindChanged);

        public static readonly DependencyProperty TextConverterProperty = BindingUtils.Register<TextMenuItem, StringToResourceConverterBase>(nameof(TextConverter), OnTextConverterChanged);

        #endregion

        #region Constructors

        public TextMenuItem()
        {
        }

        #endregion

        #region Functions

        private void ResetIcon()
        {
            Icon = Kind != null ? new PackIcon { Kind = Kind.Value } : null;
        }

        private void ResetText()
        {
            if (TextConverter == null || string.IsNullOrEmpty(TextKey))
                return;

            SetBinding(HeaderProperty, this.CreateBinding(x => x.TextKey, new FormatConverter(TextConverter), TextFormat));
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
            var input = (TextMenuItem)source;
            input.ResetFormat();
        }

        private static void OnTextFormatChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (TextMenuItem)source;
            input.ResetText();
        }

        private static void OnTexKeyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (TextMenuItem)source;
            input.ResetText();
        }

        private static void OnKindChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (TextMenuItem)source;
            input.ResetIcon();
        }

        private static void OnTextConverterChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (TextMenuItem)source;
            input.ResetFormat();
            input.ResetText();
        }


        #endregion

        #region Properties

        [Bindable]
        public string TextKey { get; set; }

        [Bindable]
        public string TextFormat { get; set; }

        [Bindable]
        public string TextFormatKey { get; set; }

        [Bindable]
        public PackIconKind? Kind { get; set; }

        [Bindable]
        public StringToResourceConverterBase TextConverter { get; set; }
        #endregion
    }
}
