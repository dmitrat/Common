using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using OutWit.Common.Controls.Converters;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Buttons
{
    public class NavigationButton : Button
    {
        #region DependencyProperties

        public static readonly DependencyProperty OverlayBrushProperty = BindingUtils.Register<NavigationButton, Brush>(nameof(OverlayBrush), Brushes.White);

        public static readonly DependencyProperty IsSelectedProperty = BindingUtils.Register<NavigationButton, bool>(nameof(IsSelected));

        public static readonly DependencyProperty HeaderKeyProperty = BindingUtils.Register<NavigationButton, string>(nameof(HeaderKey), null, OnHeaderTextChanged);
        public static readonly DependencyProperty HeaderProperty = BindingUtils.Register<NavigationButton, string>(nameof(Header));

        public static readonly DependencyProperty TextConverterProperty = BindingUtils.Register<NavigationButton, StringToResourceConverterBase>(nameof(TextConverter), null, OnTextConverterChanged);

        #endregion

        #region Constructors
        static NavigationButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NavigationButton),
                new FrameworkPropertyMetadata(typeof(NavigationButton)));
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

        #region Event Handlers
        private static void OnHeaderTextChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var button = (NavigationButton)source;
            button.ResetHeader();
        }

        private static void OnTextConverterChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var button = (NavigationButton)source;
            button.ResetHeader();
        }
        #endregion

        #region Properties

        [Bindable]
        public Brush OverlayBrush { get; set; }

        [Bindable]
        public bool IsSelected { get; set; }

        [Bindable]
        public string Header { get; set; }

        [Bindable]
        public string HeaderKey { get; set; }

        [Bindable]
        public StringToResourceConverterBase TextConverter { get; set; }

        #endregion
    }
}
