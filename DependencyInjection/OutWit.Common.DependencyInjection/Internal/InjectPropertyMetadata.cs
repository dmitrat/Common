using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace OutWit.Common.DependencyInjection.Internal
{
    /// <summary>
    /// Cached reflection metadata for a single <see cref="InjectAttribute"/> property.
    /// Shared between <see cref="InjectAspect"/> (lazy getter advice) and
    /// <see cref="PropertyInjector"/> (eager reflection-based injection) so the
    /// reflection scan happens exactly once per type.
    /// </summary>
    internal sealed class InjectPropertyMetadata
    {
        #region Constants

        private static readonly ConcurrentDictionary<Type, Dictionary<string, InjectPropertyMetadata>> META_CACHE = new();

        #endregion

        #region Constructors

        private InjectPropertyMetadata(PropertyInfo property, Type serviceType, bool isRequired, InjectMode mode)
        {
            Property = property;
            ServiceType = serviceType;
            IsRequired = isRequired;
            Mode = mode;
        }

        #endregion

        #region Properties

        public PropertyInfo Property { get; }

        public Type ServiceType { get; }

        public bool IsRequired { get; }

        public InjectMode Mode { get; }

        #endregion

        #region Factory

        /// <summary>
        /// Returns the cached map of property-name → metadata for the given type,
        /// scanning the type if necessary on first call.
        /// </summary>
        public static Dictionary<string, InjectPropertyMetadata> GetMap(Type type)
        {
            return META_CACHE.GetOrAdd(type, BuildMap);
        }

        private static Dictionary<string, InjectPropertyMetadata> BuildMap(Type type)
        {
            var result = new Dictionary<string, InjectPropertyMetadata>();

            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            foreach (var prop in type.GetProperties(flags))
            {
                var attr = prop.GetCustomAttribute<InjectAttribute>(inherit: true);
                if (attr == null)
                    continue;

                // Inherited auto-properties with a private setter reflect as
                // non-writable from the derived type; re-fetch via DeclaringType
                // to get the writable accessor.
                var writableProp = prop.CanWrite
                    ? prop
                    : prop.DeclaringType?.GetProperty(prop.Name, flags);

                if (writableProp == null || !writableProp.CanWrite)
                    throw new InvalidOperationException(
                        $"[Inject] property '{type.Name}.{prop.Name}' must have a setter (public or private).");

                bool isRequired = attr.Requirement switch
                {
                    InjectRequirement.Required => true,
                    InjectRequirement.Optional => false,
                    _ => !NullabilityUtils.IsNullable(writableProp)
                };

                result[prop.Name] = new InjectPropertyMetadata(
                    writableProp,
                    writableProp.PropertyType,
                    isRequired,
                    attr.Mode);
            }

            return result;
        }

        #endregion
    }
}
