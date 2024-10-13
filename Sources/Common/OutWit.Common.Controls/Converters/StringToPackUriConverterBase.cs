using System;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;

namespace OutWit.Common.Controls.Converters
{
    public abstract class StringToPackUriConverterBase : IValueConverter
    {
        protected StringToPackUriConverterBase(Assembly assembly)
        {
            AssemblyName = assembly.FullName;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(parameter is string))
                return null;

            var imageName = (string)parameter;

            return $"pack://application:,,,/{AssemblyName};component/Images/{imageName}.png";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }

        private string AssemblyName { get; }
    }
}
