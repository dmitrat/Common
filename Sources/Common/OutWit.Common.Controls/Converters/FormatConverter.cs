using System;
using System.Globalization;
using System.Windows.Data;

namespace OutWit.Common.Controls.Converters
{
    public class FormatConverter : IValueConverter
    {
        public FormatConverter(StringToResourceConverterBase stringToResource = null)
        {
            StringToResource = stringToResource;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var format = "{0}";

            if (parameter is string parameterStr && !string.IsNullOrEmpty(parameterStr))
                format = parameterStr;

            try
            {
                if (StringToResource != null)
                    value = StringToResource.Convert($"{value}", targetType, null, culture);

                return string.Format(format, value);
            }
            catch (Exception e)
            {
                return value;
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }

        private StringToResourceConverterBase StringToResource { get; }
    }
}
