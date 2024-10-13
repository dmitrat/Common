using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using OutWit.Common.Controls.Converters;
using OutWit.Common.Controls.Output;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Buttons
{
    public class ImageRepeatButton : RepeatButton
    {
        #region DependencyProperties

        public static readonly DependencyProperty KindProperty = BindingUtils.Register<ImageRepeatButton, PackIconKind?>(nameof(Kind));

        public static readonly DependencyProperty IconHeightProperty = BindingUtils.Register<ImageRepeatButton, double>(nameof(IconHeight));
        public static readonly DependencyProperty IconWidthProperty = BindingUtils.Register<ImageRepeatButton, double>(nameof(IconWidth));
        public static readonly DependencyProperty IconMarginProperty = BindingUtils.Register<ImageRepeatButton, Thickness>(nameof(IconMargin));

        public static readonly DependencyProperty ForegroundDisabledProperty = BindingUtils.Register<ImageRepeatButton, Brush>(nameof(ForegroundDisabled), OnForegroundDisabledChanged);
        public static readonly DependencyProperty ForegroundEnabledProperty = BindingUtils.Register<ImageRepeatButton, Brush>(nameof(ForegroundEnabled), OnForegroundEnabledChanged);

        public static readonly DependencyProperty ForegroundSelectedProperty = BindingUtils.Register<ImageRepeatButton, Brush>(nameof(ForegroundSelected));


        #endregion

        #region Constructors

        static ImageRepeatButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ImageRepeatButton),
                new FrameworkPropertyMetadata(typeof(ImageRepeatButton)));
        }

        public ImageRepeatButton()
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
            var button = (ImageRepeatButton)d;
            button.InitDefaults();
        }

        private static void OnForegroundEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var button = (ImageRepeatButton)d;
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
        public Thickness IconMargin { get; set; }

        [Bindable]
        public Brush ForegroundDisabled { get; set; }

        [Bindable]
        public Brush ForegroundEnabled { get; set; }

        [Bindable]
        public Brush ForegroundSelected { get; set; }
        #endregion
    }
}
