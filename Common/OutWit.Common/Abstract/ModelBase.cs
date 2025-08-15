using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using OutWit.Common.Attributes;

namespace OutWit.Common.Abstract
{
    [DataContract]
    public abstract class ModelBase : NotifyPropertyChangedBase, ICloneable
    {
        #region Constants

        public const double DEFAULT_TOLERANCE = 0.0000001;

        #endregion

        #region Caching

        private static readonly ConcurrentDictionary<Type, List<ModelBasePropertyInfo>> TO_STRING_CACHE
            = new ConcurrentDictionary<Type, List<ModelBasePropertyInfo>>();

        #endregion

        #region ToString

        public override string ToString()
        {
            var type = GetType();

            IReadOnlyCollection<ModelBasePropertyInfo> propertiesToLog = UpdateCache(type);
            if (propertiesToLog.Count == 0)
                return base.ToString();


            var parts = new List<string>();
            foreach (var info in propertiesToLog)
            {
                var value = info.Property.GetValue(this);
                string formattedValue;

                if (value != null && !string.IsNullOrEmpty(info.Format))
                    formattedValue = string.Format($"{{0:{info.Format}}}", value);
                else
                    formattedValue = value?.ToString() ?? "null";
                
                parts.Add($"{info.Name}: {formattedValue}");
            }

            return string.Join(", ", parts);
        }

        private static List<ModelBasePropertyInfo> UpdateCache(Type type)
        {
            if (TO_STRING_CACHE.TryGetValue(type, out List<ModelBasePropertyInfo> propertiesToLog)) 
                return propertiesToLog;
            
            propertiesToLog = new List<ModelBasePropertyInfo>();
            
            IEnumerable<PropertyInfo> properties = type.GetProperties().Where(info => info.CanRead);

            foreach (var info in properties)
            {
                var attribute = info.GetCustomAttribute<ToStringAttribute>();
                if (attribute != null)
                {
                    propertiesToLog.Add(new ModelBasePropertyInfo
                        {
                            Property = info,
                            Name = attribute.Name ?? info.Name,
                            Format = attribute.Format
                        }
                    );
                }
            }

            TO_STRING_CACHE[type] = propertiesToLog;

            return propertiesToLog;
        }

        #endregion

        #region Functions

        public abstract bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE);
        public abstract ModelBase Clone();

        #endregion

        #region ICloneable

        object ICloneable.Clone()
        {
            return Clone();
        }

        #endregion
    }
}

