using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using MaterialDesignThemes.Wpf;
using OutWit.Common.Controls.Converters;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Output
{
    public class TextOutput : TextBlock
    {
        #region DependencyProperties
        
        public static readonly DependencyProperty TextKeyProperty = BindingUtils.Register<TextOutput, string>(nameof(TextKey), OnTexKeyChanged);

        public static readonly DependencyProperty TextFormatProperty = BindingUtils.Register<TextOutput, string>(nameof(TextFormat), OnTextFormatChanged);
        public static readonly DependencyProperty TextFormatKeyProperty = BindingUtils.Register<TextOutput, string>(nameof(TextFormatKey), OnTextFormatKeyChanged);

        public static readonly DependencyProperty TextConverterProperty = BindingUtils.Register<TextOutput, StringToResourceConverterBase>(nameof(TextConverter), OnTextConverterChanged);

        #endregion

        #region Constructors

        public TextOutput()
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
            var input = (TextOutput)source;
            input.ResetFormat();
        }

        private static void OnTextFormatChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (TextOutput)source;
            input.ResetText();
        }

        private static void OnTexKeyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (TextOutput)source;
            input.ResetText();
        }

        private static void OnTextConverterChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (TextOutput)source;
            input.ResetFormat();
            input.ResetText();
        }


        #endregion

        #region Properties

        [MVVM.Aspects.Bindable]
        public string TextKey { get; set; }
        
        [MVVM.Aspects.Bindable]
        public string TextFormat { get; set; }

        [MVVM.Aspects.Bindable]
        public string TextFormatKey { get; set; }
        
        [MVVM.Aspects.Bindable]
        public StringToResourceConverterBase TextConverter { get; set; }
        
        #endregion
    }
}
