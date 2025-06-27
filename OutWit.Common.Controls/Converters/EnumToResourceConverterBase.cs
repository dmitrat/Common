using System;
using System.Globalization;
using System.Windows.Data;
using OutWit.Common.Interfaces;

namespace OutWit.Common.Controls.Converters
{
    public abstract class EnumToResourceConverterBase : IValueConverter
    {

        protected EnumToResourceConverterBase(IResources resources)
        {
            Resources = resources;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (Resources == null)
                return Binding.DoNothing;

            if (!(value is Enum))
                return value;

            return Resources[Enum.GetName(value.GetType(), value)];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }

        private IResources Resources { get; }
    }
}
