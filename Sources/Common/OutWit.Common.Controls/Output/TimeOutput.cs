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
    public class TimeOutput : Control
    {
        #region DependencyProperties
        
        public static readonly DependencyProperty ValueProperty = BindingUtils.Register<TimeOutput, DateTime?>(nameof(Value));

        public static readonly DependencyProperty TextStyleProperty = BindingUtils.Register<TimeOutput, Style>(nameof(TextStyle));

        #endregion

        #region Constructors

        static TimeOutput()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TimeOutput),
                new FrameworkPropertyMetadata(typeof(TimeOutput)));
        }

        public TimeOutput()
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
