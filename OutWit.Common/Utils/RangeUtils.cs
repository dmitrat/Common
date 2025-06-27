using System;
using System.Collections.Generic;
using System.Text;
using OutWit.Common.Interfaces;

namespace OutWit.Common.Utils
{
    public static class RangeUtils
    {
        public static int Duration(this IRange<int> me)
        {
            return me.To - me.From;
        }

        public static bool Contains(this IRange<int> me, int value)
        {
            return value >= me.From && value < me.To;
        }

        public static int Middle(this IRange<int> me)
        {
            return (int) Math.Round((me.From + me.To) / 2.0);
        }

    }
}
