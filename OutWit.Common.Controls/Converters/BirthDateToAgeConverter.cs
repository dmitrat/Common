using System;
using System.Globalization;
using System.Windows.Data;

namespace OutWit.Common.Controls.Converters
{
    public class BirthDateToAgeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is DateTime))
                return value;

            var birthDate = (DateTime) value;

            var now = DateTime.Now;
            if(birthDate < new DateTime(birthDate.Year, now.Month, now.Day))
                return (now.Year - birthDate.Year);

            return (now.Year - birthDate.Year) - 1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
