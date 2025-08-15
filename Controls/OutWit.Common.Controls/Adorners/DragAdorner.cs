using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace OutWit.Common.Controls.Adorners
{
    public class DragAdorner : Adorner
    {
        #region Constructors

        public DragAdorner(FrameworkElement container, UIElement adornedElement, Point point) :
            base(adornedElement)
        {
            Container = container;
            StartPoint = point;

            InitDefaults();
            InitEvents();
            InitVisual();
        }

        #endregion

        #region Initialization

        private void InitDefaults()
        {
            //Container.AllowDrop = true;
            AdornedElement.Opacity = 0.5;
        }

        private void InitEvents()
        {
            Container.DragOver += OnDragOver;
            Container.Drop += OnDrop;
        }

        private void InitVisual()
        {
            Visual = new Rectangle
            {
                Fill = new VisualBrush(AdornedElement),
                Width = AdornedElement.RenderSize.Width,
                Height = AdornedElement.RenderSize.Height,
                IsHitTestVisible = false,
                //RenderTransform = new ScaleTransform(1.1, 1.1),
                Effect = new DropShadowEffect { Opacity = 0.5 },
            };
        }

        #endregion

        #region Functions

        public void Close()
        {
            AdornedElement.Opacity = 1;
            ((AdornerLayer)Parent)?.Remove(this);
        }

        public override GeneralTransform GetDesiredTransform(GeneralTransform transform)
        {
            var transformGroup = new GeneralTransformGroup();
            transformGroup.Children.Add(base.GetDesiredTransform(transform));
            transformGroup.Children.Add(new TranslateTransform(CurrentPoint.X - StartPoint.X, CurrentPoint.Y - StartPoint.Y));
            return transformGroup;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            Visual.Arrange(new Rect(finalSize));
            return Visual.DesiredSize;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            Visual.Arrange(new Rect(constraint));
            return Visual.DesiredSize;
        }

        protected override Visual GetVisualChild(int index)
        {
            return Visual;
        }

        #endregion

        #region Event Handlers

        private void OnDrop(object sender, DragEventArgs e)
        {
            Close();
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            if (!(Parent is AdornerLayer layer)) 
                return;

            CurrentPoint = e.GetPosition(Container);
            layer.Update(AdornedElement);
        } 

        #endregion

        #region Properties

        private FrameworkElement Container { get; }
        private Point StartPoint { get; }

        private UIElement Visual { get; set; }
        private Point CurrentPoint { get; set; }

        protected override int VisualChildrenCount => 1;

        #endregion
    }
}
