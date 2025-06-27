using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Commands;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Scroll
{
    public class VerticalScrollViewer : ScrollViewer
    {
        #region DependencyProperties

        public static readonly DependencyProperty ScrollButtonHeightProperty = BindingUtils.Register<VerticalScrollViewer, double?>(nameof(ScrollButtonHeight));
        public static readonly DependencyProperty ScrollButtonMarginProperty = BindingUtils.Register<VerticalScrollViewer, Thickness>(nameof(ScrollButtonMargin));
        public static readonly DependencyProperty HideScrollProperty = BindingUtils.Register<VerticalScrollViewer, bool>(nameof(HideScroll));
        public static readonly DependencyProperty ScrollUpCmdProperty = BindingUtils.Register<VerticalScrollViewer, ICommand>(nameof(ScrollUpCmd));
        public static readonly DependencyProperty ScrollDownCmdProperty = BindingUtils.Register<VerticalScrollViewer, ICommand>(nameof(ScrollDownCmd));

        #endregion

        #region Constructors
        static VerticalScrollViewer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(VerticalScrollViewer),
                new FrameworkPropertyMetadata(typeof(VerticalScrollViewer)));
        }

        public VerticalScrollViewer()
        {
            InitEvents();
            InitCommands();
        }

        #endregion

        #region Initialization

        private void InitEvents()
        {
            PreviewMouseWheel += OnMouseWheel;
            SizeChanged += OnSizeChanged;
            Loaded += OnLoaded;
            ScrollChanged += OnScrollChanged;
        }

        private void InitCommands()
        {
            ScrollUpCmd = new DelegateCommand(x => LineUp());
            ScrollDownCmd = new DelegateCommand(x => LineDown());
        }

        #endregion

        #region Functions

        private void Update()
        {
            if (Content is FrameworkElement control && ScrollInfo is ScrollContentPresenter content)
                HideScroll = control.ActualHeight <= content.ActualHeight;
            else
                HideScroll = false;
        }

        #endregion

        #region Event Handlers

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
                LineUp();
            else
                LineDown();

        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            Update();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Update();
        }

        private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            Update();
        }

        #endregion

        #region Properties

        [Bindable]
        public double? ScrollButtonHeight { get; set; }

        [Bindable]
        public Thickness ScrollButtonMargin { get; set; }

        [Bindable]
        public bool HideScroll { get; set; }

        [Bindable]
        public ICommand ScrollUpCmd { get; set; }

        [Bindable]
        public ICommand ScrollDownCmd { get; set; }

        #endregion
    }
}
