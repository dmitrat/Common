using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using OutWit.Common.MVVM.Aspects;

namespace OutWit.Common.Controls.Windows
{
    public class HeaderWindow : CustomWindow
    {
        #region DependencyProperties

        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(nameof(Header), typeof(Control), typeof(HeaderWindow), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty HeaderStyleProperty = DependencyProperty.Register(nameof(HeaderStyle), typeof(Style), typeof(HeaderWindow), new FrameworkPropertyMetadata(null));

        #endregion

        #region Constructors

        static HeaderWindow()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(HeaderWindow),
                new FrameworkPropertyMetadata(typeof(HeaderWindow)));
        }

        #endregion

        #region Properties

        [Bindable]
        public Control Header { get; set; }

        [Bindable]
        public Style HeaderStyle { get; set; }

        #endregion
    }
}
