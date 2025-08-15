using System;
using System.Collections.Generic;
using System.Text;

namespace OutWit.Common.Search
{
    public static class SearchUtils
    {
        public static int FindLessOrEqualValueIndex(this IList<int> me, int key)
        {
            if (me.Count == 0)
                return -1;

            if (key < me[0])
                return -1;

            if (key >= me[me.Count - 1])
                return me.Count - 1;

            var index = me.FindClosestValueIndex(key);

            if (me[index] > key)
                index = Math.Max(0, index - 1);

            return index;
        }

        public static int FindGreaterOrEqualValueIndex(this IList<int> me, int key)
        {
            if (me.Count == 0)
                return -1;

            if (key <= me[0])
                return 0;

            if (key > me[me.Count - 1])
                return - 1;

            var index = me.FindClosestValueIndex(key);

            if (me[index] < key)
                index = Math.Min(me.Count - 1, index + 1);

            return index;
        }

        public static int FindClosestValueIndex(this IList<int> me, int key)
        {
            if(me.Count == 0)
                return -1;

            int lo = 0;
            int hi = me.Count - 1;

            if (key < me[lo])
                return lo;

            if (key > me[hi])
                return hi;

            while (lo <= hi)
            {
                int mid = (hi + lo) / 2;

                if (key < me[mid])
                    hi = mid - 1;

                else if (key > me[mid])
                    lo = mid + 1;

                else
                    return mid;
            }
            return (me[lo] - key) < (key - me[hi]) ? lo : hi;
        }
    }
}
