using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using OutWit.Common.Controls.Grids;
using OutWit.Common.Controls.Input;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Cards
{
    public class FloatingCard : MaterialDesignThemes.Wpf.Card
    {
        #region DependencyProperties

        public static readonly DependencyProperty IsCheckedProperty = BindingUtils.Register<FloatingCard, bool>(nameof(IsChecked));

        public static readonly DependencyProperty WatchForFocusProperty = BindingUtils.Register<FloatingCard, bool>(nameof(WatchForFocus), true);

        #endregion

        #region Constructors

        static FloatingCard()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(FloatingCard),
                new FrameworkPropertyMetadata(typeof(FloatingCard)));
        }

        public FloatingCard()
        {
            InitCommands();
        }

        #endregion

        #region Initialization

        private void InitCommands()
        {
            this.PreviewMouseUp += OnPreviewMouseUp;

            this.Loaded += OnLoaded;
            this.Unloaded += OnUnloaded;

            this.GotFocus += OnGotFocus;
            this.LostFocus += OnLostFocus;

        }

        #endregion

        #region EventHandlers

        private void OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (WatchForFocus)
                this.FindAllChildrenOf<FrameworkElement>(x => x.Focusable).FirstOrDefault()?.Focus();

            else
                Focus();
        }

        private void OnLoaded(object sender, EventArgs e)
        {
            if(!WatchForFocus)
                return;

            var firstFocusable = this.FindAllChildrenOf<FrameworkElement>(x => x.Focusable).FirstOrDefault();
            if(firstFocusable == null)
                return;

            firstFocusable.GotFocus += OnChildrenGotFocus;
            firstFocusable.LostFocus += OnChildrenLostFocus;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (!WatchForFocus)
                return;

            var firstFocusable = this.FindAllChildrenOf<FrameworkElement>(x => x.Focusable).FirstOrDefault();
            if (firstFocusable == null)
                return;

            firstFocusable.GotFocus -= OnChildrenGotFocus;
            firstFocusable.LostFocus -= OnChildrenLostFocus;
        }

        private void OnChildrenGotFocus(object sender, RoutedEventArgs e)
        {
            if(!WatchForFocus)
                return;

            IsChecked = true;
        }

        private void OnChildrenLostFocus(object sender, RoutedEventArgs e)
        {
            if (!WatchForFocus)
                return;

            IsChecked = false;
        }

        private void OnGotFocus(object sender, RoutedEventArgs e)
        {
            if (WatchForFocus)
                return;

            IsChecked = true;
        }

        private void OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (WatchForFocus)
                return;

            IsChecked = false;
        }
        #endregion

        #region Properties

        [Bindable]
        public bool IsChecked { get; set; }

        [Bindable]
        public bool WatchForFocus { get; set; }
        #endregion
    }
}
