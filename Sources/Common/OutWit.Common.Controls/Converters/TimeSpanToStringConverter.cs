using System;
using System.Globalization;
using System.Windows.Data;

namespace OutWit.Common.Controls.Converters
{
    public class TimeSpanToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is TimeSpan))
                return value;

            var time = (TimeSpan) value;

            return $"{Math.Floor(time.TotalHours):00}:{time.Minutes:00}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
