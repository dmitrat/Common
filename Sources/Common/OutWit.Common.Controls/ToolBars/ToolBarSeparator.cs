using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace OutWit.Common.Controls.ToolBars
{
    public class ToolBarSeparator : ToolBar
    {
        #region Constructors

        static ToolBarSeparator()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ToolBarSeparator),
                new FrameworkPropertyMetadata(typeof(ToolBarSeparator)));
        }

        #endregion
    }
}
