using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Sliders
{
    public class MultiRangeSliderItem : Slider
    {
        #region Events

        public event MultiRangeSliderItemSelectedEventHandler Selected = delegate { };

        public event MultiRangeSliderItemSelectedEventHandler LeftValueChanged = delegate { };

        #endregion

        #region DependencyProperties

        public static readonly DependencyProperty LeftValueProperty = BindingUtils.Register<MultiRangeSliderItem, double>(nameof(LeftValue), double.MinValue, OnLeftValueChanged);
        public static readonly DependencyProperty RightValueProperty = BindingUtils.Register<MultiRangeSliderItem, double>(nameof(RightValue), double.MaxValue);
        
        public static readonly DependencyProperty MinimumValueProperty = BindingUtils.Register<MultiRangeSliderItem, double>(nameof(MinimumValue), double.MinValue, MinimumValueChanged);
        public static readonly DependencyProperty MaximumValueProperty = BindingUtils.Register<MultiRangeSliderItem, double>(nameof(MaximumValue), double.MaxValue, MaximumValueChanged);
        
        public static readonly DependencyProperty IsSelectedProperty = BindingUtils.Register<MultiRangeSliderItem, bool>(nameof(IsSelected), IsSelectedChanged);
        
        public static readonly DependencyProperty ItemProperty = BindingUtils.Register<MultiRangeSliderItem, object>(nameof(Item));

        #endregion

        #region Constructors

        static MultiRangeSliderItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MultiRangeSliderItem),
                new FrameworkPropertyMetadata(typeof(MultiRangeSliderItem)));
        }

        public MultiRangeSliderItem()
        {
            IsFirst = false;
            IsLast = false;

            //IsBlocked = true;
            Maximum = double.MaxValue;
            Minimum = double.MinValue;
            //Value = 21;
            //IsBlocked = false;
        }

        #endregion

        #region Functions

        private double ValidateValue(double value)
        {
            if (value >= MinimumValue + TickFrequency && value <= MaximumValue - TickFrequency)
                return value;
            if (Math.Abs(value - MaximumValue) < TickFrequency)
                return IsLast ? MaximumValue : (MaximumValue - TickFrequency);
            if (Math.Abs(value - MinimumValue) < TickFrequency)
                return IsFirst ? MinimumValue : (MinimumValue + TickFrequency);

            return double.NaN;
        }

        private void ReportSelected()
        {
            Selected(this);
        }

        public void Block()
        {
            IsBlocked = true;
        }

        public void Release()
        {
            IsBlocked = false;
        }

        private void ReportLeftValueChanged()
        {
            LeftValueChanged(this);
        }

        #endregion

        #region Event Handlers

        protected override void OnValueChanged(double oldValue, double newValue)
        {
            if (IsBlocked) 
                return;

            IsBlocked = true;

            var validatedValue = ValidateValue(newValue);

            if (!double.IsNaN(validatedValue))
            {
                Value = validatedValue;
                LeftValue = validatedValue;
            }
            else
            {
                Value = oldValue;
            }

            IsBlocked = false;
        }

        protected override void OnThumbDragStarted(DragStartedEventArgs e)
        {
            if (Item != null) IsSelected = true;

            base.OnThumbDragStarted(e);
        }


        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.IsProperty((MultiRangeSliderItem i) => i.Minimum) && Value < Minimum)
            {
                IsBlocked = true;
                Value = Minimum;
                IsBlocked = false;

            }
        }

        private static void OnLeftValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sliderItem = (MultiRangeSliderItem)d;
            sliderItem.Value = sliderItem.LeftValue;
            sliderItem.ReportLeftValueChanged();
        }

        private static void MinimumValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sliderItem = (MultiRangeSliderItem)d;

            //sliderItem.RightValue = sliderItem.MaximumValue;

        }

        private static void MaximumValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sliderItem = (MultiRangeSliderItem)d;

            sliderItem.RightValue = sliderItem.MaximumValue;

        }

        private static void IsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sliderItem = (MultiRangeSliderItem)d;

            if(sliderItem.IsSelected)
                sliderItem.ReportSelected();
        }

        #endregion

        #region Properties

        private bool IsBlocked { get; set; }
        public bool IsFirst { get; set; }
        public bool IsLast { get; set; }

        [Bindable]
        public double LeftValue { get; set; }

        [Bindable]
        public double RightValue { get; set; }

        [Bindable]
        public double MaximumValue { get; set; }

        [Bindable]
        public double MinimumValue { get; set; }

        [Bindable]
        public bool IsSelected { get; set; }

        [Bindable]
        public object Item { get; set; }

        #endregion
    }

    public delegate void MultiRangeSliderItemSelectedEventHandler(MultiRangeSliderItem sender);
}
