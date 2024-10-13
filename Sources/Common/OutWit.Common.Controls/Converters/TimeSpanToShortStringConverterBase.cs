using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using OutWit.Common.Interfaces;
using OutWit.Common.Values;

namespace OutWit.Common.Controls.Converters
{
    public class TimeSpanToShortStringConverterBase : IValueConverter
    {
        protected TimeSpanToShortStringConverterBase(IResources resources)
        {
            Resources = resources;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is TimeSpan))
                return value;

            var duration = (TimeSpan) value;

            if (duration.TotalDays > 1)
                return $"{Math.Round(duration.TotalDays)} {Resources["Days"]}";

            if (duration.TotalDays.Is(1.0))
                return $"{Math.Round(duration.TotalDays)} {Resources["Day"]}";


            if (duration.TotalHours > 1)
                return $"{Math.Round(duration.TotalHours)} {Resources["Hours"]}";

            if (duration.TotalHours.Is(1.0))
                return $"{Math.Round(duration.TotalHours)} {Resources["Hour"]}";


            if (duration.TotalMinutes > 1)
                return $"{Math.Round(duration.TotalMinutes)} {Resources["Minutes"]}";

            if (duration.TotalMinutes.Is(1.0))
                return $"{Math.Round(duration.TotalMinutes)} {Resources["Minute"]}";


            if (duration.TotalSeconds > 1)
                return $"{Math.Round(duration.TotalSeconds)} {Resources["Seconds"]}";

            if (duration.TotalSeconds.Is(1.0))
                return $"{Math.Round(duration.TotalSeconds)} {Resources["Second"]}";


            if (duration.TotalMilliseconds > 1)
                return $"{Math.Round(duration.TotalMilliseconds)} {Resources["Milliseconds"]}";

            if (duration.TotalMilliseconds.Is(1.0))
                return $"{Math.Round(duration.TotalMilliseconds)} {Resources["Millisecond"]}";

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }

        private IResources Resources { get; }
    }
}
