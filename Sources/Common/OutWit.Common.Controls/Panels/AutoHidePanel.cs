using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OutWit.Common.Controls.Utils;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Panels
{
    public class AutoHidePanel : ContentControl
    {
        #region Constants

        private const double TOLERANCE = 0.000001;

        #endregion

        #region DependencyProperties

        public static readonly DependencyProperty IsFixedProperty = BindingUtils.Register<AutoHidePanel, bool>(nameof(IsFixed));
        public static readonly DependencyProperty StatusProperty = BindingUtils.Register<AutoHidePanel, AutoHidePanesStatus>(nameof(Status));
        public static readonly DependencyProperty ExpandIfFixedProperty = BindingUtils.Register<AutoHidePanel, bool>(nameof(ExpandIfFixed));

        public static readonly DependencyProperty OrientationProperty = BindingUtils.Register<AutoHidePanel, Orientation>(nameof(Orientation));

        public static readonly RoutedEvent ExpandedEvent = BindingUtils.Register<AutoHidePanel>(nameof(Expanded));
        public static readonly RoutedEvent CollapsedEvent = BindingUtils.Register<AutoHidePanel>(nameof(Collapsed));

        #endregion

        #region Constructors

        static AutoHidePanel()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(AutoHidePanel),
                new FrameworkPropertyMetadata(typeof(AutoHidePanel)));
        }

        public AutoHidePanel()
        {
            InitDefaults();
            InitEvents();
        }
        #endregion

        #region Initialization

        private void InitDefaults()
        {
        }

        private void InitEvents()
        {
            MouseEnter += OnMouseEnter;
            MouseLeave += OnMouseLeave;
            Loaded += OnLoaded;

            PreviewKeyDown += OnKeyPressed;
            PreviewKeyUp += OnKeyPressed;
        }

        #endregion

        #region Functions

        private void ResetAnimation()
        {
            Triggers.Clear();

            switch (Orientation)
            {
                case Orientation.Horizontal:
                    Triggers.Add(this.GetTrigger(ExpandedEvent, WidthProperty, x => x.ActualWidth, x => x.MaxWidth, 0.5));
                    Triggers.Add(this.GetTrigger(CollapsedEvent, WidthProperty, x => x.ActualWidth, x => x.MinWidth, 0.5));
                    break;

                case Orientation.Vertical:
                    Triggers.Add(this.GetTrigger(ExpandedEvent, HeightProperty, x => x.ActualHeight, x => x.MaxHeight, 0.5));
                    Triggers.Add(this.GetTrigger(CollapsedEvent, HeightProperty, x => x.ActualHeight, x => x.MinHeight, 0.5));
                    break;
            }
        }

        private void ResetStatus()
        {
            if (IsFixed)
                return;

            switch (Orientation)
            {
                case Orientation.Horizontal:
                    {
                        if (Math.Abs(Width - MaxWidth) < TOLERANCE)
                            Status = AutoHidePanesStatus.Expanded;
                        else if (Math.Abs(Width - MinWidth) < TOLERANCE)
                            Status = AutoHidePanesStatus.Collapsed;
                        else
                            Status = AutoHidePanesStatus.Transition;
                        break;
                    }

                case Orientation.Vertical:
                    {
                        if (Math.Abs(Height - MaxHeight) < TOLERANCE)
                            Status = AutoHidePanesStatus.Expanded;
                        else if (Math.Abs(Height - MinHeight) < TOLERANCE)
                            Status = AutoHidePanesStatus.Collapsed;
                        else
                            Status = AutoHidePanesStatus.Transition;
                        break;
                    }
            }
        }

        private void Resize()
        {
            if (!IsFixed)
                return;

            switch (Status)
            {
                case AutoHidePanesStatus.Collapsed: RaiseEvent(new RoutedEventArgs(CollapsedEvent, this)); break;
                case AutoHidePanesStatus.Expanded: RaiseEvent(new RoutedEventArgs(ExpandedEvent, this)); break;
            }
        }

        private void ResetFixedStatus()
        {
            if (!ExpandIfFixed)
                return;

            if (IsFixed)
            {
                Status = AutoHidePanesStatus.Expanded;
                RaiseEvent(new RoutedEventArgs(ExpandedEvent, this));
            }
            else
            {
                Status = AutoHidePanesStatus.Collapsed;
                RaiseEvent(new RoutedEventArgs(CollapsedEvent, this));
            }
        }

        private void CheckSize(AutoHidePanesStatus defaultStatus)
        {
            //if (!IsFixed)
            //    return;

            if (Status == AutoHidePanesStatus.Transition)
                Status = defaultStatus;

            if (Status == AutoHidePanesStatus.Expanded)
            {
                ResetAnimation();
                RaiseEvent(new RoutedEventArgs(ExpandedEvent, this));
            }

            if (Status == AutoHidePanesStatus.Collapsed)
            {
                ResetAnimation();
                RaiseEvent(new RoutedEventArgs(CollapsedEvent, this));
            }

        
        }

        #endregion

        #region Events Handlers

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.IsProperty((AutoHidePanel p) => p.Width))
                ResetStatus();

            if (e.IsProperty((AutoHidePanel p) => p.Height))
                ResetStatus();

            if (e.IsProperty((AutoHidePanel p) => p.MaxWidth))
                CheckSize(AutoHidePanesStatus.Expanded);

            if (e.IsProperty((AutoHidePanel p) => p.MaxHeight))
                CheckSize(AutoHidePanesStatus.Expanded);

            if (e.IsProperty((AutoHidePanel p) => p.MinWidth))
                CheckSize(AutoHidePanesStatus.Collapsed);

            if (e.IsProperty((AutoHidePanel p) => p.MinHeight))
                CheckSize(AutoHidePanesStatus.Collapsed);

            if (e.IsProperty((AutoHidePanel p) => p.Orientation))
                ResetAnimation();

            if (e.IsProperty((AutoHidePanel p) => p.IsFixed))
                ResetFixedStatus();

            if (e.IsProperty((AutoHidePanel p) => p.Status))
                Resize();

            base.OnPropertyChanged(e);
        }

        private void OnMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (IsFixed)
                return;

            RaiseEvent(new RoutedEventArgs(CollapsedEvent, this));
        }

        private void OnMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (IsFixed)
                return;

            RaiseEvent(new RoutedEventArgs(ExpandedEvent, this));
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ResetAnimation();
            ResetStatus();
            ResetFixedStatus();
        }

        private void OnKeyPressed(object sender, KeyEventArgs e)
        {
            e.Handled = true;
        }
        #endregion

        #region Properties

        [Bindable]
        public bool IsFixed { get; set; }

        [Bindable]
        public AutoHidePanesStatus Status { get; set; }

        [Bindable]
        public Orientation Orientation { get; set; }

        [Bindable]
        public bool ExpandIfFixed { get; set; }

        #endregion

        #region Routed Events

        public event RoutedEventHandler Expanded
        {
            add => AddHandler(ExpandedEvent, value);
            remove => RemoveHandler(ExpandedEvent, value);
        }

        public event RoutedEventHandler Collapsed
        {
            add => AddHandler(CollapsedEvent, value);
            remove => RemoveHandler(CollapsedEvent, value);
        }


        #endregion
    }

    public enum AutoHidePanesStatus
    {
        Collapsed = 0,
        Transition,
        Expanded
    }
}
