using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace OutWit.Common.Controls.Bars
{
    public class NavigationBar : ListBox
    {
        #region Constructors

        static NavigationBar()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NavigationBar),
                new FrameworkPropertyMetadata(typeof(NavigationBar)));
        }

        public NavigationBar()
        {
        }

        #endregion
    }
}
