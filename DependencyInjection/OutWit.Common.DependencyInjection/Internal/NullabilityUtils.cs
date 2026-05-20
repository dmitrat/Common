using System;
using System.Linq;
using System.Reflection;

namespace OutWit.Common.DependencyInjection.Internal
{
    /// <summary>
    /// Utilities for detecting property nullability at runtime.
    /// </summary>
    internal static class NullabilityUtils
    {
        #region Functions

        /// <summary>
        /// Determines whether the specified property is nullable.
        /// Handles both nullable value types (<c>T?</c>) and nullable reference types.
        /// </summary>
        public static bool IsNullable(PropertyInfo property)
        {
            if (property.PropertyType.IsValueType)
                return Nullable.GetUnderlyingType(property.PropertyType) != null;

#if NET6_0_OR_GREATER
            return IsNullableModern(property);
#else
            return IsNullableLegacy(property);
#endif
        }

        #endregion

        #region Tools

#if NET6_0_OR_GREATER
        private static bool IsNullableModern(PropertyInfo property)
        {
            var context = new NullabilityInfoContext();
            var info = context.Create(property);
            return info.WriteState == NullabilityState.Nullable
                   || info.ReadState == NullabilityState.Nullable;
        }
#else
        private static bool IsNullableLegacy(PropertyInfo property)
        {
            // Compiler emits NullableAttribute(2) on a nullable reference property.
            var nullableAttr = property.CustomAttributes
                .FirstOrDefault(a => a.AttributeType.FullName ==
                    "System.Runtime.CompilerServices.NullableAttribute");

            if (nullableAttr != null)
            {
                var args = nullableAttr.ConstructorArguments;
                if (args.Count == 1)
                {
                    if (args[0].Value is byte b)
                        return b == 2;

                    if (args[0].Value is System.Collections.ObjectModel.ReadOnlyCollection<CustomAttributeTypedArgument> col
                        && col.Count > 0 && col[0].Value is byte firstByte)
                        return firstByte == 2;
                }
            }

            // Fallback to [NullableContext(2)] on the declaring type.
            var contextAttr = property.DeclaringType?.CustomAttributes
                .FirstOrDefault(a => a.AttributeType.FullName ==
                    "System.Runtime.CompilerServices.NullableContextAttribute");

            if (contextAttr != null)
            {
                var args = contextAttr.ConstructorArguments;
                if (args.Count == 1 && args[0].Value is byte contextByte)
                    return contextByte == 2;
            }

            return false;
        }
#endif

        #endregion
    }
}
