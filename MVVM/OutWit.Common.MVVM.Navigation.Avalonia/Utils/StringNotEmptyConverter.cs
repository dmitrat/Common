using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace OutWit.Common.MVVM.Navigation.Avalonia.Dialogs
{
    /// <summary>
    /// True when a string has something in it. Used by the built-in progress view to hide an
    /// empty status line rather than leave a gap where one would be.
    /// </summary>
    internal sealed class StringNotEmptyConverter : IValueConverter
    {
        #region Static

        public static readonly StringNotEmptyConverter INSTANCE = new();

        #endregion

        #region IValueConverter

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return !string.IsNullOrWhiteSpace(value as string);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}
