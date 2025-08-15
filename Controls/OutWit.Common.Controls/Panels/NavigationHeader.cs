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

namespace OutWit.Common.Controls.Panels
{
    public class NavigationHeader : ItemsControl
    {
        #region DependencyProperties

        public static readonly DependencyProperty OverlayBrushProperty = BindingUtils.Register<NavigationHeader, Brush>(nameof(OverlayBrush), Brushes.White);

        public static readonly DependencyProperty ImageProperty = BindingUtils.Register<NavigationHeader, Object>(nameof(Image));

        public static readonly DependencyProperty HeaderKeyProperty = BindingUtils.Register<NavigationHeader, string>(nameof(HeaderKey), null, OnHeaderTextChanged);
        public static readonly DependencyProperty HeaderProperty = BindingUtils.Register<NavigationHeader, string>(nameof(Header));

        public static readonly DependencyProperty TextConverterProperty = BindingUtils.Register<NavigationHeader, StringToResourceConverterBase>(nameof(TextConverter), null, OnTextConverterChanged);

        public static readonly DependencyProperty IsSelectedProperty = BindingUtils.Register<NavigationHeader, bool>(nameof(IsSelected));

        #endregion

        #region Constructors
        static NavigationHeader()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NavigationHeader),
                new FrameworkPropertyMetadata(typeof(NavigationHeader)));
        }

        public NavigationHeader()
        {
        }

        #endregion

        #region Functons

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
            var navigationHeader = (NavigationHeader)source;
            navigationHeader.ResetHeader();
        }

        private static void OnTextConverterChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var navigationHeader = (NavigationHeader)source;
            navigationHeader.ResetHeader();
        }

        #endregion

        #region Properties

        [Bindable]
        public Brush OverlayBrush { get; set; }

        [Bindable]
        public string Header { get; set; }

        [Bindable]
        public string HeaderKey { get; set; }

        [Bindable]
        public object Image { get; set; }

        [Bindable]
        public StringToResourceConverterBase TextConverter { get; set; }

        [Bindable]
        public bool IsSelected { get; set; }

        #endregion
    }
}
