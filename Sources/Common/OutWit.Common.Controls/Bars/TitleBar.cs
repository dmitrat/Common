using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OutWit.Common.Controls.Converters;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Bars
{
    public class TitleBar : ContentControl
    {
        #region DependencyProperties

        public static readonly DependencyProperty MinimizeCommandProperty = BindingUtils.Register<TitleBar, ICommand>(nameof(MinimizeCommand));
        public static readonly DependencyProperty MaximizeCommandProperty = BindingUtils.Register<TitleBar, ICommand>(nameof(MaximizeCommand));
        public static readonly DependencyProperty FullScreenCommandProperty = BindingUtils.Register<TitleBar, ICommand>(nameof(FullScreenCommand));
        public static readonly DependencyProperty RestoreCommandProperty = BindingUtils.Register<TitleBar, ICommand>(nameof(RestoreCommand));
        public static readonly DependencyProperty CloseCommandProperty = BindingUtils.Register<TitleBar, ICommand>(nameof(CloseCommand));
        public static readonly DependencyProperty DragCommandProperty = BindingUtils.Register<TitleBar, ICommand>(nameof(DragCommand));
        public static readonly DependencyProperty DoubleClickCommandProperty = BindingUtils.Register<TitleBar, ICommand>(nameof(DoubleClickCommand));

        public static readonly DependencyProperty AutoHideButtonsProperty = BindingUtils.Register<TitleBar, bool>(nameof(AutoHideButtons));

        public static readonly DependencyProperty IsFullScreenProperty = BindingUtils.Register<TitleBar, bool>(nameof(IsFullScreen));
        public static readonly DependencyProperty IsMaximizedProperty = BindingUtils.Register<TitleBar, bool>(nameof(IsMaximized));

        public static readonly DependencyProperty MinimizeTooltipProperty = BindingUtils.Register<TitleBar, string>(nameof(MinimizeTooltip));
        public static readonly DependencyProperty MinimizeTooltipKeyProperty = BindingUtils.Register<TitleBar, string>(nameof(MinimizeTooltipKey), OnMinimizeTooltipKeyChanged);

        public static readonly DependencyProperty MaximizeTooltipProperty = BindingUtils.Register<TitleBar, string>(nameof(MaximizeTooltip));
        public static readonly DependencyProperty MaximizeTooltipKeyProperty = BindingUtils.Register<TitleBar, string>(nameof(MaximizeTooltipKey), OnMaximizeTooltipKeyChanged);

        public static readonly DependencyProperty FullScreenTooltipProperty = BindingUtils.Register<TitleBar, string>(nameof(FullScreenTooltip));
        public static readonly DependencyProperty FullScreenTooltipKeyProperty = BindingUtils.Register<TitleBar, string>(nameof(FullScreenTooltipKey), OnFullScreenTooltipKeyChanged);

        public static readonly DependencyProperty RestoreTooltipProperty = BindingUtils.Register<TitleBar, string>(nameof(RestoreTooltip));
        public static readonly DependencyProperty RestoreTooltipKeyProperty = BindingUtils.Register<TitleBar, string>(nameof(RestoreTooltipKey), OnRestoreTooltipKeyChanged);

        public static readonly DependencyProperty CloseTooltipProperty = BindingUtils.Register<TitleBar, string>(nameof(CloseTooltip));
        public static readonly DependencyProperty CloseTooltipKeyProperty = BindingUtils.Register<TitleBar, string>(nameof(CloseTooltipKey), OnCloseTooltipKeyChanged);

        public static readonly DependencyProperty TextConverterProperty = BindingUtils.Register<TitleBar, StringToResourceConverterBase>(nameof(TextConverter), OnTextConverterChanged);

        #endregion

        #region Constructors

        static TitleBar()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TitleBar),
                new FrameworkPropertyMetadata(typeof(TitleBar)));
        }

        public TitleBar()
        {
            InitEvents();
        }

        #endregion

        #region Initialization

        private void InitEvents()
        {
            MouseDown += OnMouseDown;
        }

        #endregion

        #region Functions

        private void ResetText()
        {
            if(TextConverter == null)
                return;

            if (!string.IsNullOrEmpty(MinimizeTooltipKey))
                SetBinding(MinimizeTooltipProperty, this.CreateBinding(x => x.MinimizeTooltipKey, TextConverter));

            if (!string.IsNullOrEmpty(MaximizeTooltipKey))
                SetBinding(MaximizeTooltipProperty, this.CreateBinding(x => x.MaximizeTooltipKey, TextConverter));

            if (!string.IsNullOrEmpty(FullScreenTooltipKey))
                SetBinding(FullScreenTooltipProperty, this.CreateBinding(x => x.FullScreenTooltipKey, TextConverter));

            if (!string.IsNullOrEmpty(RestoreTooltipKey))
                SetBinding(RestoreTooltipProperty, this.CreateBinding(x => x.RestoreTooltipKey, TextConverter));

            if (!string.IsNullOrEmpty(CloseTooltipKey))
                SetBinding(CloseTooltipProperty, this.CreateBinding(x => x.CloseTooltipKey, TextConverter));
        }


        #endregion

        #region Event Handlers

        private static void OnMinimizeTooltipKeyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var titleBar = (TitleBar)source;
            titleBar.ResetText();
        }

        private static void OnMaximizeTooltipKeyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var titleBar = (TitleBar)source;
            titleBar.ResetText();
        }

        private static void OnFullScreenTooltipKeyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var titleBar = (TitleBar)source;
            titleBar.ResetText();
        }

        private static void OnRestoreTooltipKeyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var titleBar = (TitleBar)source;
            titleBar.ResetText();
        }

        private static void OnCloseTooltipKeyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var titleBar = (TitleBar)source;
            titleBar.ResetText();
        }

        private static void OnTextConverterChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var titleBar = (TitleBar)source;
            titleBar.ResetText();
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
                DragCommand?.Execute(null);
            else
                DoubleClickCommand?.Execute(null);
        }

        #endregion

        #region Properties

        [Bindable]
        public ICommand MinimizeCommand { get; set; }

        [Bindable]
        public ICommand MaximizeCommand { get; set; }

        [Bindable]
        public ICommand FullScreenCommand { get; set; }

        [Bindable]
        public ICommand RestoreCommand { get; set; }

        [Bindable]
        public ICommand CloseCommand { get; set; }

        [Bindable]
        public ICommand DragCommand { get; set; }

        [Bindable]
        public ICommand DoubleClickCommand { get; set; }

        [Bindable]
        public bool AutoHideButtons { get; set; }

        [Bindable]
        public bool IsMaximized { get; set; }

        [Bindable]
        public bool IsFullScreen { get; set; }

        [Bindable]
        public string MinimizeTooltip { get; set; }

        [Bindable]
        public string MinimizeTooltipKey { get; set; }

        [Bindable]
        public string MaximizeTooltip { get; set; }

        [Bindable]
        public string MaximizeTooltipKey { get; set; }

        [Bindable]
        public string FullScreenTooltip { get; set; }

        [Bindable]
        public string FullScreenTooltipKey { get; set; }

        [Bindable]
        public string RestoreTooltip { get; set; }

        [Bindable]
        public string RestoreTooltipKey { get; set; }

        [Bindable]
        public string CloseTooltip { get; set; }

        [Bindable]
        public string CloseTooltipKey { get; set; }

        [Bindable]
        public StringToResourceConverterBase TextConverter { get; set; }
        #endregion
    }
}
