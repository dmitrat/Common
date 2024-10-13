using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using OutWit.Common.Abstract;

namespace OutWit.Common.Values
{
    public static class ValueUtils
    {
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

        #region Byte

        public static bool Is(this byte? me, byte? value)
        {
            if (!me.HasValue && !value.HasValue)
                return true;

            if (!me.HasValue || !value.HasValue)
                return false;

            return Is(me.Value, value.Value);
        }

        public static bool Is(this byte me, byte value)
        {
            return me == value;
        }

        #endregion

        #region SByte

        public static bool Is(this sbyte? me, sbyte? value)
        {
            if (!me.HasValue && !value.HasValue)
                return true;

            if (!me.HasValue || !value.HasValue)
                return false;

            return Is(me.Value, value.Value);
        }

        public static bool Is(this sbyte me, sbyte value)
        {
            return me == value;
        }

        #endregion

        #region Bool

        public static bool Is(this bool? me, bool? value)
        {
            if (!me.HasValue && !value.HasValue)
                return true;

            if (!me.HasValue || !value.HasValue)
                return false;

            return Is(me.Value, value.Value);
        }

        public static bool Is(this bool me, bool value)
        {
            return me == value;
        }

        #endregion

        #region Short

        public static bool Is(this short? me, short? value)
        {
            if (!me.HasValue && !value.HasValue)
                return true;

            if (!me.HasValue || !value.HasValue)
                return false;

            return Is(me.Value, value.Value);
        }

        public static bool Is(this short me, short value)
        {
            return me == value;
        }

        #endregion

        #region UShort

        public static bool Is(this ushort? me, ushort? value)
        {
            if (!me.HasValue && !value.HasValue)
                return true;

            if (!me.HasValue || !value.HasValue)
                return false;

            return Is(me.Value, value.Value);
        }

        public static bool Is(this ushort me, ushort value)
        {
            return me == value;
        }

        #endregion

        #region Int

        public static bool Is(this int? me, int? value)
        {
            if (!me.HasValue && !value.HasValue)
                return true;

            if (!me.HasValue || !value.HasValue)
                return false;

            return Is(me.Value, value.Value);
        }

        public static bool Is(this int me, int value)
        {
            return me == value;
        }

        #endregion

        #region UInt

        public static bool Is(this uint? me, uint? value)
        {
            if (!me.HasValue && !value.HasValue)
                return true;

            if (!me.HasValue || !value.HasValue)
                return false;

            return Is(me.Value, value.Value);
        }

        public static bool Is(this uint me, uint value)
        {
            return me == value;
        }

        #endregion

        #region Long

        public static bool Is(this long? me, long? value)
        {
            if (!me.HasValue && !value.HasValue)
                return true;

            if (!me.HasValue || !value.HasValue)
                return false;

            return Is(me.Value, value.Value);
        }

        public static bool Is(this long me, long value)
        {
            return me == value;
        }

        #endregion

        #region ULong

        public static bool Is(this ulong? me, ulong? value)
        {
            if (!me.HasValue && !value.HasValue)
                return true;

            if (!me.HasValue || !value.HasValue)
                return false;

            return Is(me.Value, value.Value);
        }

        public static bool Is(this ulong me, ulong value)
        {
            return me == value;
        }

        #endregion

        #region String

        public static bool Is(this string me, string value)
        {
            if (me == null && value == null)
                return true;

            if (me == null || value == null)
                return false;

            return me == value;
        } 

        #endregion

        #region DateTime

        public static bool Is(this DateTime? me, DateTime? value)
        {
            if (!me.HasValue && !value.HasValue)
                return true;

            if (!me.HasValue || !value.HasValue)
                return false;

            return Is(me.Value, value.Value);
        }

        public static bool Is(this DateTime me, DateTime value)
        {
            return me.ToString("G") == value.ToString("G");
        }

        #endregion

        #region TimeSpan

        public static bool Is(this TimeSpan? me, TimeSpan? value)
        {
            if (!me.HasValue && !value.HasValue)
                return true;

            if (!me.HasValue || !value.HasValue)
                return false;

            return Is(me.Value, value.Value);
        }

        public static bool Is(this TimeSpan me, TimeSpan value)
        {
            return me.ToString("G") == value.ToString("G");
        }

        #endregion

        #region Guid

        public static bool Is(this Guid? me, Guid? value)
        {
            if (!me.HasValue && !value.HasValue)
                return true;

            if (!me.HasValue || !value.HasValue)
                return false;

            return Is(me.Value, value.Value);
        }

        public static bool Is(this Guid me, Guid value)
        {
            return me.CompareTo(value) == 0;
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

        #region Version

        public static bool Is(this Version me, Version value)
        {
            if (me == null && value == null)
                return true;

            if (me == null || value == null)
                return false;

            return me.CompareTo(value) == 0;
        } 

        #endregion

        public static bool ToBoolean(this uint me)
        {
            return me == 1;
        }

        public static byte FromBoolean(this bool me)
        {
            return me ? (byte)1 : (byte)0;
        }

        public static bool Check(this object me, object value)
        {
            if (me.GetType() != value.GetType())
                return false;

            if (me is ModelBase modelBase)
                return modelBase.Is((ModelBase) value);

            return me.Equals(value);
        }

        public static bool Check<TValue>(this TValue me, TValue value)
            where TValue : ModelBase
        {
            if (me == null && value == null)
                return true;

            if (me == null)
                return false;

            return me.Is(value);
        }

        public static T Min<T>(params T[] values)
        {
            return values.Min();
        }
        public static T Max<T>(params T[] values)
        {
            return values.Max();
        }

        public static TimeSpan Sum(this IEnumerable<TimeSpan> me)
        {
            return TimeSpan.FromMilliseconds(me.Sum(timeSpan => timeSpan.TotalMilliseconds));
        }

        public static DateTime? Max(DateTime? leftDate, DateTime? rightDate)
        {
            if (leftDate == null && rightDate == null)
                return null;

            if (leftDate == null)
                return rightDate;

            if (rightDate == null)
                return leftDate;

            return leftDate > rightDate ? leftDate : rightDate;
        }

        public static DateTime? Min(DateTime? leftDate, DateTime? rightDate)
        {
            if (leftDate == null && rightDate == null)
                return null;

            if (leftDate == null)
                return rightDate;

            if (rightDate == null)
                return leftDate;

            return leftDate < rightDate ? leftDate : rightDate;
        }

        public static int SizeOf<TValue>()
        {
            return Marshal.SizeOf(typeof(TValue));
        }

        public static IReadOnlyList<TValue> AsArray<TValue>(this TValue me)
        {
            return new[] { me };
        }
    }
}
