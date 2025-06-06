using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Linq.Expressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using OutWit.Common.Exceptions;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Utils;
using OutWit.Common.Utils;

namespace OutWit.Common.Controls.Sliders
{
    public class MultiRangeSlider : Grid
    {
        #region Events

        public event MultiRangeSliderClicked MultiRangeSliderBarClicked = delegate { };

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty RangesSourceProperty = BindingUtils.Register<MultiRangeSlider, IEnumerable>(nameof(RangesSource), RangesSourceChanged);
        public static readonly DependencyProperty RangesProperty = BindingUtils.Register<MultiRangeSlider, ObservableCollection<MultiRangeSliderItem>>(nameof(Ranges));

        public static readonly DependencyProperty SelectedItemProperty = BindingUtils.Register<MultiRangeSlider, object>(nameof(SelectedItem), SelectedItemChanged);

        public static readonly DependencyProperty ThumbWidthProperty = BindingUtils.Register<MultiRangeSlider, double>(nameof(ThumbWidth));

        public static readonly DependencyProperty MinimumProperty = BindingUtils.Register<MultiRangeSlider, double>(nameof(Minimum), 0.0, MinimumChanged);
        public static readonly DependencyProperty MaximumProperty = BindingUtils.Register<MultiRangeSlider, double>(nameof(Maximum), 100.0, MaximumChanged);

        public static readonly DependencyProperty TickFrequencyProperty = BindingUtils.Register<MultiRangeSlider, double>(nameof(TickFrequency), 1.0, TickFrequencyChanged);

        public static readonly DependencyProperty ForegroundProperty = BindingUtils.Register<MultiRangeSlider, Brush>(nameof(Foreground), Brushes.Black, ForegroundChanged);
        public static readonly DependencyProperty TrackHeightProperty = BindingUtils.Register<MultiRangeSlider, double>(nameof(TrackHeight), 3, TrackHeightChanged);
        public static readonly DependencyProperty SelectionHeightProperty = BindingUtils.Register<MultiRangeSlider, double>(nameof(SelectionHeight), 3, SelectionHeightChanged);
        public static readonly DependencyProperty RadiusXProperty = BindingUtils.Register<MultiRangeSlider, double>(nameof(RadiusX), 3, RadiusXChanged);
        public static readonly DependencyProperty RadiusYProperty = BindingUtils.Register<MultiRangeSlider, double>(nameof(RadiusY), 3, RadiusYChanged);

        public static readonly DependencyProperty IsSnapToTickEnabledProperty = BindingUtils.Register<MultiRangeSlider, bool>(nameof(IsSnapToTickEnabled), true, IsSnapToTickEnabledChanged);

        #endregion

        #region Constructors

        //static MultiRangeSlider()
        //{
        //    DefaultStyleKeyProperty.OverrideMetadata(typeof(MultiRangeSlider),
        //        new FrameworkPropertyMetadata(typeof(MultiRangeSlider)));
        //}

        public MultiRangeSlider()
        {
            InitDefaults();
            InitEvents();
        }

        #endregion

        #region Initialization

        private void InitDefaults()
        {
            Ranges = new ObservableCollection<MultiRangeSliderItem>();
            Track = new Rectangle {Height = TrackHeight, Fill = Foreground, VerticalAlignment = VerticalAlignment.Center, RadiusX = RadiusX, RadiusY = RadiusY, Opacity = 0.38};
            Selection = new Border { Height = SelectionHeight, Background = Foreground, VerticalAlignment = VerticalAlignment.Center };

            this.Children.Add(Track);
            this.Children.Add(Selection);
        }

        private void InitEvents()
        {
            Ranges.CollectionChanged += OnItemsCollectionChanged;

            this.SizeChanged += OnSizeChanged;
            this.Loaded += OnLoaded;
            this.MouseLeftButtonDown += OnMouseLeftButtonDown;
        }

        #endregion

        #region Functions

        private void ResetSliders()
        {
            ClearSliders();

            if (RangesSource != null)
                CreateSliders();
        }

        private void ClearSliders()
        {
            var slidersToRemove = Ranges.ToArray();

            foreach (var slider in slidersToRemove)
            {
                BindingOperations.ClearBinding(slider, MultiRangeSliderItem.RightValueProperty);
                BindingOperations.ClearBinding(slider, MultiRangeSliderItem.LeftValueProperty);
                BindingOperations.ClearBinding(slider, MultiRangeSliderItem.MinimumValueProperty);
                BindingOperations.ClearBinding(slider, MultiRangeSliderItem.MaximumValueProperty);

                Ranges.Remove(slider);
            }
        }

        private void CreateSliders()
        {
            foreach (var item in RangesSource)
                Ranges.Add(CreateSlider(item));

            InitSliders();

        }

        private MultiRangeSliderItem CreateSlider(object item)
        {
            var slider = new MultiRangeSliderItem { Item = item };

            slider.SetBinding(MultiRangeSliderItem.LeftValueProperty, new Binding { Source = item, Path = LeftValueBinding.Path, Mode = LeftValueBinding.Mode });
            slider.SetBinding(MultiRangeSliderItem.RightValueProperty, new Binding { Source = item, Path = RightValueBinding.Path, Mode = RightValueBinding.Mode });
            
            return slider;
        }

        private void InitSliders()
        {
            Ranges.First().IsFirst = true;

            for (int i = 0; i < Ranges.Count; i++)
            {
                InitSliderMinimum(i > 0 ? Ranges[i - 1] : null, Ranges[i]);
                InitSliderMaximum(Ranges[i], i < Ranges.Count - 1 ? Ranges[i + 1] : null);

                Ranges[i].LeftValueChanged += OnSliderValueChanged;
            }

            AddLastRange();

            ArrangeSliders();
        }

        private void AddLastRange()
        {
            if(Ranges.Last()?.IsLast == true)
                return;

            Ranges.Add(CreateLastSliderFromItem(Ranges.Last()));
        }

        private void InitSliderMaximum(MultiRangeSliderItem slider, MultiRangeSliderItem nextSlider)
        {
            slider.SetBinding(MultiRangeSliderItem.MaximumValueProperty, nextSlider == null ? GetBinding(slider, x => x.RightValue, BindingMode.OneWay) : GetBinding(nextSlider, x => x.LeftValue));
        }

        private void InitSliderMinimum(MultiRangeSliderItem previousSlider, MultiRangeSliderItem slider)
        {
            if (previousSlider == null)
                slider.MinimumValue = Minimum;
            else
                slider.SetBinding(MultiRangeSliderItem.MinimumValueProperty, GetBinding(previousSlider, x => x.LeftValue));
        }

        private Binding GetBinding(MultiRangeSliderItem slider, Expression<Func<MultiRangeSliderItem, double>> expression, BindingMode mode = BindingMode.TwoWay)
        {
            return new Binding { Source = slider, Path = new PropertyPath(PropertiesUtils.NameOfProperty(expression)), Mode = mode };
        }

        private MultiRangeSliderItem CreateLastSliderFromItem(MultiRangeSliderItem lastItem)
        {
            var slider = new MultiRangeSliderItem
            {
                Item = null,
                IsLast = true,
                MaximumValue = Maximum
            };

            slider.SetBinding(MultiRangeSliderItem.LeftValueProperty, GetBinding(lastItem, x => x.RightValue));
            slider.SetBinding(MultiRangeSliderItem.MinimumValueProperty, GetBinding(lastItem, x => x.LeftValue, BindingMode.OneWay));

            slider.LeftValueChanged += OnSliderValueChanged;

            return slider;

        }

        private void ResetSelectedSlider()
        {
            var selectedSlider = Ranges.Single(x => x.Item == SelectedItem);

            if (!selectedSlider.IsSelected)
                selectedSlider.IsSelected = true;

            foreach (var slider in Ranges.Where(slider => !slider.Equals(selectedSlider)))
                slider.IsSelected = false;
        }

        private void ArrangeSliders()
        {
            if(Ranges.Count == 0)
                return;

            var nValues = Ranges.Count - 1;

            try
            {
                for (int i = 0; i < nValues; i++)
                {
                    Ranges[i].Maximum = Maximum + ThumbValue() * (nValues - i);
                    Ranges[i].Minimum = Minimum - ThumbValue() * i;
                }

                Ranges.Last().Minimum = Minimum - ThumbValue() * nValues;

                AdjustSelection();
            }
            catch (Exception e)
            {
                

            }

         
        }

        private double WidthFromValue(double value)
        {
            return ActualWidth * (value - Minimum) / (Maximum - Minimum);
        }

        private MultiRangeSliderItem ApplyParentStyle(MultiRangeSliderItem item)
        {
            item.Block();
            item.Value = Math.Max(Minimum, item.LeftValue);
            item.Minimum = Minimum;
            item.Maximum = Maximum;
            item.TickFrequency = TickFrequency;
            item.IsSnapToTickEnabled = IsSnapToTickEnabled;
            item.Foreground = Foreground;
            item.Release();

            return item;
        }

        private double ThumbValue()
        {
            return ActualWidth > 0 ? ThumbWidth * (Maximum - Minimum) / ActualWidth : 0;
        }

        private void AdjustSelection()
        {
            Selection.Margin = new Thickness(WidthFromValue(Ranges.First().LeftValue), 0, ActualWidth - WidthFromValue(Ranges.Last().LeftValue), 0);
        }

        #endregion

        #region Event Handlers

        
        private void OnSliderValueChanged(MultiRangeSliderItem item)
        {
            AdjustSelection();
        }

        private void OnItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
                foreach (var item in e.NewItems.Cast<MultiRangeSliderItem>())
                {
                    item.Selected += OnItemSelected;
                    this.Children.Add(ApplyParentStyle(item));
                }
            if (e.Action == NotifyCollectionChangedAction.Remove)
                foreach (var item in e.OldItems.Cast<MultiRangeSliderItem>())
                {
                    item.Selected -= OnItemSelected;
                    this.Children.Remove(item);
                }

            if (Ranges != null && Ranges.Count > 0)
                Visibility = Visibility.Visible;
            else 
                Visibility = Visibility.Collapsed;
        }

        private void OnItemSelected(MultiRangeSliderItem sender)
        {
            SelectedItem = sender;
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ArrangeSliders();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (Ranges.Count > 0 && RangesSource == null)
                InitSliders();
        }

        private void OnMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount != 2) return;

            var position = Math.Round(e.GetPosition(this).X * Maximum / (ActualWidth * TickFrequency));
            position *= TickFrequency;

            MultiRangeSliderBarClicked(this, position);
        }

        private static void RangesSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var slider = (MultiRangeSlider)d;

            if (slider.Ranges.Count > 0)
                throw new ExceptionOf<MultiRangeSlider>("You can not set RangesSource and Ranges simultaneously");

            if (slider.RangesSource is INotifyCollectionChanged changed)
                changed.CollectionChanged += (_, __) => slider.ResetSliders();

            slider.ResetSliders();
        }

        private static void SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var slider = (MultiRangeSlider)d;

            slider.ResetSelectedSlider();
        }

        private static void MinimumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var slider = (MultiRangeSlider)d;

            foreach (var item in slider.Ranges)
                item.Minimum = slider.Minimum;

            if (slider.Ranges.FirstOrDefault()?.IsFirst == true)
                slider.Ranges.First().MinimumValue = slider.Minimum;
        }

        private static void MaximumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var slider = (MultiRangeSlider)d;

            foreach (var item in slider.Ranges)
                item.Maximum = slider.Maximum;

            if(slider.Ranges.LastOrDefault()?.IsLast == true)
                slider.Ranges.Last().MaximumValue = slider.Maximum;

        }

        private static void TickFrequencyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var slider = (MultiRangeSlider)d;

            foreach (var item in slider.Ranges)
                item.TickFrequency = slider.TickFrequency;

        }

        private static void IsSnapToTickEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var slider = (MultiRangeSlider)d;

            foreach (var item in slider.Ranges)
                item.IsSnapToTickEnabled = slider.IsSnapToTickEnabled;
        }

        private static void ForegroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var slider = (MultiRangeSlider)d;

            foreach (var item in slider.Ranges)
                item.Foreground = slider.Foreground;

            slider.Track.Fill = slider.Foreground;
            slider.Selection.Background = slider.Foreground;
        }

        private static void TrackHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var slider = (MultiRangeSlider)d;

            slider.Track.Height = slider.TrackHeight;
        }

        private static void SelectionHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var slider = (MultiRangeSlider)d;
            
            slider.Selection.Height = slider.SelectionHeight;
        }

        private static void RadiusXChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var slider = (MultiRangeSlider)d;

            slider.Track.RadiusX = slider.RadiusX;
        }

        private static void RadiusYChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var slider = (MultiRangeSlider)d;

            slider.Track.RadiusY = slider.RadiusY;
        }
        #endregion

        #region Properties

        private Rectangle Track { get; set; }

        private Border Selection { get; set; }

        [Bindable]
        public IEnumerable RangesSource { get; set; }

        [Bindable]
        public ObservableCollection<MultiRangeSliderItem> Ranges { get; set; }

        [Bindable]
        public object SelectedItem { get; set; }

        [Bindable]
        public double ThumbWidth { get; set; }

        [Bindable]
        public double Minimum { get; set; }

        [Bindable]
        public double Maximum { get; set; }

        [Bindable]
        public double TickFrequency { get; set; }

        [Bindable]
        public bool IsSnapToTickEnabled { get; set; }

        [Bindable]
        public Brush Foreground { get; set; }

        [Bindable]
        public double TrackHeight { get; set; }

        [Bindable]
        public double SelectionHeight { get; set; }

        [Bindable]
        public double RadiusX { get; set; }

        [Bindable]
        public double RadiusY { get; set; }
        #endregion

        #region Bindings

        public Binding LeftValueBinding { get; set; }

        public Binding RightValueBinding { get; set; }

        #endregion
    }

    public delegate void MultiRangeSliderClicked(MultiRangeSlider sender, double value);
}
