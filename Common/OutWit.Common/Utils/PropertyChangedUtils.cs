using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using OutWit.Common.Exceptions;

namespace OutWit.Common.Utils
{
    public static class PropertyChangedUtils
    {
        #region Constants

        private const string PROPERTY_CHANGED_HANDLER_CANDIDATE = "OnPropertyChanged";

        #endregion

        #region Cache

        private static readonly ConcurrentDictionary<Type, Action<INotifyPropertyChanged, string>> INVOKER_CACHE
            = new ConcurrentDictionary<Type, Action<INotifyPropertyChanged, string>>();

        #endregion

        #region FirePropertyChanget

        public static bool FirePropertyChanged<T>(this T me, [CallerMemberName] string propertyName = null) 
            where T : INotifyPropertyChanged
        {
            return FirePropertyChanged(propertyName, me);
        }

        public static bool FirePropertyChanged(string propertyName, INotifyPropertyChanged obj)
        {
            if (obj == null || string.IsNullOrEmpty(propertyName))
                return false;

            Action<INotifyPropertyChanged, string> invoker = INVOKER_CACHE.GetOrAdd(obj.GetType(), CreateInvoker);
            invoker?.Invoke(obj, propertyName);

            return true;
        }

        #endregion

        #region Create Invoker

        private static Action<INotifyPropertyChanged, string> CreateInvoker(Type type)
        {
            MethodInfo handler = FindOnPropertyChangedHandler(type);
            if (handler != null)
                return CreateInvoker(type, handler);

            FieldInfo field = FindPropertyChangedField(type);
            if (field != null)
                return CreateInvoker(field);

            return (instance, name) => { }; ;
        }

        private static Action<INotifyPropertyChanged, string> CreateInvoker(Type type, MethodInfo handler)
        {
            var instanceParameter = Expression.Parameter(typeof(INotifyPropertyChanged), "instance");
            var nameParameter = Expression.Parameter(typeof(string), "name");
            var call = Expression.Call(Expression.Convert(instanceParameter, type), handler, nameParameter);
            
            return Expression.Lambda<Action<INotifyPropertyChanged, string>>(call, instanceParameter, nameParameter).Compile();
        }

        private static Action<INotifyPropertyChanged, string> CreateInvoker(FieldInfo field)
        {
            return (instance, name) =>
            {
                if (field.GetValue(instance) is MulticastDelegate eventDelegate)
                {
                    var eventArgs = new PropertyChangedEventArgs(name);
                    foreach (var handler in eventDelegate.GetInvocationList())
                    {
                        IReadOnlyList<ParameterInfo> parameters = handler.Method.GetParameters();
                        
                        if (parameters.Count == 2)
                            handler.Method.Invoke(handler.Target, new object[] { instance, eventArgs });
                        else if (parameters.Count == 3)
                            handler.Method.Invoke(handler.Target, new object[] { handler.Target, instance, eventArgs });
                    }
                }
            };
        }

        #endregion

        #region Find Property Changed

        private static MethodInfo FindOnPropertyChangedHandler(Type type)
        {
            return type.GetMethods().FirstOrDefault(info =>
                info.Name == PROPERTY_CHANGED_HANDLER_CANDIDATE &&
                info.ReturnType == typeof(void) &&
                info.GetParameters().Length == 1 &&
                info.GetParameters()[0].ParameterType == typeof(string)
            );
        }

        private static FieldInfo FindPropertyChangedField(Type objType)
        {
            while (true)
            {
                var field = objType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                    .SingleOrDefault(info => info.FieldType == typeof(PropertyChangedEventHandler));

                if (field != null)
                    return field;

                if (objType.BaseType?.GetInterface(nameof(INotifyPropertyChanged)) == null)
                    return null;

                objType = objType.BaseType;
            }
        }

        #endregion

        #region Tools

        public static bool IsProperty<T, TResult>(this PropertyChangedEventArgs me, Expression<Func<T, TResult>> propertyExpression)
        {
            return me.PropertyName == propertyExpression.NameOfProperty();
        }

        public static string NameOfProperty<T, TResult>(this Expression<Func<T, TResult>> me)
        {
            var member = me.Body as MemberExpression;

            if (member == null || member.Member.MemberType != MemberTypes.Property)
                throw new ArgumentException($"{me} is not a property");

            return member.Member.Name;
        }

        #endregion

    }
}
