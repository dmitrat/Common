using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AspectInjector.Broker;
using OutWit.Common.Utils;
using Serilog;
using Serilog.Events;

namespace OutWit.Common.Logging.Aspects
{
    [Injection(typeof(LogAspect))]
    public class LogAttribute : Attribute
    {
    }

    [Injection(typeof(LogAspect))]
    public class NoLogAttribute : Attribute
    {
    }

    [Aspect(Scope.Global)]
    public class LogAspect
    {
        [Advice(Kind.Around, Targets = Target.Method)]
        public object HandleMethod([Argument(Source.Type)] Type type, [Argument(Source.Name)] string name, [Argument(Source.Arguments)] object[] arguments,
            [Argument(Source.Target)] Func<object[], object> method, [Argument(Source.Metadata)] MethodBase metadata, [Argument(Source.Triggers)] Attribute[] injections)
        {
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
                if (injections?.OfType<NoLogAttribute>().FirstOrDefault() == null)
                {
                    if (IsPropertyChanged(arguments) && Log.IsEnabled(LogEventLevel.Verbose))
                        Log.Information("Executed method {type}.{name}{parameters}",type.Name, name, 
                            FormatPropertyChangedArguments(arguments));

                    if (!IsPropertyChanged(arguments) && Log.IsEnabled(LogEventLevel.Information))
                        Log.Information("Executed method {type}.{name}{parameters}",type.Name, name, 
                            FormatArguments(arguments, metadata));
                }
            }
        }

        private static bool IsPropertyChanged(object[] arguments)
        {
            return arguments.Length == 2 && arguments[1] is PropertyChangedEventArgs;
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
