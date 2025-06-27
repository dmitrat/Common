using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using AspectInjector.Broker;
using Microsoft.Extensions.Logging;
using OutWit.Common.Utils;
using Serilog;

namespace OutWit.Common.Logging.Aspects
{
    [Injection(typeof(MeasureAspect))]
    public class MeasureAttribute : Attribute
    {
        public MeasureAttribute(LogLevel logLevel = LogLevel.Warning)
        {
            LogLevel = logLevel;
        }

        public LogLevel LogLevel { get; }
    }

    [Aspect(Scope.Global)]
    public class MeasureAspect
    {
        [Advice(Kind.Around, Targets = Target.Method)]
        public object HandleMethod([Argument(Source.Type)] Type type, [Argument(Source.Name)] string name, [Argument(Source.Arguments)] object[] arguments,
            [Argument(Source.Target)] Func<object[], object> method, [Argument(Source.Metadata)] MethodBase metadata, [Argument(Source.Triggers)] Attribute[] injections)
        {
            var attribute = injections.OfType<MeasureAttribute>().Single();

            var start = DateTime.Now;

            try
            {
                return method(arguments);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while executing {type}.{name}{parameters}",type.Name, name, 
                    FormatArguments(arguments, metadata));
                throw;
            }
            finally
            {
                var end = DateTime.Now;

                switch (attribute.LogLevel)
                {
                    case LogLevel.Warning:
                        Log.Warning($"{type.Name}.{name} duration: {(end - start).TotalMilliseconds} ms"); break;

                    case LogLevel.Error:
                        Log.Warning($"{type.Name}.{name} duration: {(end - start).TotalMilliseconds} ms"); break;

                    case LogLevel.Information:
                        Log.Warning($"{type.Name}.{name} duration: {(end - start).TotalMilliseconds} ms"); break;

                    case LogLevel.Critical:
                        Log.Warning($"{type.Name}.{name} duration: {(end - start).TotalMilliseconds} ms"); break;

                    case LogLevel.Debug:
                        Log.Warning($"{type.Name}.{name} duration: {(end - start).TotalMilliseconds} ms"); break;

                    case LogLevel.Trace:
                        Log.Warning($"{type.Name}.{name} duration: {(end - start).TotalMilliseconds} ms"); break;
                }
                
            }
        }

        private static string FormatPropertyChangedArguments(object[] arguments)
        {
            return $"(property: {(arguments[1] as PropertyChangedEventArgs)?.PropertyName ?? "NULL"})";
        }

        private static string FormatArguments(object[] arguments, MethodBase metadata)
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
