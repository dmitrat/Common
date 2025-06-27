using System;
using System.Windows;

namespace OutWit.Common.Controls.Input
{
    public class DateInput : InputBase<DateTime?>
    {
        #region Constructors

        static DateInput()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DateInput),
                new FrameworkPropertyMetadata(typeof(DateInput)));
        }

        #endregion
    }
}
