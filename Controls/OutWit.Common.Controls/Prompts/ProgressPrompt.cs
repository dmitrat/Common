using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OutWit.Common.Controls.Converters;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Commands;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Prompts
{
    public class ProgressPrompt : ProgressBar
    {
        #region DependencyProperties

        public static readonly DependencyProperty HeaderProperty = BindingUtils.Register<ProgressPrompt, string>(nameof(Header));
        public static readonly DependencyProperty HeaderKeyProperty = BindingUtils.Register<ProgressPrompt, string>(nameof(HeaderKey), OnHeaderKeyChanged);

        public static readonly DependencyProperty ButtonTextProperty = BindingUtils.Register<ProgressPrompt, string>(nameof(ButtonText));
        public static readonly DependencyProperty ButtonTextKeyProperty = BindingUtils.Register<ProgressPrompt, string>(nameof(ButtonTextKey), OnButtonTextKeyChanged);
        public static readonly DependencyProperty IsButtonVisibleProperty = BindingUtils.Register<ProgressPrompt, bool>(nameof(IsButtonVisible));

        public static readonly DependencyProperty IsCancelledProperty = BindingUtils.Register<ProgressPrompt, bool>(nameof(IsCancelled));
        public static readonly DependencyProperty CancelCommandProperty = BindingUtils.Register<ProgressPrompt, ICommand>(nameof(CancelCommand));

        public static readonly DependencyProperty TextConverterProperty = BindingUtils.Register<ProgressPrompt, StringToResourceConverterBase>(nameof(TextConverter), OnTextConverterChanged);

        #endregion

        #region Constructors

        static ProgressPrompt()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ProgressPrompt),
                new FrameworkPropertyMetadata(typeof(ProgressPrompt)));
        }

        public ProgressPrompt()
        {
            InitCommands();
        }

        #endregion

        #region Initialization

        private void InitCommands()
        {
            CancelCommand = new DelegateCommand(x=>Cancel());
        }

        #endregion

        #region Functions

        private void Cancel()
        {
            IsCancelled = true;
        }

        private void ResetHeader()
        {
            if (TextConverter == null || string.IsNullOrEmpty(HeaderKey))
                return;

            SetBinding(HeaderProperty, this.CreateBinding(x => x.HeaderKey, TextConverter));
        }

        private void ResetButtonText()
        {
            if (TextConverter == null || string.IsNullOrEmpty(ButtonTextKey))
                return;

            SetBinding(ButtonTextProperty, this.CreateBinding(x => x.ButtonTextKey, TextConverter));
        }

        #endregion

        #region Events Handlers

        private static void OnHeaderKeyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (ProgressPrompt)source;
            input.ResetHeader();
        }

        private static void OnButtonTextKeyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (ProgressPrompt)source;
            input.ResetButtonText();
        }

        private static void OnTextConverterChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (ProgressPrompt)source;
            input.ResetHeader();
            input.ResetButtonText();
        }

        #endregion

        #region Properties

        [Bindable]
        public string Header { get; set; }

        [Bindable]
        public string HeaderKey { get; set; }

        [Bindable]
        public string ButtonText { get; set; }

        [Bindable]
        public string ButtonTextKey { get; set; }

        [Bindable]
        public bool IsButtonVisible { get; set; }

        [Bindable]
        public bool IsCancelled { get; private set; }

        [Bindable]
        public ICommand CancelCommand { get; private set; }

        [Bindable]
        public StringToResourceConverterBase TextConverter { get; set; }
        #endregion
    }
}
