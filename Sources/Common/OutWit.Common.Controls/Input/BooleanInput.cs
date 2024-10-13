using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Commands;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Input
{
    public class BooleanInput: InputBase<bool>
    {
        #region DependencyProperties

        public static readonly DependencyProperty TrueTextKeyProperty = BindingUtils.Register<BooleanInput, string>(nameof(TrueTextKey), OnTrueTextKeyChanged);
        public static readonly DependencyProperty TrueTextProperty = BindingUtils.Register<BooleanInput, string>(nameof(TrueText));

        public static readonly DependencyProperty FalseTextKeyProperty = BindingUtils.Register<BooleanInput, string>(nameof(FalseTextKey), OnFalseTextKeyChanged);
        public static readonly DependencyProperty FalseTextProperty = BindingUtils.Register<BooleanInput, string>(nameof(FalseText));

        public static readonly DependencyProperty OptionsDistanceProperty = BindingUtils.Register<BooleanInput, GridLength>(nameof(OptionsDistance));
        public static readonly DependencyProperty SuffixLengthProperty = BindingUtils.Register<BooleanInput, GridLength>(nameof(SuffixLength));

        public static readonly DependencyProperty HeaderFontSizeProperty = BindingUtils.Register<BooleanInput, double>(nameof(HeaderFontSize));

        public static readonly DependencyProperty TrueSelectedCommandProperty = BindingUtils.Register<BooleanInput, ICommand>(nameof(TrueSelectedCommand));
        public static readonly DependencyProperty FalseSelectedCommandProperty = BindingUtils.Register<BooleanInput, ICommand>(nameof(FalseSelectedCommand));

        #endregion

        #region Constructors

        static BooleanInput()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(BooleanInput),
                new FrameworkPropertyMetadata(typeof(BooleanInput)));
        }

        public BooleanInput()
        {
            InitDefaults();
            InitCommands();
        }

        #endregion

        #region Initialization

        private void InitDefaults()
        {
            HeaderFontSize = FontSize * HeaderScale;
        }

        private void InitCommands()
        {
            TrueSelectedCommand = new DelegateCommand(x => SelectTrueGender((bool)x));
            FalseSelectedCommand = new DelegateCommand(x => SelectFalseGender((bool)x));
        }

        #endregion

        #region Functions

        private void SelectTrueGender(bool isChecked)
        {
            if (isChecked)
                Value = true;
            else
                Value = false;
        }

        private void SelectFalseGender(bool isChecked)
        {
            if (isChecked)
                Value = false;
            else
                Value = true;
        }

        private void ResetTrueText()
        {
            if (TextConverter == null || string.IsNullOrEmpty(TrueTextKey))
                return;

            SetBinding(TrueTextProperty, new Binding { Converter = TextConverter, ConverterParameter = TrueTextKey });
        }

        private void ResetFalseText()
        {
            if (TextConverter == null || string.IsNullOrEmpty(FalseTextKey))
                return;

            SetBinding(FalseTextProperty, new Binding { Converter = TextConverter, ConverterParameter = FalseTextKey });
        }

        #endregion

        #region Events Handlers

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.IsProperty((BooleanInput i) => i.TextConverter))
            {
                ResetTrueText();
                ResetFalseText();
            }

            else if (e.IsProperty((BooleanInput i) => i.FontSize))
                HeaderFontSize = FontSize * HeaderScale;

            else if (e.IsProperty((BooleanInput i) => i.HeaderScale))
                HeaderFontSize = FontSize * HeaderScale;
        }

        private static void OnTrueTextKeyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (BooleanInput)source;
            input.ResetTrueText();
        }

        private static void OnFalseTextKeyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (BooleanInput)source;
            input.ResetFalseText();
        }
        #endregion

        #region Properties

        [Bindable]
        public GridLength OptionsDistance { get; set; }

        [Bindable]
        public GridLength SuffixLength { get; set; }

        [Bindable]
        public string TrueTextKey { get; set; }

        [Bindable]
        public string TrueText { get; set; }

        [Bindable]
        public string FalseTextKey { get; set; }

        [Bindable]
        public string FalseText { get; set; }

        [Bindable]
        public double HeaderFontSize { get; set; }

        [Bindable]
        public ICommand TrueSelectedCommand { get; set; }

        [Bindable]
        public ICommand FalseSelectedCommand { get; set; }

        #endregion
    }
}
