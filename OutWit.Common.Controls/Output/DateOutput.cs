using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;
using OutWit.Common.Controls.Converters;
using OutWit.Common.Controls.Grids;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Output
{
    public class DateOutput : Control
    {
        #region DependencyProperties
        
        public static readonly DependencyProperty ValueProperty = BindingUtils.Register<DateOutput, DateTime?>(nameof(Value));

        public static readonly DependencyProperty TextStyleProperty = BindingUtils.Register<DateOutput, Style>(nameof(TextStyle));

        #endregion

        #region Constructors

        static DateOutput()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DateOutput),
                new FrameworkPropertyMetadata(typeof(DateOutput)));
        }

        public DateOutput()
        {

        }

        #endregion

        #region Functions


        #endregion

        #region Events Handlers



        #endregion

        #region Properties

        [Bindable]
        public DateTime? Value { get; set; }

        [Bindable]
        public Style TextStyle { get; set; }

        #endregion
    }
}
