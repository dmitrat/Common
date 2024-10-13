using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using OutWit.Common.Locker;
using OutWit.Common.MessagePack.Ranges;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Utils;
using Binding = System.Windows.Data.Binding;

namespace OutWit.Common.Controls.Input
{
    public class RangeSliderInput : InputBase<Range<int>>
    {
        #region DependencyProperties

        public static readonly DependencyProperty TicksProperty = BindingUtils.Register<RangeSliderInput, DoubleCollection>(nameof(Ticks), OnTicksChanged);

        public static readonly DependencyProperty IsSnapToTickEnabledProperty = BindingUtils.Register<RangeSliderInput, bool>(nameof(IsSnapToTickEnabled));
        public static readonly DependencyProperty TickPlacementProperty = BindingUtils.Register<RangeSliderInput, TickPlacement>(nameof(TickPlacement));

        public static readonly DependencyProperty UnitsKeyProperty = BindingUtils.Register<RangeSliderInput, string>(nameof(UnitsKey), OnUnitsKeyChanged);
        public static readonly DependencyProperty UnitsProperty = BindingUtils.Register<RangeSliderInput, string>(nameof(Units));

        public static readonly DependencyProperty ValueMinProperty = BindingUtils.Register<RangeSliderInput, double>(nameof(ValueMin), double.MinValue, OnValueMinChanged);
        public static readonly DependencyProperty ValueMaxProperty = BindingUtils.Register<RangeSliderInput, double>(nameof(ValueMax), double.MaxValue, OnValueMaxChanged);

        public static readonly DependencyProperty ValueLeftProperty = BindingUtils.Register<RangeSliderInput, double>(nameof(ValueLeft), double.MinValue, OnValueLeftChanged);
        public static readonly DependencyProperty ValueRightProperty = BindingUtils.Register<RangeSliderInput, double>(nameof(ValueRight), double.MaxValue, OnValueRightChanged);

        #endregion

        #region Constructors

        static RangeSliderInput()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RangeSliderInput),
                new FrameworkPropertyMetadata(typeof(RangeSliderInput)));
        }

        public RangeSliderInput()
        {
        }

        #endregion

        #region Initialization

        
        #endregion

        #region Functions

        private void ResetBoundaries()
        {
            if(Ticks == null || Ticks.Count == 0)
                return;

            ValueMin = Ticks.Min();
            ValueMax = Ticks.Max();
        }

        private void ResetUnits()
        {
            if (TextConverter == null || string.IsNullOrEmpty(UnitsKey))
                return;

            SetBinding(UnitsProperty, new Binding { Converter = TextConverter, ConverterParameter = UnitsKey });
        }

        private void ResetValues()
        {
            if (GlobalLocker.IsLocked() || Value == null)
                return;

            using (new GlobalLocker())
            {
                ValueLeft = Value.From;
                ValueRight = Value.To;
            }
        }

        private void CheckValues()
        {
            //if (GlobalLocker.IsLocked())
            //    return;

            //using (new GlobalLocker())
            //{
            //    if (ValueLeft < ValueMin)
            //        ValueLeft = ValueMin;

            //    if (ValueRight > ValueMax)
            //        ValueRight = ValueMax;

            //}
        }


        private void ResetRange()
        {
            if (GlobalLocker.IsLocked())
                return;

            using (new GlobalLocker())
            {
                Value.Reset((int)ValueLeft, (int)ValueRight);
            }
        }

        #endregion

        #region Events Handlers

        private static void OnTicksChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (RangeSliderInput)source;
            input.ResetBoundaries();
        }

        private static void OnUnitsKeyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (RangeSliderInput)source;
            input.ResetUnits();
        }

        private static void OnValueLeftChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (RangeSliderInput)source;
            input.ResetRange();
        }

        private static void OnValueRightChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (RangeSliderInput)source;
            input.ResetRange();
        }

        private static void OnValueMinChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (RangeSliderInput)source;
            input.CheckValues();
        }

        private static void OnValueMaxChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (RangeSliderInput)source;
            input.CheckValues();
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if(e.IsProperty((RangeSliderInput i)=>i.TextConverter))
                ResetUnits();

            if (e.IsProperty((RangeSliderInput i) => i.Value))
                ResetValues();
        }

        #endregion

        #region Properties

        [Bindable]
        public DoubleCollection Ticks { get; set; }

        [Bindable]
        public bool IsSnapToTickEnabled { get; set; }

        [Bindable]
        public TickPlacement TickPlacement { get; set; }

        [Bindable]
        public string UnitsKey { get; set; }

        [Bindable]
        public string Units { get; set; }

        [Bindable]
        public double ValueMin { get; set; }

        [Bindable]
        public double ValueMax { get; set; }

        [Bindable]
        public double ValueLeft { get; set; }

        [Bindable]
        public double ValueRight { get; set; }

        #endregion
    }
}
