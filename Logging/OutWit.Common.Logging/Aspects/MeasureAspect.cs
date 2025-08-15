using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using AspectInjector.Broker;
using Microsoft.Extensions.Logging;
using OutWit.Common.Logging.Utils;
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
                Log.Error(ex, "Error while executing {type}.{name}{parameters}",type.Name, name, FormatUtils.
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
                        Log.Error($"{type.Name}.{name} duration: {(end - start).TotalMilliseconds} ms"); break;

                    case LogLevel.Information:
                        Log.Information($"{type.Name}.{name} duration: {(end - start).TotalMilliseconds} ms"); break;

                    case LogLevel.Critical:
                        Log.Fatal($"{type.Name}.{name} duration: {(end - start).TotalMilliseconds} ms"); break;

                    case LogLevel.Debug:
                        Log.Debug($"{type.Name}.{name} duration: {(end - start).TotalMilliseconds} ms"); break;

                    case LogLevel.Trace:
                        Log.Verbose($"{type.Name}.{name} duration: {(end - start).TotalMilliseconds} ms"); break;
                }
                
            }
        }
    }
}
