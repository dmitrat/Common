using System;
using System.Globalization;
using System.Reflection.Metadata;
using System.Windows.Data;
using OutWit.Common.Interfaces;

namespace OutWit.Common.Controls.Converters
{
    public abstract class StringToResourceConverterBase : IValueConverter
    {
        protected StringToResourceConverterBase(IResources resources)
        {
            Resources = resources;
        }

        public string Convert(string value)
        {
            if (Resources == null || string.IsNullOrEmpty(value))
                return value;

            return Resources[value];
        }

        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (Resources == null)
                return value;

            var stringKey = "";

            if (parameter is string parameterStr && !string.IsNullOrEmpty(parameterStr))
                stringKey = parameterStr;

            else if (value != null && !string.IsNullOrEmpty($"{value}"))
                stringKey = $"{value}";

            return string.IsNullOrEmpty(stringKey) ? null : Resources[stringKey];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }

        private IResources Resources { get; }
    }
}
