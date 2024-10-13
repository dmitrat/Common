using System;
using System.Globalization;
using System.Windows.Data;

namespace OutWit.Common.Controls.Converters
{
    public class DateTimeTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is DateTime))
                return value;

            var time = (DateTime) value;

            return $"{time:t}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
