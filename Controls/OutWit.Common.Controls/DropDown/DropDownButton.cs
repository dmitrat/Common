using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Threading;
using OutWit.Common.MVVM.Commands;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.DropDown
{
    [ContentProperty(nameof(Content))]
    [DefaultProperty(nameof(Content))]
    public class DropDownButton : DropDownBase
    {
        #region DependencyProperties

        public static readonly DependencyProperty ContentProperty = BindingUtils.Register<DropDownButton, object>(nameof(Content));

        public static readonly DependencyProperty IsPopupOpenProperty = BindingUtils.Register<DropDownButton, bool>(nameof(IsPopupOpen), IsPopupOpenChanged);

        public static readonly DependencyProperty ClosePopupCmdProperty = BindingUtils.Register<DropDownButton, ICommand>(nameof(ClosePopupCmd));

        public static readonly DependencyProperty PopupMouseOverCmdProperty = BindingUtils.Register<DropDownButton, ICommand>(nameof(PopupMouseOverCmd));
        public static readonly DependencyProperty ContentMouseOverCmdProperty = BindingUtils.Register<DropDownButton, ICommand>(nameof(ContentMouseOverCmd));
        public static readonly DependencyProperty PreviewMouseDownCmdProperty = BindingUtils.Register<DropDownButton, ICommand>(nameof(PreviewMouseDownCmd));

        public static readonly DependencyProperty PopupStatusChangedCmdProperty = BindingUtils.Register<DropDownButton, ICommand>(nameof(PopupStatusChangedCmd));

        #endregion

        #region Constructors

        static DropDownButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DropDownButton),
                new FrameworkPropertyMetadata(typeof(DropDownButton)));
        }

        public DropDownButton()
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
            ClosePopupCmd = new DelegateCommand(x=>ClosePopup());

            PopupMouseOverCmd = new DelegateCommand(x => PopupMouseOver((bool) x));
            ContentMouseOverCmd = new DelegateCommand(x => ContentMouseOver((bool) x));
            PreviewMouseDownCmd = new DelegateCommand(x => PreviewMouseClick(x as MouseButtonEventArgs));
        }

        #endregion

        #region Functions

        public void ClosePopup()
        {
            IsPopupOpen = false;
        }
        
        private void ResetHeader()
        {
            if (TextConverter == null || string.IsNullOrEmpty(HeaderKey))
                return;

            SetBinding(HeaderProperty, new Binding { Converter = TextConverter, ConverterParameter = HeaderKey });
        }

        private void ResetTooltip()
        {
            if (TextConverter == null || string.IsNullOrEmpty(ToolTipKey))
                return;

            SetBinding(ToolTipProperty, new Binding { Converter = TextConverter, ConverterParameter = ToolTipKey });
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

        private void RisePopupStatusChanged()
        {
            PopupStatusChangedCmd?.Execute(IsPopupOpen);
        }

        #endregion

        #region Events Handlers

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if(e.IsProperty((DropDownBase d)=>d.HeaderKey))
                ResetHeader();

            if (e.IsProperty((DropDownBase d) => d.ToolTipKey))
                ResetTooltip();

            if (e.IsProperty((DropDownBase d) => d.TextConverter))
            {
                ResetHeader();
                ResetTooltip();
            }
        }

        private static void IsPopupOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var button = (DropDownButton) d;
            button.RisePopupStatusChanged();
        }

        #endregion

        #region Properties

        private DispatcherTimer Timer { get; set; }

        #endregion

        #region Bindable Properties

        [MVVM.Aspects.Bindable]
        public object Content { get; set; }

        [MVVM.Aspects.Bindable]
        internal bool IsPopupOpen { get; private set; }

        [MVVM.Aspects.Bindable]
        public ICommand ClosePopupCmd { get; internal set; }

        [MVVM.Aspects.Bindable]
        internal ICommand PopupMouseOverCmd { get; private set; }
        [MVVM.Aspects.Bindable]
        internal ICommand ContentMouseOverCmd { get; private set; }
        [MVVM.Aspects.Bindable]
        internal ICommand PreviewMouseDownCmd { get; private set; }

        [MVVM.Aspects.Bindable]
        public ICommand PopupStatusChangedCmd { get; set; }

        #endregion
    }
}
