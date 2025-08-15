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
    public class HorizontalScrollViewer : ScrollViewer
    {
        #region DependencyProperties

        public static readonly DependencyProperty HideScrollProperty = BindingUtils.Register<HorizontalScrollViewer, bool>(nameof(HideScroll));
        public static readonly DependencyProperty ScrollLeftCmdProperty = BindingUtils.Register<HorizontalScrollViewer, ICommand>(nameof(ScrollLeftCmd));
        public static readonly DependencyProperty ScrollRightCmdProperty = BindingUtils.Register<HorizontalScrollViewer, ICommand>(nameof(ScrollRightCmd));

        #endregion

        #region Constructors
        static HorizontalScrollViewer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(HorizontalScrollViewer),
                new FrameworkPropertyMetadata(typeof(HorizontalScrollViewer)));
        }

        public HorizontalScrollViewer()
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
            ScrollLeftCmd = new DelegateCommand(x => LineLeft());
            ScrollRightCmd = new DelegateCommand(x => LineRight());
        }

        #endregion

        #region Functions

        private void Update()
        {
            if (Content is FrameworkElement control && ScrollInfo is ScrollContentPresenter content)
                HideScroll = control.ActualWidth <= content.ActualWidth;
            else
                HideScroll = false;
        }

        #endregion

        #region Event Handlers

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
                LineLeft();
            else
                LineRight();

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
        public bool HideScroll { get; set; }

        [Bindable]
        public ICommand ScrollLeftCmd { get; set; }

        [Bindable]
        public ICommand ScrollRightCmd { get; set; }

        #endregion
    }
}
