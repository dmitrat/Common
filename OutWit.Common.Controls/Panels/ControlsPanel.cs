using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using MaterialDesignThemes.Wpf;
using OutWit.Common.Controls.Converters;
using OutWit.Common.Controls.Input;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Panels
{
    public class ControlsPanel : ItemsControl
    {
        #region DependencyProperties

        public static readonly DependencyProperty ImageKindProperty = BindingUtils.Register<ControlsPanel, PackIconKind>(nameof(ImageKind));
        public static readonly DependencyProperty ImageSizeProperty = BindingUtils.Register<ControlsPanel, double>(nameof(ImageSize));
        public static readonly DependencyProperty ShowImageProperty = BindingUtils.Register<ControlsPanel, bool>(nameof(ShowImage));

        public static readonly DependencyProperty HeaderKeyProperty = BindingUtils.Register<ControlsPanel, string>(nameof(HeaderKey), OnHeaderTextChanged);
        public static readonly DependencyProperty HeaderProperty = BindingUtils.Register<ControlsPanel, string>(nameof(Header));
        public static readonly DependencyProperty HeaderScaleProperty = BindingUtils.Register<ControlsPanel, double>(nameof(HeaderScale), 0.5);

        public static readonly DependencyProperty TextWrappingProperty = BindingUtils.Register<ControlsPanel, TextWrapping>(nameof(TextWrapping), TextWrapping.NoWrap);

        public static readonly DependencyProperty IsReadOnlyProperty = BindingUtils.Register<ControlsPanel, bool>(nameof(IsReadOnly));

        public static readonly DependencyProperty TextConverterProperty = BindingUtils.Register<ControlsPanel, StringToResourceConverterBase>(nameof(TextConverter), OnTextConverterChanged);

        #endregion

        #region Constructors

        static ControlsPanel()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ControlsPanel),
                new FrameworkPropertyMetadata(typeof(ControlsPanel)));
        }

        #endregion

        #region Functions

        private void ResetHeader()
        {
            if (TextConverter == null || string.IsNullOrEmpty(HeaderKey))
                return;

            SetBinding(HeaderProperty,
                new Binding { Converter = TextConverter, ConverterParameter = HeaderKey });
        }

        #endregion

        #region Events Handlers

        private static void OnHeaderTextChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (ControlsPanel)source;
            input.ResetHeader();
        }

        private static void OnTextConverterChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (ControlsPanel)source;
            input.ResetHeader();
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
}
