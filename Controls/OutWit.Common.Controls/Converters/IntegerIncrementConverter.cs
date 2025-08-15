using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace OutWit.Common.Controls.Converters
{
    public class IntegerIncrementConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is int) || parameter == null)
                return value;

            if (!int.TryParse($"{parameter}", out var increment))
                return value;

            return (int) value + increment;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is int) || parameter == null)
                return value;

            if (!int.TryParse($"{parameter}", out var increment))
                return value;

            return (int)value - increment;
        }
    }
}
