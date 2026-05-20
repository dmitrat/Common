using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace OutWit.Common.DependencyInjection.Internal
{
    /// <summary>
    /// Locates an <see cref="IServiceProvider"/> stored as an instance field on a target object.
    /// Used by <see cref="InjectAspect"/> when the mixin <see cref="IInjectable.ServiceProvider"/>
    /// has not been set explicitly. The looked-up field is cached per type.
    /// </summary>
    internal static class ServiceProviderLocator
    {
        #region Constants

        private static readonly ConcurrentDictionary<Type, FieldInfo?> FIELD_CACHE = new();

        #endregion

        #region Functions

        public static IServiceProvider? Find(object instance)
        {
            var field = FIELD_CACHE.GetOrAdd(instance.GetType(), FindServiceProviderField);
            return field?.GetValue(instance) as IServiceProvider;
        }

        #endregion

        #region Tools

        private static FieldInfo? FindServiceProviderField(Type type)
        {
            var current = type;
            while (current != null && current != typeof(object))
            {
                var field = current.GetFields(
                        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .FirstOrDefault(f => typeof(IServiceProvider).IsAssignableFrom(f.FieldType));

                if (field != null)
                    return field;

                current = current.BaseType;
            }

            return null;
        }

        #endregion
    }
}
