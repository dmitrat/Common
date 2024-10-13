using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using OutWit.Common.Controls.Converters;
using OutWit.Common.Controls.Output;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Buttons
{
    public class ImageButton : Button
    {
        #region DependencyProperties

        public static readonly DependencyProperty KindProperty = BindingUtils.Register<ImageButton, PackIconKind?>(nameof(Kind));

        public static readonly DependencyProperty IconHeightProperty = BindingUtils.Register<ImageButton, double>(nameof(IconHeight));

        public static readonly DependencyProperty IconWidthProperty = BindingUtils.Register<ImageButton, double>(nameof(IconWidth)); 

        public static readonly DependencyProperty ForegroundDisabledProperty = BindingUtils.Register<ImageButton, Brush>(nameof(ForegroundDisabled), OnForegroundDisabledChanged);
        public static readonly DependencyProperty ForegroundEnabledProperty = BindingUtils.Register<ImageButton, Brush>(nameof(ForegroundEnabled), OnForegroundEnabledChanged);

        public static readonly DependencyProperty ForegroundSelectedProperty = BindingUtils.Register<ImageButton, Brush>(nameof(ForegroundSelected));


        #endregion

        #region Constructors

        static ImageButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ImageButton),
                new FrameworkPropertyMetadata(typeof(ImageButton)));
        }

        public ImageButton()
        {
            IntEvents();
        }

        #endregion

        #region Initialization

        private void InitDefaults()
        {
            Foreground = IsEnabled ? ForegroundEnabled : ForegroundDisabled;
        }

        private void IntEvents()
        {

            this.IsEnabledChanged += OnEnabledChanged;

        }

        #endregion

        #region EventHandlers

        private void OnEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            InitDefaults();
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            Foreground = ForegroundSelected;
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            Foreground = ForegroundEnabled;
            base.OnMouseLeave(e);
        }

        private static void OnForegroundDisabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var button = (ImageButton)d;
            button.InitDefaults();
        }

        private static void OnForegroundEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var button = (ImageButton)d;
            button.InitDefaults();
        }

        #endregion

        #region Properties

        [Bindable]
        public PackIconKind? Kind { get; set; }

        [Bindable]
        public double IconHeight { get; set; }

        [Bindable]
        public double IconWidth { get; set; }

        [Bindable]
        public Brush ForegroundDisabled { get; set; }

        [Bindable]
        public Brush ForegroundEnabled { get; set; }

        [Bindable]
        public Brush ForegroundSelected { get; set; }
        #endregion
    }
}
