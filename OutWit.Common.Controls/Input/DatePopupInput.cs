using System;
using System.Collections;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Commands;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Input
{
    [TemplatePart(Name = nameof(DatePopupInput.Input), Type = typeof(TextBox))]
    public class DatePopupInput : InputBase<DateTime?>
    {
        #region DependencyProperties

        public static readonly DependencyProperty OpenOnLoadProperty = BindingUtils.Register<DatePopupInput, bool>(nameof(OpenOnLoad));

        public static readonly DependencyProperty ValueMinProperty = BindingUtils.Register<DatePopupInput, DateTime?>(nameof(ValueMin));
        public static readonly DependencyProperty ValueMaxProperty = BindingUtils.Register<DatePopupInput, DateTime?>(nameof(ValueMax));

        public static readonly DependencyProperty PopupDelayProperty = BindingUtils.Register<DatePopupInput, int>(nameof(PopupDelay), 200);
        public static readonly DependencyProperty PopupMaxHeightProperty = BindingUtils.Register<DatePopupInput, double>(nameof(PopupMaxHeight), 100);
        public static readonly DependencyProperty PopupBackgroundProperty = BindingUtils.Register<DatePopupInput, Brush>(nameof(PopupBackground), Brushes.White);
        public static readonly DependencyProperty IsPopupOpenProperty = BindingUtils.Register<DatePopupInput, bool>(nameof(IsPopupOpen));

        public static readonly DependencyProperty OpenPopupCmdProperty = BindingUtils.Register<DatePopupInput, ICommand>(nameof(OpenPopupCmd));
        public static readonly DependencyProperty ClosePopupCmdProperty = BindingUtils.Register<DatePopupInput, ICommand>(nameof(ClosePopupCmd));

        #endregion

        #region Constructors

        static DatePopupInput()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DatePopupInput),
                new FrameworkPropertyMetadata(typeof(DatePopupInput)));
        }

        public DatePopupInput()
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
        #endregion
    }
}
