using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using OutWit.Common.Abstract;
using OutWit.Common.Collections;

namespace OutWit.Common.Values
{
    public static class ValueUtils
    {
        #region Generic

        public static bool Is(this IComparable me, IComparable other)
        {
            if (me == null && other == null)
                return true;
            
            if (me == null || other == null)
                return false;

            return me.CompareTo(other) == 0;
        }

        public static bool Is<T>(this T me, T other)
            where T : IComparable<T>
        {
            if (me == null && other == null)
                return true;
            if (me == null || other == null)
                return false;

            return me.CompareTo(other) == 0;
        }

        public static bool Is<T>(this T? me, T? other) 
            where T : struct, IComparable<T>
        {
            if (!me.HasValue && !other.HasValue)
                return true;
            if (!me.HasValue || !other.HasValue)
                return false;

            return me.Value.Is(other.Value);
        }


        #endregion

        #region Decimal

        public static bool Is(this decimal? me, decimal? value, double tolerance = ModelBase.DEFAULT_TOLERANCE)
        {
            if (!me.HasValue && !value.HasValue)
                return true;

            if (!me.HasValue || !value.HasValue)
                return false;

            return Is(me.Value, value.Value, tolerance);
        }

        public static bool Is(this decimal me, decimal value, double tolerance = ModelBase.DEFAULT_TOLERANCE)
        {
            return (double)Math.Abs(me - value) < tolerance;
        }

        #endregion

        #region Double

        public static bool Is(this double? me, double? value, double tolerance = ModelBase.DEFAULT_TOLERANCE)
        {
            if (!me.HasValue && !value.HasValue)
                return true;

            if (!me.HasValue || !value.HasValue)
                return false;

            return Is(me.Value, value.Value, tolerance);
        }

        public static bool Is(this double me, double value, double tolerance = ModelBase.DEFAULT_TOLERANCE)
        {
            return Math.Abs(me - value) < tolerance;
        }

        #endregion

        #region Float

        public static bool Is(this float? me, float? value, double tolerance = ModelBase.DEFAULT_TOLERANCE)
        {
            if (!me.HasValue && !value.HasValue)
                return true;

            if (!me.HasValue || !value.HasValue)
                return false;

            return Is(me.Value, value.Value, tolerance);
        }

        public static bool Is(this float me, float value, double tolerance = ModelBase.DEFAULT_TOLERANCE)
        {
            return Math.Abs(me - value) < tolerance;
        }

        #endregion

        #region Bool

        public static bool ToBoolean(this uint me)
        {
            return me == 1;
        }

        public static byte FromBoolean(this bool me)
        {
            return me ? (byte)1 : (byte)0;
        }

        #endregion

        #region TimeSpan

        public static TimeSpan Sum(this IEnumerable<TimeSpan> me)
        {
            return TimeSpan.FromMilliseconds(me.Sum(timeSpan => timeSpan.TotalMilliseconds));
        }

        #endregion

        #region Enum

        public static bool Is(this Enum me, Enum value)
        {
            if (me == null && value == null)
                return true;

            if (me == null || value == null)
                return false;

            return me.GetType() == value.GetType() && me.Equals(value);
        }

        #endregion

        #region Type

        public static bool Is(this Type me, Type value)
        {
            if (me == null && value == null)
                return true;

            if (me == null || value == null)
                return false;

            return me.AssemblyQualifiedName == value.AssemblyQualifiedName;
        }

        #endregion

        #region Check

        public static bool Check<TValue>(this TValue me, TValue value)
            where TValue : ModelBase
        {
            if (me == null && value == null)
                return true;

            if (me == null)
                return false;

            return me.Is(value);
        }

        public static bool Check(this object me, object second)
        {
            if (me is IDictionary firstDictionary && second is IDictionary secondDictionary)
                return firstDictionary.Check(secondDictionary);

            if (me is ICollection firstCollection && second is ICollection secondCollection)
                return firstCollection.Check(secondCollection);

            if (me is ModelBase firstModel && second is ModelBase secondModel)
                return firstModel.Is(secondModel);

            if (me is IComparable firstComparable && second is IComparable secondComparable)
                return firstComparable.Is(secondComparable);

            if (me is Type fistType && second is Type secondType)
                return fistType.Is(secondType);

            return Equals(me, second);
        }

        #endregion

        #region Min

        public static T Min<T>(params T[] values)
        {
            return values.Min();
        }

        public static T? Min<T>(T? left, T? right) where T : struct, IComparable<T>
        {
            if (left == null && right == null)
                return null;
            
            if (left == null) 
                return right;
            
            if (right == null)
                return left;
            
            return left.Value.CompareTo(right.Value) < 0 
                ? left 
                : right;
        }

        #endregion

        #region Max

        public static T Max<T>(params T[] values)
        {
            return values.Max();
        }

        public static T? Max<T>(T? left, T? right) where T : struct, IComparable<T>
        {
            if (left == null && right == null)
                return null;
            
            if (left == null) 
                return right;
            
            if (right == null)
                return left;
            
            return left.Value.CompareTo(right.Value) > 0 
                ? left
                : right;
        }

        #endregion


        public static int SizeOf<TValue>()
        {
            return Marshal.SizeOf(typeof(TValue));
        }

        public static IReadOnlyList<TValue> AsArray<TValue>(this TValue me)
        {
            return new[] { me };

        }

        #region Tools

#if NET6_0_OR_GREATER
        
        public static bool EqualWith<T>(this T me, T other, Func<T, T, bool> comparer)
        {
            if (me is null && other is null)
                return true;
            if (me is null || other is null)
                return false;
            
            return comparer(me, other);
        }
#endif
#endregion
    }
}
