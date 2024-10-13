using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Panels
{
    public class VirtualizingWrapPanel : VirtualizingPanel, IScrollInfo
    {
        #region DependencyProperties

        public static readonly DependencyProperty ItemHeightProperty = BindingUtils.Register<VirtualizingWrapPanel, double>(nameof(ItemHeight), 
            FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender, 100.0d);

        public static readonly DependencyProperty ItemWidthProperty = BindingUtils.Register<VirtualizingWrapPanel, double>(nameof(ItemWidth), 
            FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender, 100.0d);

        public static readonly DependencyProperty RowsProperty = BindingUtils.Register<VirtualizingWrapPanel, int>(nameof(Rows), 
            FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender, 4, OnLayoutChanged);
        public static readonly DependencyProperty ColumnsProperty = BindingUtils.Register<VirtualizingWrapPanel, int>(nameof(Columns),
            FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender, 4, OnLayoutChanged);

        public static readonly DependencyProperty LeftTopItemIndexProperty = BindingUtils.Register<VirtualizingWrapPanel, int>(nameof(LeftTopItemIndex), OnScroll);

        #endregion

        #region Fields

        private double m_offset;
        private Size m_extent;
        private Size m_viewport;

        #endregion

        #region Constructors

        public VirtualizingWrapPanel()
        {
            InitDefaults();
        }

        #endregion

        #region Initialization

        private void InitDefaults()
        {
            m_offset = 0;
            m_extent = new Size();
            m_viewport = new Size();

            CanHorizontallyScroll = false;
            CanVerticallyScroll = false;
        }

        #endregion

        #region Functions

        protected override Size MeasureOverride(Size availableSize)
        {
            if (GetItemsCount() == 0 || ItemContainerGenerator == null)
                return new Size();

            //var availableWidth = double.IsInfinity(availableSize.Width) ? ActualWidth : availableSize.Width;
            //var availableHeight = double.IsInfinity(availableSize.Height) ? ActualHeight : availableSize.Height;

            //availableSize = new Size(availableWidth, availableHeight);

            UpdateScrollInfo(availableSize);

            GetVisibleRange(out var firstVisibleItemIndex, out var lastVisibleItemIndex);

            IItemContainerGenerator generator = this.ItemContainerGenerator;

            var position = ItemContainerGenerator.GeneratorPositionFromIndex(firstVisibleItemIndex);
            var childIndex = (position.Offset == 0) ? position.Index : position.Index + 1;

            try
            {
                using (ItemContainerGenerator.StartAt(position, GeneratorDirection.Forward, true))
                {
                    for (var itemIndex = firstVisibleItemIndex; itemIndex <= lastVisibleItemIndex; ++itemIndex, ++childIndex)
                    {
                        var child = (UIElement)ItemContainerGenerator.GenerateNext(out var newlyRealized);

                        if (newlyRealized)
                        {
                            if (childIndex >= InternalChildren.Count)
                                AddInternalChild(child);
                            else
                                InsertInternalChild(childIndex, child);

                            ItemContainerGenerator.PrepareItemContainer(child);
                        }

                        child.Measure(GetChildSize());
                    }
                }
            }
            catch
            {
                return new Size();
            }

            CleanUpItems(firstVisibleItemIndex, lastVisibleItemIndex);

            return availableSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            UpdateScrollInfo(finalSize);

            //int index = 0;
            for (var i = 0; i < Children.Count; i++)
            {
                var child = Children[i];

                if(child.Visibility == Visibility.Collapsed)
                    continue;

                var position = new GeneratorPosition(i, 0);

                var itemIndex = ItemContainerGenerator.IndexFromGeneratorPosition(position);

                //itemIndex = index++;

                ArrangeChild(itemIndex, child);
            }

            return finalSize;
        }

        private void CleanUpItems(int minDesiredGenerated, int maxDesiredGenerated)
        {
            for (var i = InternalChildren.Count - 1; i >= 0; i--)
            {
                var position = new GeneratorPosition(i, 0);

                int itemIndex = ItemContainerGenerator.IndexFromGeneratorPosition(position);

                if (itemIndex < minDesiredGenerated || itemIndex > maxDesiredGenerated)
                {
                    ItemContainerGenerator.Remove(position, 1);
                    RemoveInternalChildRange(i, 1);
                }
            }
        }

        public void ScrollInToView()
        {
            UpdateLayout();

            if (LeftTopItemIndex < 0)
                return;

            var row = LeftTopItemIndex / Columns;

            var itemOffset = row * ItemHeight - VerticalOffset;

            if (itemOffset < 0)
                SetVerticalOffset(VerticalOffset + itemOffset);
            else if (itemOffset + ItemHeight > ViewportHeight)
                SetVerticalOffset(VerticalOffset + itemOffset + ItemHeight - ViewportHeight);

            UpdateLayout();
        }

        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            return rectangle;
        }

        private void UpdateScrollInfo(Size availableSize)
        {
            UpdateExtent(availableSize);
            UpdateViewport(availableSize);
        }

        private void UpdateExtent(Size availableSize)
        {
            var extent = CalculateExtent(availableSize, GetItemsCount());

            if (extent == m_extent)
                return;

            var changeDelta = m_extent.Height / extent.Height;

            m_extent = extent;

            ScrollOwner?.InvalidateScrollInfo();

            if (VerticalOffset != 0)
                SetVerticalOffset(VerticalOffset * changeDelta);
        }

        private void UpdateViewport(Size availableSize)
        {
            if (availableSize == m_viewport)
                return;

            m_viewport = availableSize;

            ScrollOwner?.InvalidateScrollInfo();

            if (m_viewport.Height >= m_extent.Height)
                m_offset = 0;
        } 

        #endregion

        #region Vertical Scroll

        public void SetVerticalOffset(double offset)
        {
            offset = Math.Max(0, Math.Min(ExtentHeight - ViewportHeight, Math.Max(0, offset)));
            offset = Math.Round(offset / ItemHeight, 0) * ItemHeight;

            m_offset = offset <= 0? 0 : offset / (ExtentHeight - ViewportHeight);

            ScrollOwner?.InvalidateScrollInfo();

            InvalidateMeasure();
        }

        public void LineDown()
        {
            SetVerticalOffset(VerticalOffset + ItemHeight);
        }

        public void LineUp()
        {
            SetVerticalOffset(VerticalOffset - ItemHeight);
        }

        public void PageDown()
        {
            SetVerticalOffset(VerticalOffset + ViewportHeight * 0.8);
        }

        public void PageUp()
        {
            SetVerticalOffset(VerticalOffset - ViewportHeight * 0.8);
        }

        public void MouseWheelUp()
        {
            
            //SetVerticalOffset(VerticalOffset - ItemHeight);
        }

        public void MouseWheelDown()
        {
            //SetVerticalOffset(VerticalOffset + ItemHeight);
        }

        #endregion

        #region Horizontal Scroll

        public void SetHorizontalOffset(double offset)
        {
            //Vertical scroll only
        }

        public void LineLeft()
        {
            //Vertical scroll only
        }

        public void LineRight()
        {
            //Vertical scroll only
        }

        public void PageLeft()
        {
            //Vertical scroll only
        }

        public void PageRight()
        {
            //Vertical scroll only
        }

        public void MouseWheelLeft()
        {
            //Vertical scroll only
        }

        public void MouseWheelRight()
        {
            //Vertical scroll only
        }

        #endregion

        #region Tools

        private Size CalculateExtent(Size availableSize, int itemCount)
        {
            ItemWidth = availableSize.Width / Columns;
            ItemHeight = availableSize.Height / Rows;
         
            return new Size(Columns * ItemWidth, ItemHeight * Math.Ceiling((double)itemCount / Columns));
        }

        private void GetVisibleRange(out int firstVisibleItemIndex, out int lastVisibleItemIndex)
        {
            firstVisibleItemIndex = (int)Math.Round(VerticalOffset / ItemHeight) * Columns;
            lastVisibleItemIndex = (int)Math.Round((VerticalOffset + ViewportHeight) / ItemHeight) * Columns - 1;
            
            firstVisibleItemIndex = Math.Max(firstVisibleItemIndex, 0);
            lastVisibleItemIndex = Math.Min(lastVisibleItemIndex, GetItemsCount() - 1);
        }

        private int GetItemsCount()
        {
            //if (Children.Count == 0)
            //    return 0;

            //return Children.OfType<UIElement>().Count(x => x.IsVisible);
            var itemsControl = ItemsControl.GetItemsOwner(this);
            return itemsControl.HasItems ? itemsControl.Items.Count : 0;
        }

        private Size GetChildSize()
        {
            return new Size(ItemWidth, ItemHeight);
        }

        private void ArrangeChild(int itemIndex, UIElement child)
        {
            var row = itemIndex / Columns;
            var column = itemIndex % Columns;

            child.Arrange(new Rect(column * ItemWidth, row * ItemHeight - VerticalOffset, ItemWidth,  ItemHeight));
        }

        #endregion

        #region Event Handlers

        protected override void OnItemsChanged(object sender, ItemsChangedEventArgs args)
        {
            base.OnItemsChanged(sender, args);

            switch (args.Action)
            {
                //case NotifyCollectionChangedAction.Reset: 
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Replace:
                    RemoveInternalChildRange(args.Position.Index, args.ItemUICount);
                    break;
                case NotifyCollectionChangedAction.Move:
                    RemoveInternalChildRange(args.OldPosition.Index, args.ItemUICount);
                    break;
            }

            MeasureOverride(m_viewport);
        }

        private static void OnLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var panel = (VirtualizingWrapPanel) d;
            panel.SetVerticalOffset(0);
            panel.ScrollInToView();
        }

        private static void OnScroll(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var panel = (VirtualizingWrapPanel)d;
            panel.ScrollInToView();
        }

        #endregion

        #region Properties

        public ScrollViewer ScrollOwner { get; set; }

        public bool CanHorizontallyScroll { get; set; }
        public bool CanVerticallyScroll { get; set; }

        public double ExtentHeight => m_extent.Height;
        public double ExtentWidth => m_extent.Width;

        public double ViewportHeight => m_viewport.Height;
        public double ViewportWidth => m_viewport.Width;

        public double HorizontalOffset => 0;
        public double VerticalOffset => Math.Max(0, m_offset * (ExtentHeight - ViewportHeight));

        #endregion

        #region Bindable Properties

        [MVVM.Aspects.Bindable]
        [TypeConverter(typeof(LengthConverter))]
        public double ItemHeight { get; set; }

        [MVVM.Aspects.Bindable]
        [TypeConverter(typeof(LengthConverter))]
        public double ItemWidth { get; set; }

        [MVVM.Aspects.Bindable]
        public int Rows { get; set; }

        [MVVM.Aspects.Bindable]
        public int Columns { get; set; }

        [MVVM.Aspects.Bindable]
        public int LeftTopItemIndex { get; set; }

        #endregion
    }
}
