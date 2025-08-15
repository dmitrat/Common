using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MaterialDesignThemes.Wpf;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Commands;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Input
{
    public class DropDownSelectorInput : InputBase<double>
    {
        #region DependencyProperties

        public static readonly DependencyProperty OverlayBrushProperty = BindingUtils.Register<DropDownSelectorInput, Brush>(nameof(OverlayBrush));

        public static readonly DependencyProperty TicksProperty = BindingUtils.Register<DropDownSelectorInput, DoubleCollection>(nameof(Ticks), OnTicksChanged);

        public static readonly DependencyProperty SuffixKeyProperty = BindingUtils.Register<DropDownSelectorInput, string>(nameof(SuffixKey), OnSuffixKeyChanged);
        public static readonly DependencyProperty SuffixProperty = BindingUtils.Register<DropDownSelectorInput, string>(nameof(Suffix));

        public static readonly DependencyProperty PrefixKeyProperty = BindingUtils.Register<DropDownSelectorInput, string>(nameof(PrefixKey), OnPrefixKeyChanged);
        public static readonly DependencyProperty PrefixProperty = BindingUtils.Register<DropDownSelectorInput, string>(nameof(Prefix));

        public static readonly DependencyProperty IsPopupOpenProperty = BindingUtils.Register<DropDownSelectorInput, bool>(nameof(IsPopupOpen));

        public static readonly DependencyProperty PopupMouseOverCmdProperty = BindingUtils.Register<DropDownSelectorInput, ICommand>(nameof(PopupMouseOverCmd));

        public static readonly DependencyProperty ContentMouseOverCmdProperty = BindingUtils.Register<DropDownSelectorInput, ICommand>(nameof(ContentMouseOverCmd));

        public static readonly DependencyProperty PreviewMouseDownCmdProperty = BindingUtils.Register<DropDownSelectorInput, ICommand>(nameof(PreviewMouseDownCmd));


        #endregion

        #region Constructors

        static DropDownSelectorInput()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DropDownSelectorInput),
                new FrameworkPropertyMetadata(typeof(DropDownSelectorInput)));
        }

        public DropDownSelectorInput()
        {
            InitDefaults();
            InitCommands();
        }

        #endregion

        #region Initializations

        private void InitDefaults()
        {
            IsPopupOpen = false;
        }

        private void InitCommands()
        {
            PopupMouseOverCmd = new DelegateCommand(x => PopupMouseOver((bool) x));
            ContentMouseOverCmd = new DelegateCommand(x => ContentMouseOver((bool) x));
            PreviewMouseDownCmd = new DelegateCommand(x=>PreviewMouseClick(x as MouseButtonEventArgs));
        }

        #endregion

        #region Functions

        private void ResetBoundaries()
        {
        }

        private void ResetPrefix()
        {
            if (TextConverter == null || string.IsNullOrEmpty(PrefixKey))
                return;

            SetBinding(PrefixProperty, new Binding { Converter = TextConverter, ConverterParameter = PrefixKey });
        }

        private void ResetSuffix()
        {
            if (TextConverter == null || string.IsNullOrEmpty(SuffixKey))
                return;

            SetBinding(SuffixProperty, new Binding { Converter = TextConverter, ConverterParameter = SuffixKey });
        }

        private void PreviewMouseClick(MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void PopupMouseOver(bool isMouseOver)
        {
            if (isMouseOver)
            {
                StopCloseTime();
                IsPopupOpen = true;
            }

            else
                SetCloseTimer();
        }

        private void ContentMouseOver(bool isMouseOver)
        {
            if (isMouseOver)
            {
                StopCloseTime();
                IsPopupOpen = true;
            }

            else
                SetCloseTimer();
        }

        private void SetCloseTimer()
        {
            Timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500),
                IsEnabled = true
            };

            Timer.Tick += (_, __) => IsPopupOpen = false;
            Timer.Start();
        }

        private void StopCloseTime()
        {
            Timer?.Stop();
            Timer = null;
        }

        #endregion

        #region Events Handlers

        private static void OnTicksChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (DropDownSelectorInput)source;
            input.ResetBoundaries();
        }

        private static void OnSuffixKeyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (DropDownSelectorInput)source;
            input.ResetSuffix();
        }

        private static void OnPrefixKeyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (DropDownSelectorInput)source;
            input.ResetPrefix();
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.IsProperty((SliderInput i) => i.TextConverter))
            {
                ResetSuffix();
                ResetPrefix();
            }
        }

        #endregion

        #region Properties
        
        private DispatcherTimer Timer { get; set; }

        #endregion

        #region Bindable Properties

        [Bindable]
        public Brush OverlayBrush { get; set; }

        [Bindable]
        public DoubleCollection Ticks { get; set; }

        [Bindable]
        public string SuffixKey { get; set; }

        [Bindable]
        public string Suffix { get; set; }

        [Bindable]
        public string PrefixKey { get; set; }

        [Bindable]
        public string Prefix { get; set; }

        [Bindable]
        public bool IsPopupOpen { get; set; }

        [Bindable]
        public ICommand PopupMouseOverCmd { get; set; }

        [Bindable]
        public ICommand ContentMouseOverCmd { get; set; }

        [Bindable]
        public ICommand PreviewMouseDownCmd { get; set; }

        #endregion
    }
}
