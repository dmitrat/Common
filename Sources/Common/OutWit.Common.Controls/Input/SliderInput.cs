using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Utils;
using static System.Windows.Forms.DataFormats;

namespace OutWit.Common.Controls.Input
{
    public class SliderInput : InputBase<double?>
    {
        #region DependencyProperties

        public static readonly DependencyProperty TicksProperty = BindingUtils.Register<SliderInput, DoubleCollection>(nameof(Ticks), OnTicksChanged);

        public static readonly DependencyProperty IsSnapToTickEnabledProperty = BindingUtils.Register<SliderInput, bool>(nameof(IsSnapToTickEnabled));
        public static readonly DependencyProperty TickPlacementProperty = BindingUtils.Register<SliderInput, TickPlacement>(nameof(TickPlacement));

        public static readonly DependencyProperty UnitsKeyProperty = BindingUtils.Register<SliderInput, string>(nameof(UnitsKey), OnUnitsKeyChanged);
        public static readonly DependencyProperty UnitsProperty = BindingUtils.Register<SliderInput, string>(nameof(Units));

        public static readonly DependencyProperty TickFrequencyProperty = BindingUtils.Register<SliderInput, double>(nameof(TickFrequency));

        public static readonly DependencyProperty ValueMinProperty = BindingUtils.Register<SliderInput, double>(nameof(ValueMin), double.MinValue);
        public static readonly DependencyProperty ValueMaxProperty = BindingUtils.Register<SliderInput, double>(nameof(ValueMax), double.MaxValue);
        public static readonly DependencyProperty FormatProperty = BindingUtils.Register<SliderInput, string>(nameof(Format));


        #endregion

        #region Constructors

        static SliderInput()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SliderInput),
                new FrameworkPropertyMetadata(typeof(SliderInput)));
        }

        public SliderInput()
        {
        }

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

        #endregion

        #region Events Handlers

        private static void OnTicksChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (SliderInput)source;
            input.ResetBoundaries();
        }

        private static void OnUnitsKeyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (SliderInput)source;
            input.ResetUnits();
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if(e.IsProperty((SliderInput i)=>i.TextConverter))
                ResetUnits();
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
        public double TickFrequency { get; set; }

        [Bindable]
        public double ValueMin { get; set; }

        [Bindable]
        public double ValueMax { get; set; }

        [Bindable]
        public string Format { get; set; }

        #endregion
    }
}
