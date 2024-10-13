using System;
using System.Globalization;
using System.Windows.Data;
using OutWit.Common.Interfaces;

namespace OutWit.Common.Controls.Converters
{
    public abstract class IntegerToStringConverterBase : IValueConverter
    {
        protected IntegerToStringConverterBase(IResources resources)
        {
            Resources = resources;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var stringKey = "";

            if (parameter is string parameterStr && !string.IsNullOrEmpty(parameterStr))
                stringKey = parameterStr;

            if (string.IsNullOrEmpty(stringKey) || value == null)
                return null;

            try
            {
                return string.Format(Resources[stringKey], value);
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

        private IResources Resources { get; }
    }
}
