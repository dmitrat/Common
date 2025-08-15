using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace OutWit.Common.Logging.Utils
{
    public static class LogUtils
    {
        public static void Measure(this ILogger me, string header, Action action, LogLevel level = LogLevel.Warning)
        {
            var start = DateTime.Now;

            try
            {
                action();
            }
            catch (Exception ex)
            {
                me?.LogError(ex, $"{header} failed");
            }

            var end = DateTime.Now;
            me?.Log(level, $"{header} duration: {(end - start).TotalMilliseconds} ms");

        }

        public static void Log(this ILogger me, LogLevel level, string message, params object[] args)
        {
            switch (level)
            {
                case LogLevel.None: me.LogInformation(string.Format(message, args)); break;
                case LogLevel.Critical: me.LogCritical(string.Format(message, args)); break;
                case LogLevel.Error: me.LogError(string.Format(message, args)); break;
                case LogLevel.Warning: me.LogWarning(string.Format(message, args)); break;
                case LogLevel.Information: me.LogInformation(string.Format(message, args)); break;
                case LogLevel.Debug: me.LogDebug(string.Format(message, args)); break;
                case LogLevel.Trace: me.LogTrace(string.Format(message, args)); break;
            }
        }
    }
}
