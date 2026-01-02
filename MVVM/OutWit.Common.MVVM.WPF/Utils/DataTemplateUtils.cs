using System.Windows;
using System.Windows.Controls;

namespace OutWit.Common.MVVM.WPF.Utils
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
