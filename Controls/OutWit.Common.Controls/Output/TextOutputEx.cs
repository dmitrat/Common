using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using MaterialDesignThemes.Wpf;
using OutWit.Common.Aspects;
using OutWit.Common.Controls.Converters;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Output
{
    public class TextOutputEx : Control
    {
        #region DependencyProperties
        
        public static readonly DependencyProperty TextProperty = BindingUtils.Register<TextOutputEx, string>(nameof(Text));
        public static readonly DependencyProperty TextKeyProperty = BindingUtils.Register<TextOutputEx, string>(nameof(TextKey), OnTexKeyChanged);

        public static readonly DependencyProperty TextFormatProperty = BindingUtils.Register<TextOutputEx, string>(nameof(TextFormat), OnTextFormatChanged);
        public static readonly DependencyProperty TextFormatKeyProperty = BindingUtils.Register<TextOutputEx, string>(nameof(TextFormatKey), OnTextFormatKeyChanged);

        public static readonly DependencyProperty EmptyTextProperty = BindingUtils.Register<TextOutputEx, string>(nameof(EmptyText));
        public static readonly DependencyProperty EmptyTextKeyProperty = BindingUtils.Register<TextOutputEx, string>(nameof(EmptyTextKey), OnEmptyTextKeyChanged);

        public static readonly DependencyProperty TextWrappingProperty = BindingUtils.Register<TextOutputEx, TextWrapping>(nameof(TextWrapping));
        public static readonly DependencyProperty TextAlignmentProperty = BindingUtils.Register<TextOutputEx, TextAlignment>(nameof(TextAlignment));

        public static readonly DependencyProperty TextConverterProperty = BindingUtils.Register<TextOutputEx, StringToResourceConverterBase>(nameof(TextConverter), OnTextConverterChanged);

        #endregion

        #region Constructors

        static TextOutputEx()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TextOutputEx),
                new FrameworkPropertyMetadata(typeof(TextOutputEx)));
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

        private void ResetEmptyText()
        {
            if (TextConverter == null || string.IsNullOrEmpty(EmptyTextKey))
                return;

            SetBinding(EmptyTextProperty, new Binding { Converter = TextConverter, ConverterParameter = EmptyTextKey });
        }

        #endregion

        #region Events Handlers

        private static void OnTextFormatKeyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (TextOutputEx)source;
            input.ResetFormat();
        }

        private static void OnTextFormatChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (TextOutputEx)source;
            input.ResetText();
        }

        private static void OnTexKeyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (TextOutputEx)source;
            input.ResetText();
        }

        private static void OnEmptyTextKeyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var editor = (TextOutputEx)source;
            editor.ResetEmptyText();
        }

        private static void OnTextConverterChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (TextOutputEx)source;
            input.ResetFormat();
            input.ResetText();
            input.ResetEmptyText();
        }


        #endregion

        #region Properties

        [MVVM.Aspects.Bindable]
        public string Text { get; set; }

        [MVVM.Aspects.Bindable]
        public string TextKey { get; set; }

        [MVVM.Aspects.Bindable]
        public string TextFormat { get; set; }


        [MVVM.Aspects.Bindable]
        public string TextFormatKey { get; set; }

        [MVVM.Aspects.Bindable]
        public string EmptyText { get; set; }

        [MVVM.Aspects.Bindable]
        public string EmptyTextKey { get; set; }

        [MVVM.Aspects.Bindable]
        public TextWrapping TextWrapping { get; set; }

        [MVVM.Aspects.Bindable]
        public TextAlignment TextAlignment { get; set; }
        
        [MVVM.Aspects.Bindable]
        public StringToResourceConverterBase TextConverter { get; set; }
        
        #endregion
    }
}
