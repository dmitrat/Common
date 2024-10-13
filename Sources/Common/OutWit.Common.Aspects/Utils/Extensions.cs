using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using OutWit.Common.Exceptions;
using OutWit.Common.Utils;

namespace OutWit.Common.Aspects.Utils
{
    public static class Extensions
    {
        #region Classes

        public class AspectsExtensions { }

        #endregion

        public static bool FirePropertyChanged<T>(this T me, [CallerMemberName] string propertyName = null) where T : INotifyPropertyChanged
        {
            return FirePropertyChanged(propertyName, me);
        }

        public static bool FirePropertyChanged(string propertyName, INotifyPropertyChanged obj)
        {
            var eventDelegate = GetPropertyChangedField(obj.GetType())?.GetValue(obj) as MulticastDelegate;
            if (eventDelegate == null)
                return false;

            var delegates = eventDelegate.GetInvocationList();
            foreach (var dlg in delegates)
                try
                {
                    dlg.Method.Invoke(dlg.Target, new object[] { obj, new PropertyChangedEventArgs(propertyName) });
                }
                catch (TargetInvocationException targetInvocationException)
                {
                    if (targetInvocationException.InnerException != null)
                        throw targetInvocationException.InnerException;
                }

            return true;
        }

        private static FieldInfo GetPropertyChangedField(Type objType)
        {
            while (true)
            {
                //var property = objType.GetField("PropertyChanged", BindingFlags.Instance | BindingFlags.NonPublic);
                var property = objType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic).SingleOrDefault(x => x.FieldType == typeof(PropertyChangedEventHandler));
                if (property != null) 
                    return property;

                if (objType.BaseType?.GetInterface("INotifyPropertyChanged") == null)
                    return null;

                objType = objType.BaseType;
            }
        }

        public static bool IsProperty<T, TResult>(this PropertyChangedEventArgs me, Expression<Func<T, TResult>> propertyExpression)
        {
            return me.PropertyName == propertyExpression.NameOfProperty();
        }

    }
}
