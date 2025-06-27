using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace OutWit.Common.Controls.Utils
{
    public static class LayoutExtensions
    {
        public static bool Within(this Size me, Size size)
        {
            if (!double.IsInfinity(size.Width) && size.Width < me.Width)
                return false;

            if (!double.IsInfinity(size.Height) && size.Height < me.Height)
                return false;

            return true;
        }
    }
}
