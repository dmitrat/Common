using System;
using System.Windows;
using System.Windows.Data;
using MaterialDesignThemes.Wpf;
using OutWit.Common.Controls.Converters;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Utils;
using Binding = System.Windows.Data.Binding;

namespace OutWit.Common.Controls.Input
{
    public abstract class InputBase : System.Windows.Controls.Control
    {
        #region DependencyProperties

        public static readonly DependencyProperty ImageKindProperty = BindingUtils.Register<InputBase, PackIconKind>(nameof(ImageKind));
        public static readonly DependencyProperty ImageSizeProperty = BindingUtils.Register<InputBase, double>(nameof(ImageSize));
        public static readonly DependencyProperty ShowImageProperty = BindingUtils.Register<InputBase, bool>(nameof(ShowImage));

        public static readonly DependencyProperty SuffixKeyProperty = BindingUtils.Register<InputBase, string>(nameof(SuffixKey), OnSuffixTextChanged);
        public static readonly DependencyProperty SuffixProperty = BindingUtils.Register<InputBase, string>(nameof(Suffix));

        public static readonly DependencyProperty PrefixKeyProperty = BindingUtils.Register<InputBase, string>(nameof(PrefixKey), OnPrefixTextChanged);
        public static readonly DependencyProperty PrefixProperty = BindingUtils.Register<InputBase, string>(nameof(Prefix));

        public static readonly DependencyProperty HeaderKeyProperty = BindingUtils.Register<InputBase, string>(nameof(HeaderKey), OnHeaderTextChanged);
        public static readonly DependencyProperty HeaderProperty = BindingUtils.Register<InputBase, string>(nameof(Header));
        public static readonly DependencyProperty HeaderScaleProperty = BindingUtils.Register<InputBase, double>(nameof(HeaderScale), 0.5);

        public static readonly DependencyProperty TextWrappingProperty = BindingUtils.Register<InputBase, TextWrapping>(nameof(TextWrapping), TextWrapping.NoWrap);

        public static readonly DependencyProperty IsReadOnlyProperty = BindingUtils.Register<InputBase, bool>(nameof(IsReadOnly));

        public static readonly DependencyProperty TextConverterProperty = BindingUtils.Register<InputBase, StringToResourceConverterBase>(nameof(TextConverter), OnTextConverterChanged);

        #endregion

        #region Functions

        private void ResetHeader()
        {
            if (TextConverter == null || string.IsNullOrEmpty(HeaderKey))
                return;

            SetBinding(HeaderProperty,
                new Binding { Converter = TextConverter, ConverterParameter = HeaderKey });
        }

        private void ResetSuffix()
        {
            if (TextConverter == null || string.IsNullOrEmpty(SuffixKey))
                return;

            SetBinding(SuffixProperty,
                new Binding { Converter = TextConverter, ConverterParameter = SuffixKey });
        }

        private void ResetPrefix()
        {
            if (TextConverter == null || string.IsNullOrEmpty(PrefixKey))
                return;

            SetBinding(PrefixProperty,
                new Binding { Converter = TextConverter, ConverterParameter = PrefixKey });
        }

        #endregion

        #region Events Handlers

        private static void OnHeaderTextChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (InputBase)source;
            input.ResetHeader();
        }

        private static void OnPrefixTextChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (InputBase)source;
            input.ResetPrefix();
        }

        private static void OnSuffixTextChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (InputBase)source;
            input.ResetSuffix();
        }

        private static void OnTextConverterChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (InputBase)source;
            input.ResetHeader();
            input.ResetPrefix();
            input.ResetSuffix();
        }

        #endregion

        #region Properties

        [Bindable]
        public PackIconKind ImageKind { get; set; }

        [Bindable]
        public double ImageSize { get; set; }

        [Bindable]
        public bool ShowImage { get; set; }

        [Bindable]
        public string PrefixKey { get; set; }

        [Bindable]
        public string Prefix { get; set; }

        [Bindable]
        public string SuffixKey { get; set; }

        [Bindable]
        public string Suffix { get; set; }

        [Bindable]
        public string HeaderKey { get; set; }

        [Bindable]
        public string Header { get; set; }

        [Bindable]
        public double HeaderScale { get; set; }

        [Bindable]
        public TextWrapping TextWrapping { get; set; }

        [Bindable]
        public bool IsReadOnly { get; set; }

        [Bindable]
        public StringToResourceConverterBase TextConverter { get; set; }

        #endregion
    }

    public abstract class InputBase<TValue> : InputBase
    {
        #region Events

        public event InputBaseEventHandler<TValue> ValueChanged = delegate { };

        #endregion

        #region DependencyProperties

        public static readonly DependencyProperty ValueProperty = BindingUtils.Register<InputBase<TValue>, TValue>(nameof(Value), OnValueChanged);

        #endregion

        private void ReportValueChanged()
        {
            ValueChanged(this);
        }

        #region EventHandlers

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var input = (InputBase<TValue>)d;
            input.ReportValueChanged();
        } 

        #endregion

        #region Properties

        public TValue Value
        {
            get => (TValue)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        #endregion
    }

    public delegate void InputBaseEventHandler<TValue>(InputBase<TValue> sender);
}
