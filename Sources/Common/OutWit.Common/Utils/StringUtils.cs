using System;
using System.Collections.Generic;
using System.Text;

namespace OutWit.Common.Utils
{
    public static class StringUtils
    {
        public static string TrimEnd(this string me, int nSymbols)
        {
            if (string.IsNullOrEmpty(me))
                return null;

            if (nSymbols >= me.Length)
                return "";

            return me.Substring(0, me.Length - nSymbols); }
    }
}
