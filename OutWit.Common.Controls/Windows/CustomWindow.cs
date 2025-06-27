using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using OutWit.Common.Controls.Utils;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Commands;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Windows
{
    public class CustomWindow : Window
    {
        #region Events

        public event FullScreenStateChangedEventHandler FullScreenStateChanged = delegate { };

        #endregion


        #region DependencyProperties

        public static readonly DependencyProperty MinimizeCommandProperty = BindingUtils.Register<CustomWindow, ICommand>(nameof(MinimizeCommand));
        public static readonly DependencyProperty MaximizeCommandProperty = BindingUtils.Register<CustomWindow, ICommand>(nameof(MaximizeCommand));
        public static readonly DependencyProperty FullScreenCommandProperty = BindingUtils.Register<CustomWindow, ICommand>(nameof(FullScreenCommand));
        public static readonly DependencyProperty RestoreCommandProperty = BindingUtils.Register<CustomWindow, ICommand>(nameof(RestoreCommand));
        public static readonly DependencyProperty CloseCommandProperty = BindingUtils.Register<CustomWindow, ICommand>(nameof(CloseCommand));
        public static readonly DependencyProperty DragCommandProperty = BindingUtils.Register<CustomWindow, ICommand>(nameof(DragCommand));
        public static readonly DependencyProperty DoubleClickCommandProperty = BindingUtils.Register<CustomWindow, ICommand>(nameof(DoubleClickCommand));

        public static readonly DependencyProperty IsFullScreenProperty = BindingUtils.Register<CustomWindow, bool>(nameof(IsFullScreen));
        public static readonly DependencyProperty IsMaximizedProperty = BindingUtils.Register<CustomWindow, bool>(nameof(IsMaximized));

        #endregion

        #region Constructors

        static CustomWindow()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CustomWindow),
                new FrameworkPropertyMetadata(typeof(CustomWindow)));
        }

        public CustomWindow()
        {
            InitDefaults();
            InitCommands();
        }

        #endregion

        #region Initialization

        private void InitDefaults()
        {
            
        }

        private void InitCommands()
        {
            MaximizeCommand = new DelegateCommand(x => Maximize());
            MinimizeCommand = new DelegateCommand(x => Minimize());
            FullScreenCommand = new DelegateCommand(x => FullScreen());
            RestoreCommand = new DelegateCommand(x => Restore());
            CloseCommand = new DelegateCommand(x => Close());
            DoubleClickCommand = new DelegateCommand(x => ToggleMaximize());
            DragCommand = new DelegateCommand(x => DragMove());
        }

        #endregion

        #region Functions

        public void Maximize()
        {
            MaxWidth = this.AreaWidth() + 7; //ResizeBorderThickness (5) + CornerRadius (2)
            MaxHeight = this.AreaHeight() + 7; //ResizeBorderThickness (5) + CornerRadius (2)

            WindowState = WindowState.Maximized;
        }

        public void Minimize()
        {
            WindowState = WindowState.Minimized;
        }

        public void ToggleMaximize()
        {
            if(WindowState == WindowState.Maximized)
                Restore();
            else
                Maximize();
        }

        public void FullScreen()
        {
            if (WindowState == WindowState.Maximized)
                WindowState = WindowState.Normal;

            MaxWidth = this.DeviceWidth() + 7; //ResizeBorderThickness (5) + CornerRadius (2)
            MaxHeight = this.DeviceHeight() + 7; //ResizeBorderThickness (5) + CornerRadius (2)

            IsFullScreen = true;
            WindowState = WindowState.Maximized;

            FullScreenStateChanged(this, IsFullScreen);
        }

        public void Restore()
        {
            IsFullScreen = false;
            WindowState = WindowState.Normal;

            FullScreenStateChanged(this, IsFullScreen);
        }

        #endregion

        #region Properties

        [MVVM.Aspects.Bindable]
        public bool IsFullScreen { get; private set; }

        [MVVM.Aspects.Bindable]
        public bool IsMaximized { get; set; }

        [MVVM.Aspects.Bindable]
        public ICommand MinimizeCommand { get; private set; }

        [MVVM.Aspects.Bindable]
        public ICommand MaximizeCommand { get; private set; }

        [MVVM.Aspects.Bindable]
        public ICommand FullScreenCommand { get; private set; }

        [MVVM.Aspects.Bindable]
        public ICommand RestoreCommand { get; private set; }

        [MVVM.Aspects.Bindable]
        public ICommand CloseCommand { get; private set; }

        [MVVM.Aspects.Bindable]
        public ICommand DragCommand { get; private set; }

        [MVVM.Aspects.Bindable]
        public ICommand DoubleClickCommand { get; private set; }
        #endregion
    }

    public delegate void FullScreenStateChangedEventHandler(object sender, bool isFullScreen);
}
