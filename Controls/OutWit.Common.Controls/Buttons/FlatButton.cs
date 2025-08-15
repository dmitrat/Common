using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using OutWit.Common.MVVM.Utils;
using OutWit.Common.Controls.Converters;
using OutWit.Common.Controls.Output;
using OutWit.Common.MVVM.Aspects;

namespace OutWit.Common.Controls.Buttons
{
    public class FlatButton : Button
    {
        #region DependencyProperties

        public static readonly DependencyProperty OverlayBrushProperty = BindingUtils.Register<FlatButton, Brush>(nameof(OverlayBrush), Brushes.White);

        public static readonly DependencyProperty KindProperty = BindingUtils.Register<FlatButton, PackIconKind?>(nameof(Kind));

        public static readonly DependencyProperty IconHeightProperty = BindingUtils.Register<FlatButton, double>(nameof(IconHeight));

        public static readonly DependencyProperty IconWidthProperty = BindingUtils.Register<FlatButton, double>(nameof(IconWidth));

        public static readonly DependencyProperty IconPositionProperty = BindingUtils.Register<FlatButton, IconPositions>(nameof(IconPosition));

        public static readonly DependencyProperty ToolTipKeyProperty = DependencyProperty.Register(nameof(ToolTipKey), typeof(string), typeof(FlatButton), new PropertyMetadata(null, OnToolTipTextChanged));

        public static readonly DependencyProperty HeaderKeyProperty = DependencyProperty.Register(nameof(HeaderKey), typeof(string), typeof(FlatButton), new PropertyMetadata(null, OnHeaderTextChanged));
        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(nameof(Header), typeof(string), typeof(FlatButton), new PropertyMetadata(null));

        public static readonly DependencyProperty TextConverterProperty = DependencyProperty.Register(nameof(TextConverter), typeof(StringToResourceConverterBase), typeof(FlatButton), new PropertyMetadata(null, OnTextConverterChanged));

        public static readonly DependencyProperty IsHiddenProperty = DependencyProperty.Register(nameof(IsHidden), typeof(bool), typeof(FlatButton), new PropertyMetadata(false));

        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(FlatButton), new PropertyMetadata(new CornerRadius(10)));

        public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(nameof(StrokeThickness), typeof(double), typeof(FlatButton), new PropertyMetadata(1.0));

        #endregion

        #region Constructors
        static FlatButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(FlatButton),
                new FrameworkPropertyMetadata(typeof(FlatButton)));
        }

        #endregion

        #region Functions

        private void ResetToolTip()
        {
            if (TextConverter == null || string.IsNullOrEmpty(ToolTipKey))
                return;

            SetBinding(ToolTipProperty,
                new Binding { Converter = TextConverter, ConverterParameter = ToolTipKey });
        }

        private void ResetHeader()
        {
            if (TextConverter == null || string.IsNullOrEmpty(HeaderKey))
                return;

            SetBinding(HeaderProperty,
                new Binding { Converter = TextConverter, ConverterParameter = HeaderKey });
        }

        #endregion

        #region Events Handlers

        private static void OnToolTipTextChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var button = (FlatButton)source;
            button.ResetToolTip();
        }

        private static void OnHeaderTextChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var button = (FlatButton)source;
            button.ResetHeader();
        }

        private static void OnTextConverterChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var button = (FlatButton)source;
            button.ResetToolTip();
            button.ResetHeader();
        }

        #endregion

        #region Properties

        [Bindable]
        public Brush OverlayBrush { get; set; }

        [Bindable]
        public PackIconKind? Kind { get; set; }

        [Bindable]
        public double IconHeight { get; set; }

        [Bindable]
        public double IconWidth { get; set; }

        [Bindable]
        public IconPositions IconPosition { get; set; }

        [Bindable]
        public string ToolTipKey { get; set; }

        [Bindable]
        public string HeaderKey { get; set; }

        [Bindable]
        public string Header { get; set; }

        [Bindable]
        public StringToResourceConverterBase TextConverter { get; set; }

        [Bindable]
        public bool IsHidden { get; set; }

        [Bindable]
        public CornerRadius CornerRadius { get; set; }

        [Bindable]
        public double StrokeThickness { get; set; }
        #endregion
    }
}
