using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using OutWit.Common.Abstract;

namespace OutWit.Common.Utils
{
    public static class PropertiesUtils
    {
        public static TValue With<TValue>(this TValue me, Action<TValue> setter)
            where TValue : ModelBase
        {
            var clone = (TValue)me.Clone();

            setter(clone);

            return clone;
        }

        public static TValue With<TValue, TProperty>(this TValue me, Expression<Func<TValue, TProperty>> propertyExpression, TProperty newValue)
            where TValue : ModelBase
        {
            var member = propertyExpression.Body as MemberExpression;
            
            if (member == null)
                throw new ArgumentException($"Expression is not a property", nameof(propertyExpression));

            var propertyInfo = member.Member as PropertyInfo;
            if (propertyInfo == null)
                throw new ArgumentException("Expression is not a property", nameof(propertyExpression));

            if (!propertyInfo.CanWrite)
                throw new InvalidOperationException($"Property '{propertyInfo.Name}' has no setter and cannot be modified.");

            var clone = (TValue)me.Clone();

            propertyInfo.SetValue(clone, newValue, null);

            return clone;
        }
    }
}
