using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Common.Reflection.Comparers
{
    public class MethodInfoEqualityComparer : IEqualityComparer<MethodInfo>
    {
        public bool Equals(MethodInfo? x, MethodInfo? y)
        {
            if (x is null) 
                return y is null;
            if (y is null) 
                return false;

            if (ReferenceEquals(x, y)) 
                return true;

            // Compare names. If they are different, methods are not equal.
            if (x.Name != y.Name)
            {
                return false;
            }

            // Get parameters for both methods.
            var xParams = x.GetParameters();
            var yParams = y.GetParameters();

            // If the number of parameters is different, they are not equal.
            if (xParams.Length != yParams.Length)
            {
                return false;
            }

            // Compare parameter types one by one.
            for (int i = 0; i < xParams.Length; i++)
            {
                if (xParams[i].ParameterType != yParams[i].ParameterType)
                {
                    return false;
                }
            }

            // If name and all parameter types match, the methods are considered equal.
            return true;
        }

        public int GetHashCode([DisallowNull] MethodInfo obj)
        {
            // Use the modern HashCode struct to combine hash codes.
            var hash = new HashCode();

            // Start with the method name.
            hash.Add(obj.Name);

            // Add each parameter's type to the hash code calculation.
            foreach (var param in obj.GetParameters())
            {
                hash.Add(param.ParameterType);
            }

            return hash.ToHashCode();
        }
    }
}
