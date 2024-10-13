using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using OutWit.Common.Controls.Utils;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Panels
{
    public class CardDeckPanel : Panel
    {
        #region Constants

        private const double TRANSFORM_ORIGIN_X = 0.8;
        private const double TRANSFORM_ORIGIN_Y = 1.0;

        #endregion

        #region DependencyProperties

        public static readonly DependencyProperty CardWidthProperty = BindingUtils.Register<CardDeckPanel, double>(nameof(CardWidth), 100);
        public static readonly DependencyProperty CardHeightProperty = BindingUtils.Register<CardDeckPanel, double>(nameof(CardHeight), 250);

        public static readonly DependencyProperty HorizontalOffsetProperty = BindingUtils.Register<CardDeckPanel, double>(nameof(HorizontalOffset), 5);
        public static readonly DependencyProperty VerticalOffsetProperty = BindingUtils.Register<CardDeckPanel, double>(nameof(VerticalOffset), 10);

        public static readonly DependencyProperty AnimationDurationProperty = BindingUtils.Register<CardDeckPanel, double>(nameof(AnimationDuration), 700);

        public static readonly DependencyProperty MaxVisibleCardsProperty = BindingUtils.Register<CardDeckPanel, int>(nameof(MaxVisibleCards), 5);

        #endregion

        #region Constructors

        public CardDeckPanel()
        {
            InitDefaults();
            InitEvents();
        }

        #endregion

        #region Initialization

        private void InitDefaults()
        {
            OrderedElements = new List<UIElement>();
        }

        private void InitEvents()
        {
            this.MouseWheel += OnMouseWheel;
        }

        #endregion

        #region Functions
        
        protected override Size MeasureOverride(Size availableSize)
        {
            var desiredSize = GetDesiredSize();

            return desiredSize.Within(availableSize) ? desiredSize : availableSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            foreach (UIElement child in Children)
            {
                child.Measure(finalSize);
                child.Arrange(new Rect(0, 0, child.DesiredSize.Width, child.DesiredSize.Height));
            }

            Animate();

            return finalSize;
        }

        private Size GetDesiredSize()
        {
            //var visibleChildCount = Children.Count > 0 ? 
            //    Children.OfType<UIElement>().Count(element => element.IsVisible) : 0;

            //visibleChildCount = Math.Min(visibleChildCount, MaxVisibleCards);

            var visibleChildCount = Math.Min(Children.Count, MaxVisibleCards);

            var width = 0.0;
            var height = 0.0;

            if (visibleChildCount > 0)
            {
                width = CardWidth + (visibleChildCount - 1) * HorizontalOffset;
                height = CardHeight + (visibleChildCount - 1) * VerticalOffset;
            }

            return new Size(width, height);
        }

        private void Animate()
        {
            UpdateVisibility();

            double x = 0;
            double y = 0;

            int index = 0;
            foreach (var element in OrderedElements)
            {
                if (element.Visibility == Visibility.Collapsed)
                    continue;

                AnimateTo(element, x, y);

                x += HorizontalOffset;
                y += VerticalOffset;

                SetZIndex(element, index++);
            }
        }

        private void AnimateTo(UIElement child, double x, double y)
        {
            if (child == null) 
                return;

            var group = (TransformGroup)child.RenderTransform;
            var trans = (TranslateTransform)group.Children[0];

            trans.BeginAnimation(TranslateTransform.XProperty, MakeAnimation(x));
            trans.BeginAnimation(TranslateTransform.YProperty, MakeAnimation(y));
        }

        private DoubleAnimation MakeAnimation(double to)
        {
            return new DoubleAnimation(to, TimeSpan.FromMilliseconds(AnimationDuration))
            {
                AccelerationRatio = 0.2, DecelerationRatio = 0.7
            };
        }

        private void ScrollDown()
        {
            var element = OrderedElements.First();
            OrderedElements.Remove(element);
            OrderedElements.Add(element);
            Animate();
        }

        private void ScrollUp()
        {
            var element = OrderedElements.Last();
            OrderedElements.Remove(element);
            OrderedElements.Insert(0, element);
            Animate();
        }

        private void ScrollTo(UIElement child)
        {
            OrderedElements.Remove(child);
            OrderedElements.Add(child);
            Animate();
        }

        private void UpdateVisibility()
        {
            for (int i = Math.Max(OrderedElements.Count - MaxVisibleCards, 0); i < OrderedElements.Count; i++)
                OrderedElements[i].Visibility = Visibility.Visible;

            for (int i = 0; i < OrderedElements.Count - MaxVisibleCards; i++)
                OrderedElements[i].Visibility = Visibility.Collapsed;
        }

        private void AddElement(UIElement element)
        {
            element.AddHandler(MouseLeftButtonDownEvent, new RoutedEventHandler(CardLeftButtonDown), true);

            if (!(element.RenderTransform is TransformGroup))
            {
                var group = new TransformGroup();
                group.Children.Add(new TranslateTransform());

                element.RenderTransformOrigin = new Point(TRANSFORM_ORIGIN_X, TRANSFORM_ORIGIN_Y);
                element.RenderTransform = group;
            }

            OrderedElements.Add(element);

        }

        private void RemoveElement(UIElement element)
        {
            element.RemoveHandler(MouseLeftButtonDownEvent, new RoutedEventHandler(CardLeftButtonDown));

            if (OrderedElements.Contains(element))
                OrderedElements.Remove(element);
        }

        #endregion

        #region Event Handlers

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
                ScrollDown();
            else
                ScrollUp();
        }

        private void CardLeftButtonDown(object sender, RoutedEventArgs e)
        {
            if (sender is UIElement child && Children.Contains(child))
                ScrollTo(child);
        }

        protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
        {
            base.OnVisualChildrenChanged(visualAdded, visualRemoved);

            if (visualRemoved is UIElement removed)
                RemoveElement(removed);

            if (visualAdded is UIElement added)
                AddElement(added);
            
        }

        #endregion

        #region Properties

        private List<UIElement> OrderedElements { get; set; }

        #endregion

        #region Bindable Properties

        [Bindable]
        public double CardWidth { get; set; }

        [Bindable]
        public double CardHeight { get; set; }

        [Bindable]
        public double HorizontalOffset { get; set; }

        [Bindable]
        public double VerticalOffset { get; set; }

        [Bindable]
        public double AnimationDuration { get; set; }

        [Bindable]
        public int MaxVisibleCards { get; set; }

        #endregion
    }
}
