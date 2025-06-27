using System;
using System.IO;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;

namespace OutWit.Common.Logging
{
    public class SimpleLoggerFormatter
    {
        public string Format<TState>(in LogEntry<TState> logEntry)
        {
            string message = logEntry.Formatter(logEntry.State, logEntry.Exception);
            if (logEntry.Exception == null && message == null)
                return "";

            using var writer = new StringWriter();

            WriteInternal(writer, message, logEntry.LogLevel, logEntry.EventId.Id, logEntry.Exception?.ToString(), logEntry.Category, DateTimeOffset.Now);
            
            return writer.ToString();
        }

        private void WriteInternal(TextWriter textWriter, string message, LogLevel logLevel, int eventId, string exception, string category, DateTimeOffset stamp)
        {
            
            textWriter.Write($"{stamp:d} {stamp:T}");
            textWriter.Write(" ");
            textWriter.Write(GetLogLevelString(logLevel));
            
            textWriter.Write(": ");
            textWriter.Write(category);
            textWriter.Write('[');
            textWriter.Write(eventId.ToString());
            textWriter.Write(']');

            textWriter.Write(' ');
            textWriter.Write(message);

            if (exception != null)
            {
                textWriter.Write(' ');
                textWriter.Write(exception);
            }
            
            textWriter.Write(Environment.NewLine);
        }

        private static string GetLogLevelString(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Trace => "trce",
                LogLevel.Debug => "dbug",
                LogLevel.Information => "info",
                LogLevel.Warning => "warn",
                LogLevel.Error => "fail",
                LogLevel.Critical => "crit",
                _ => throw new ArgumentOutOfRangeException(nameof(logLevel))
            };
        }

    }
}
