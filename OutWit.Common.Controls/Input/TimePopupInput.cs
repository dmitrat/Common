using System;
using System.Collections;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Commands;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Input
{
    [TemplatePart(Name = nameof(TimePopupInput.Input), Type = typeof(TextBox))]
    [TemplatePart(Name = nameof(TimePopupInput.Time), Type = typeof(Clock))]
    public class TimePopupInput : InputBase<DateTime?>
    {
        #region DependencyProperties

        public static readonly DependencyProperty OpenOnLoadProperty = BindingUtils.Register<TimePopupInput, bool>(nameof(OpenOnLoad));

        public static readonly DependencyProperty ValueMinProperty = BindingUtils.Register<TimePopupInput, DateTime?>(nameof(ValueMin));
        public static readonly DependencyProperty ValueMaxProperty = BindingUtils.Register<TimePopupInput, DateTime?>(nameof(ValueMax));

        public static readonly DependencyProperty PopupDelayProperty = BindingUtils.Register<TimePopupInput, int>(nameof(PopupDelay), 200);
        public static readonly DependencyProperty PopupMaxHeightProperty = BindingUtils.Register<TimePopupInput, double>(nameof(PopupMaxHeight), 100);
        public static readonly DependencyProperty PopupBackgroundProperty = BindingUtils.Register<TimePopupInput, Brush>(nameof(PopupBackground), Brushes.White);
        public static readonly DependencyProperty IsPopupOpenProperty = BindingUtils.Register<TimePopupInput, bool>(nameof(IsPopupOpen));

        public static readonly DependencyProperty OpenPopupCmdProperty = BindingUtils.Register<TimePopupInput, ICommand>(nameof(OpenPopupCmd));
        public static readonly DependencyProperty ClosePopupCmdProperty = BindingUtils.Register<TimePopupInput, ICommand>(nameof(ClosePopupCmd));

        #endregion

        #region Constructors

        static TimePopupInput()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TimePopupInput),
                new FrameworkPropertyMetadata(typeof(TimePopupInput)));
        }

        public TimePopupInput()
        {
            InitEvents();
            InitCommands();
        }

        #endregion

        #region Initialization

        private void InitEvents()
        {
            this.Loaded += OnLoaded;
            this.GotMouseCapture += OnGotMouseCapture;
            
        }

        private void InitCommands()
        {
            OpenPopupCmd = new DelegateCommand(x => OpenPopup());
            ClosePopupCmd = new DelegateCommand(x => ClosePopup());
        }

        #endregion

        #region Functions


        private void OpenPopup()
        {
            IsPopupOpen = true;
        }

        private void ClosePopup()
        {
            IsPopupOpen = false;

            var parentGrid = this.FindFirstParentOf<DataGrid>();
            parentGrid?.CommitEdit(DataGridEditingUnit.Row, true);

        }

        private void FocusInput()
        {
            Input.CaretIndex = Int32.MaxValue;
            Input.Focus();
        }

        #endregion

        #region Event Handlers
        
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            FocusInput();

            if(OpenOnLoad)
                OpenPopup();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            Input = Template.FindName(nameof(Input), this) as TextBox;
            Time = Template.FindName(nameof(Time), this) as Clock;

            Time?.AddHandler(Clock.ClockChoiceMadeEvent, new ClockChoiceMadeEventHandler(OnClockChoiceMade));
        }

        private void OnClockChoiceMade(object sender, ClockChoiceMadeEventArgs e)
        {
            if(e.Mode == ClockDisplayMode.Minutes)
                ClosePopup();
        }

        private void OnGotMouseCapture(object sender, MouseEventArgs e)
        {
            OpenPopup();
        }

        #endregion

        #region Properties

        [MVVM.Aspects.Bindable]
        public bool OpenOnLoad { get; set; }

        [MVVM.Aspects.Bindable]
        public DateTime? ValueMin { get; set; }

        [MVVM.Aspects.Bindable]
        public DateTime? ValueMax { get; set; }

        [MVVM.Aspects.Bindable]
        public int PopupDelay { get; set; }

        [MVVM.Aspects.Bindable]
        public double PopupMaxHeight { get; set; }

        [MVVM.Aspects.Bindable]
        public Brush PopupBackground { get; set; }

        [MVVM.Aspects.Bindable]
        public bool IsPopupOpen { get; set; }

        [MVVM.Aspects.Bindable]
        public ICommand OpenPopupCmd { get; private set; }

        [MVVM.Aspects.Bindable]
        public ICommand ClosePopupCmd { get; private set; }

        private TextBox Input { get; set; }
        private Clock Time { get; set; }
        #endregion
    }
}
