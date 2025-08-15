using System;
using System.Globalization;
using System.Windows.Data;

namespace OutWit.Common.Controls.Converters
{
    public class DateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is DateTime))
                return value;

            var date = (DateTime) value;

            return $"{date:g}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
