using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Xaml.Behaviors;
using OutWit.Common.Controls.Adorners;
using OutWit.Common.Controls.Buttons;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Behaviors
{
    public class DragSourceBehavior : Behavior<FrameworkElement>
    {
        #region DependencyProperties

        public static readonly DependencyProperty DataProperty = BindingUtils.Register<DragSourceBehavior, object>(nameof(Data), OnDataChanged);

        #endregion

        #region Constructors

        public DragSourceBehavior()
        {
            InitDefaults();
        }

        #endregion

        #region Initialization

        private void InitDefaults()
        {
            IsDragging = false;
        }

        #endregion

        #region Functions


        private void StartDragOperation(MouseEventArgs e)
        {
            var window = AssociatedObject.FindFirstParentOf<Window>();

            if(window == null)
                return;

            IsDragging = true;

            var container = window.Content as FrameworkElement;
            Adorner = new DragAdorner(container, AssociatedObject, e.GetPosition(container));
            AdornerLayer.GetAdornerLayer(AssociatedObject)?.Add(Adorner);

            DragDrop.AddQueryContinueDragHandler(AssociatedObject, OnDragContinue);
            DragDrop.DoDragDrop(AssociatedObject, Data ?? AssociatedObject, DragDropEffects.All);
        }

        private bool IsUserTryingToDrag(Point point)
        {
            return (Math.Abs(point.X - Position.X) >= SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(point.Y - Position.Y) >= SystemParameters.MinimumVerticalDragDistance);
        }

        private bool CheckSource(object source)
        {
            if (source is Thumb)
                return false;

            if (source is ScrollBar)
                return false;

            if (source is CheckBox)
                return false;

            if (source is Button)
                return false;

            return true;
        }

        #endregion

        #region Event Handlers

        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.PreviewMouseLeftButtonDown += OnMouseDown;
            AssociatedObject.PreviewMouseMove += OnMouseMove;
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            IsDragging = false;

            Position = e.GetPosition(AssociatedObject);

        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if(!CheckSource(e.OriginalSource) ||IsDragging)
                return;

            if(e.LeftButton != MouseButtonState.Pressed)
                return;

            if(IsUserTryingToDrag(e.GetPosition(AssociatedObject)))
                StartDragOperation(e);
        }

        private void OnDragContinue(object sender, QueryContinueDragEventArgs e)
        {
            if (e.Action == DragAction.Continue && e.KeyStates != DragDropKeyStates.LeftMouseButton)
                Adorner.Close();
        }

        private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }

        #endregion

        #region Properties

        private DragAdorner Adorner { get; set; }

        private Point Position { get; set; }

        private bool IsDragging { get; set; }

        #endregion

        #region Bindable Properties

        [Bindable]
        public object Data { get; set; } 

        #endregion
    }
}
