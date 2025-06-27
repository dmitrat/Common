using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace OutWit.Common.MVVM.Utils
{
    public static class DataTemplateUtils
    {
        public static DataTemplate Create<TData, TControl>()
            where TControl : Control
        {
            return new DataTemplate
            {
                DataType = typeof(TData),
                VisualTree = new FrameworkElementFactory(typeof(TControl))
            };
        }
    }
}
