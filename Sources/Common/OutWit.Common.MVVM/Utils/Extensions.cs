using OutWit.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using OutWit.Common.Utils;

namespace OutWit.Common.MVVM.Utils
{
    public static class Extensions
    {
        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteObject(IntPtr hObject);

        #region Classes

        public class E { }

        #endregion

        public static Binding CreateBinding<T, TResult>(this T me,  Expression<Func<T, TResult>> expression, IValueConverter converter = null, object converterParameter = null)
		{
			var member = expression.Body as MemberExpression;

			if (member == null || member.Member.MemberType != MemberTypes.Property)
				throw new ExceptionOf<E>($"{expression} is not a property");

			var path = CreateBinding(member, "");

			return new Binding{Source = me, Path = new PropertyPath(path.Substring(0, path.Length - 1)), Converter = converter, ConverterParameter = converterParameter};
		}

        public static Binding CreateBinding<T, TResult>(this T me, Expression<Func<T, TResult>> expression, BindingMode mode, IValueConverter converter = null, object converterParameter = null)
        {
            var member = expression.Body as MemberExpression;

            if (member == null || member.Member.MemberType != MemberTypes.Property)
                throw new ExceptionOf<E>($"{expression} is not a property");

            var path = CreateBinding(member, "");

            return new Binding { Source = me, Path = new PropertyPath(path.Substring(0, path.Length - 1)), Converter = converter, ConverterParameter = converterParameter, Mode = mode};
        }

        public static Binding CreateBinding<T, TResult>(this T me, Expression<Func<T, TResult>> expression, BindingMode mode, object fallbackValue, IValueConverter converter = null, object converterParameter = null)
        {
            var member = expression.Body as MemberExpression;

            if (member == null || member.Member.MemberType != MemberTypes.Property)
                throw new ExceptionOf<E>($"{expression} is not a property");

            var path = CreateBinding(member, "");

            return new Binding { Source = me, FallbackValue = fallbackValue, Path = new PropertyPath(path.Substring(0, path.Length - 1)), Converter = converter, ConverterParameter = converterParameter, Mode = mode };
        }

        public static Binding CreateBinding<T, TResult>(this T me, Expression<Func<T, TResult>> expression,  object fallbackValue, IValueConverter converter = null, object converterParameter = null)
        {
            var member = expression.Body as MemberExpression;

            if (member == null || member.Member.MemberType != MemberTypes.Property)
                throw new ExceptionOf<E>($"{expression} is not a property");

            var path = CreateBinding(member, "");

            return new Binding { Source = me, FallbackValue = fallbackValue, Path = new PropertyPath(path.Substring(0, path.Length - 1)), Converter = converter, ConverterParameter = converterParameter};
        }

        public static Binding CreateBinding<T, TResult>(this T me, Expression<Func<T, TResult>> expression, int itemIndex, IValueConverter converter = null, object converterParameter = null)
        {
            var member = expression.Body as MemberExpression;

            if (member == null || member.Member.MemberType != MemberTypes.Property)
                throw new ExceptionOf<E>($"{expression} is not a property");

            var path = CreateBinding(member, "");

            return new Binding
            {
                Source = me,
                Path = new PropertyPath($"{path.Substring(0, path.Length - 1)}[{itemIndex}]"),
                Converter = converter,
                ConverterParameter = converterParameter
            };
        }

        private static string CreateBinding(MemberExpression expression, string path)
		{
			if (expression == null || expression.Member.MemberType != MemberTypes.Property)
				return path;

			return CreateBinding(expression.Expression as MemberExpression, path.Insert(0, $"{expression.Member.Name}."));
		}

        //public static BitmapSource ToBitmapSource(this System.Drawing.Bitmap source)
        //      {
        //          BitmapSource bitSrc = null;

        //          var hBitmap = source.GetHbitmap();

        //          try
        //          {
        //              bitSrc = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
        //                  hBitmap,
        //                  IntPtr.Zero,
        //                  Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight(source.Width, source.Height));
        //          }
        //          catch (Win32Exception)
        //          {
        //              bitSrc = null;
        //          }
        //          finally
        //          {
        //              DeleteObject(hBitmap);
        //          }

        //          return bitSrc;
        //      }

        public static bool IsProperty<T, TResult>(this DependencyPropertyChangedEventArgs me, Expression<Func<T, TResult>> propertyExpression)
        {
            return me.Property.Name == propertyExpression.NameOfProperty();
        }

        public static void FirePropertyChanged<T, TResult>(this T me, Expression<Func<T, TResult>> propertyExpression)
            where T : INotifyPropertyChanged
        {
            PropertyChangedUtils.FirePropertyChanged(propertyExpression.NameOfProperty(), me);
        }

        public static void Refresh<T>(this T me) where T : INotifyPropertyChanged
	    {
            PropertyChangedUtils.FirePropertyChanged("", me);
	    }

        public static ObservableCollection<StringHolder> ToObservable(this IReadOnlyList<string> values)
        {
            if (values == null || values.Count == 0)
                return new ObservableCollection<StringHolder>();

            return new ObservableCollection<StringHolder>(values.Select(x => (StringHolder)x));
        }
    }
}
