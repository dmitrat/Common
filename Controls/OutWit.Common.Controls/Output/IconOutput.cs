using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;
using OutWit.Common.Controls.Converters;
using OutWit.Common.Controls.Menu;
using OutWit.Common.Controls.Special;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Output
{
    public class IconOutput : Control
    {
        #region DependencyProperties

        public static readonly DependencyProperty KindProperty = BindingUtils.Register<IconOutput, PackIconKind?>(nameof(Kind));

        public static readonly DependencyProperty IconHeightProperty = BindingUtils.Register<IconOutput, double>(nameof(IconHeight));

        public static readonly DependencyProperty IconWidthProperty = BindingUtils.Register<IconOutput, double>(nameof(IconWidth));

        public static readonly DependencyProperty IconPositionProperty = BindingUtils.Register<IconOutput, IconPositions>(nameof(IconPosition));

        public static readonly DependencyProperty TextProperty = BindingUtils.Register<IconOutput, string>(nameof(Text));

        public static readonly DependencyProperty TextKeyProperty = BindingUtils.Register<IconOutput, string>(nameof(TextKey), OnTexKeyChanged);

        public static readonly DependencyProperty ToolTipKeyProperty = BindingUtils.Register<IconOutput, string>(nameof(ToolTipKey), OnToolTipKeyChanged);

        public static readonly DependencyProperty TextFormatProperty = BindingUtils.Register<IconOutput, string>(nameof(TextFormat), OnTextFormatChanged);
        public static readonly DependencyProperty TextFormatKeyProperty = BindingUtils.Register<IconOutput, string>(nameof(TextFormatKey), OnTextFormatKeyChanged);

        public static readonly DependencyProperty TextConverterProperty = BindingUtils.Register<IconOutput, StringToResourceConverterBase>(nameof(TextConverter), OnTextConverterChanged);

        #endregion

        #region Constructors

        static IconOutput()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(IconOutput),
                new FrameworkPropertyMetadata(typeof(IconOutput)));
        }

        public IconOutput()
        {
            
        }

        #endregion

        #region Functions

        private void ResetText()
        {
            if (TextConverter == null || string.IsNullOrEmpty(TextKey))
                return;

            SetBinding(TextProperty, this.CreateBinding(x => x.TextKey, new FormatConverter(TextConverter), TextFormat));
        }

        private void ResetFormat()
        {
            if (TextConverter == null || string.IsNullOrEmpty(TextFormatKey))
                return;

            SetBinding(TextFormatProperty, this.CreateBinding(x => x.TextFormatKey, TextConverter));
        }

        public void ResetTooltip()
        {

            if (TextConverter == null || string.IsNullOrEmpty(ToolTipKey))
                return;

            SetBinding(ToolTipProperty, this.CreateBinding(x => x.ToolTipKey, TextConverter));
        }

        #endregion

        #region Events Handlers

        private static void OnTextFormatKeyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (IconOutput)source;
            input.ResetFormat();
        }

        private static void OnTextFormatChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (IconOutput)source;
            input.ResetText();
        }

        private static void OnTexKeyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (IconOutput)source;
            input.ResetText();
        }

        private static void OnToolTipKeyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (IconOutput)source;
            input.ResetTooltip();
        }

        private static void OnTextConverterChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (IconOutput)source;
            input.ResetFormat();
            input.ResetText();
            input.ResetTooltip();
        }


        #endregion

        #region Properties

        [Bindable]
        public PackIconKind? Kind { get; set; }

        [Bindable]
        public double IconHeight { get; set; }

        [Bindable]
        public double IconWidth { get; set; }

        [Bindable]
        public IconPositions IconPosition { get; set; }

        [Bindable]
        public string Text { get; set; }

        [Bindable]
        public string TextKey { get; set; }

        [Bindable]
        public string ToolTipKey { get; set; }

        [Bindable]
        public string TextFormat { get; set; }

        [Bindable]
        public string TextFormatKey { get; set; }

        [Bindable]
        public StringToResourceConverterBase TextConverter { get; set; }
        
        #endregion
    }

    public enum IconPositions
    {
        Left = 0,
        Right = 1,
        Hidden = 2
    }
}
