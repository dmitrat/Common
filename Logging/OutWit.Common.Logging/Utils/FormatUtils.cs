using OutWit.Common.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace OutWit.Common.Logging.Utils
{
    internal static class FormatUtils
    {
        public static string FormatPropertyChangedArguments(object[] arguments)
        {
            return $"(property: {(arguments[1] as PropertyChangedEventArgs)?.PropertyName ?? "NULL"})";
        }

        public static string FormatArguments(object[] arguments, MethodBase metadata)
        {
            if (arguments == null || arguments.Length == 0)
                return "()";

            if (arguments.Length == 2 && arguments[1] is PropertyChangedEventArgs)
                return FormatPropertyChangedArguments(arguments);

            var parameters = metadata.GetParameters();

            var str = "";
            for (int i = 0; i < arguments.Length; i++)
            {
                str += $"{parameters[i].Name}: {arguments[i]}, ";
            }

            return $"({str.TrimEnd(2)})";
        }
    }
}
