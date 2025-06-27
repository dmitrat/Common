using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AspectInjector.Broker;
using MaterialDesignThemes.Wpf;
using OutWit.Common.Controls.Converters;
using OutWit.Common.Controls.Special;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Commands;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Popup
{
    public class PopupButton : ContentControl
    {
        #region DependencyProperties

        public static readonly DependencyProperty IconKindProperty = BindingUtils.Register<PopupButton, PackIconKind?>(nameof(IconKind));
        public static readonly DependencyProperty IconHeightProperty = BindingUtils.Register<PopupButton, double>(nameof(IconHeight));
        public static readonly DependencyProperty IconWidthProperty = BindingUtils.Register<PopupButton, double>(nameof(IconWidth));

        public static readonly DependencyProperty OverlayBrushProperty = BindingUtils.Register<PopupButton, Brush>(nameof(OverlayBrush), Brushes.White);
        public static readonly DependencyProperty PassiveBrushProperty = BindingUtils.Register<PopupButton, Brush>(nameof(PassiveBrush));
        public static readonly DependencyProperty ActiveBrushProperty = BindingUtils.Register<PopupButton, Brush>(nameof(ActiveBrush));

        public static readonly DependencyProperty IsPassiveProperty = BindingUtils.Register<PopupButton, bool>(nameof(IsPassive));
        public static readonly DependencyProperty IsSelectedProperty = BindingUtils.Register<PopupButton, bool>(nameof(IsSelected));

        public static readonly DependencyProperty TitleProperty = BindingUtils.Register<PopupButton, string>(nameof(Title));
        public static readonly DependencyProperty TitleKeyProperty = BindingUtils.Register<PopupButton, string>(nameof(TitleKey), OnTitleKeyChanged);

        public static readonly DependencyProperty HeaderProperty = BindingUtils.Register<PopupButton, string>(nameof(Header));
        public static readonly DependencyProperty HeaderKeyProperty = BindingUtils.Register<PopupButton, string>(nameof(HeaderKey), OnHeaderKeyChanged);

        public static readonly DependencyProperty ToolTipKeyProperty = BindingUtils.Register<PopupButton, string>(nameof(ToolTipKey), OnToolTipKeyChanged);

        public static readonly DependencyProperty TextConverterProperty = BindingUtils.Register<PopupButton, StringToResourceConverterBase>(nameof(TextConverter), OnTextConverterChanged);

        public static readonly DependencyProperty RefreshPopupBindingCommandProperty = BindingUtils.Register<PopupButton, ICommand>(nameof(RefreshPopupBindingCommand));
        public static readonly DependencyProperty MouseClickCommandProperty = BindingUtils.Register<PopupButton, ICommand>(nameof(MouseClickCommand));
        public static readonly DependencyProperty SecondaryActionCommandProperty = BindingUtils.Register<PopupButton, ICommand>(nameof(SecondaryActionCommand));

        #endregion

        #region Constructors

        static PopupButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PopupButton),
                new FrameworkPropertyMetadata(typeof(PopupButton)));
        }

        public PopupButton()
        {
            InitCommands();
        }

        #endregion

        #region Initialization

        private void InitCommands()
        {
            RefreshPopupBindingCommand = new DelegateCommand(x=>RefreshPopupBinding(x as DependencyObject));
            MouseClickCommand = new DelegateCommand(x=> MouseClick(x as MouseButtonEventArgs));

        }

        #endregion

        #region Functions

        private void RefreshPopupBinding(DependencyObject popup)
        {
            popup?.UpdateBinding();
        }

        private void MouseClick(MouseButtonEventArgs e)
        {
            if(e == null)
                return;

            if(e.MiddleButton == MouseButtonState.Pressed)
                SecondaryActionCommand?.Execute(null);

            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                e.Handled = true;
                if(e.LeftButton == MouseButtonState.Pressed)
                    SecondaryActionCommand?.Execute(null);
            }
        }

        private void ResetTitle()
        {
            if (TextConverter == null || string.IsNullOrEmpty(TitleKey))
                return;

            SetBinding(TitleProperty, this.CreateBinding(x => x.TitleKey, TextConverter));
        }

        private void ResetHeader()
        {
            if (TextConverter == null || string.IsNullOrEmpty(HeaderKey))
                return;

            SetBinding(HeaderProperty, this.CreateBinding(x => x.HeaderKey, TextConverter));
        }

        private void ResetToolTip()
        {
            if (TextConverter == null || string.IsNullOrEmpty(ToolTipKey))
                return;

            SetBinding(ToolTipProperty, this.CreateBinding(x => x.ToolTipKey, TextConverter));
        }

        #endregion

        #region Events Handlers

        private static void OnTitleKeyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (PopupButton)source;
            input.ResetTitle();
        }

        private static void OnHeaderKeyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (PopupButton)source;
            input.ResetHeader();
        }

        private static void OnToolTipKeyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (PopupButton)source;
            input.ResetToolTip();
        }

        private static void OnTextConverterChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (PopupButton)source;
            input.ResetToolTip();
            input.ResetHeader();
            input.ResetTitle();
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.IsProperty((PopupButton b) => b.Foreground))
                IsSelected = Foreground == ActiveBrush;

            base.OnPropertyChanged(e);
        }

        #endregion

        #region Properties

        [Bindable]
        public PackIconKind? IconKind { get; set; }

        [Bindable]
        public double IconHeight { get; set; }

        [Bindable]
        public double IconWidth { get; set; }

        [Bindable]
        public Brush OverlayBrush { get; set; }

        [Bindable]
        public Brush PassiveBrush { get; set; }


        [Bindable]
        public Brush ActiveBrush { get; set; }

        [Bindable]
        public bool IsPassive { get; set; }

        [Bindable]
        public bool IsSelected { get; set; }

        [Bindable]
        public string Title { get; set; }

        [Bindable]
        public string TitleKey { get; set; }

        [Bindable]
        public string Header { get; set; }

        [Bindable]
        public string HeaderKey { get; set; }

        [Bindable]
        public string ToolTipKey { get; set; }

        [Bindable]
        public StringToResourceConverterBase TextConverter { get; set; }

        [Bindable]
        public ICommand RefreshPopupBindingCommand { get; private set; }

        [Bindable]
        public ICommand MouseClickCommand { get; private set; }


        [Bindable]
        public ICommand SecondaryActionCommand { get; set; }
        #endregion
    }
}
