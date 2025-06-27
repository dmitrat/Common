using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using OutWit.Common.Controls.Converters;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Prompts
{
    public class ProcessingPrompt: Control
    {
        #region DependencyProperties
        
        public static readonly DependencyProperty HeaderProperty = BindingUtils.Register<ProcessingPrompt, string>(nameof(Header));
        public static readonly DependencyProperty HeaderKeyProperty = BindingUtils.Register<ProcessingPrompt, string>(nameof(HeaderKey), OnHeaderKeyChanged);

        public static readonly DependencyProperty TextConverterProperty = BindingUtils.Register<ProcessingPrompt, StringToResourceConverterBase>(nameof(TextConverter), OnTextConverterChanged);

        #endregion

        #region Constructors

        static ProcessingPrompt()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ProcessingPrompt),
                new FrameworkPropertyMetadata(typeof(ProcessingPrompt)));
        }

        public ProcessingPrompt()
        {
        }

        #endregion

        #region Functions

        private void ResetHeader()
        {
            if (TextConverter == null || string.IsNullOrEmpty(HeaderKey))
                return;

            SetBinding(HeaderProperty, this.CreateBinding(x => x.HeaderKey, TextConverter));
        }

        #endregion

        #region Events Handlers

        private static void OnHeaderKeyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (ProcessingPrompt)source;
            input.ResetHeader();
        }

        private static void OnTextConverterChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (ProcessingPrompt)source;
            input.ResetHeader();
        }

        #endregion

        #region Properties

        [Bindable]
        public string Header { get; set; }

        [Bindable]
        public string HeaderKey { get; set; }

        [Bindable]
        public StringToResourceConverterBase TextConverter { get; set; }
        #endregion
    }
}
