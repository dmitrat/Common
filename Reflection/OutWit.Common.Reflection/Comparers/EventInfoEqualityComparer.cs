using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Common.Reflection.Comparers
{
    public class EventInfoEqualityComparer : IEqualityComparer<EventInfo>
    {
        public bool Equals(EventInfo? x, EventInfo? y)
        {
            // If both are null, they are equal. If one is null, they are not.
            if (x is null) 
                return y is null;
            
            if (y is null) 
                return false;

            // Use reference equality as a quick check.
            if (ReferenceEquals(x, y)) return true;

            // Consider them equal if the name and the handler type are the same.
            return x.Name == y.Name && x.EventHandlerType == y.EventHandlerType;
        }

        public int GetHashCode([DisallowNull] EventInfo obj)
        {
            // Generate a hash code based on the properties used for equality comparison.
            return HashCode.Combine(obj.Name, obj.EventHandlerType);
        }
    }
}
