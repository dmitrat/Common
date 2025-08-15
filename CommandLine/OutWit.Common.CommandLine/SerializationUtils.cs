using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace OutWit.Common.CommandLine
{
    public static class SerializationUtils
    {
        private const string COMMAND_LINE_EXPRESSION = "\"[^\"]*\"|\\S+";

        public static string SerializeCommandLine<T>(this T me)
        {
            var arguments = new List<string>();
            foreach (var property in me.GetType().GetProperties())
            {
                var optionAttribute = property.GetCustomAttribute<OptionAttribute>();
                if (optionAttribute == null)
                    continue;

                var value = property.GetValue(me);
                if (value == null)
                    continue;

                var key = !string.IsNullOrEmpty(optionAttribute.LongName)
                    ? $"--{optionAttribute.LongName}"
                    : $"-{optionAttribute.ShortName}";

                if (property.PropertyType == typeof(bool) && (bool)value)
                    arguments.Add(key);
                else if(property.PropertyType != typeof(bool))
                    arguments.Add($"{key} {Check(value)}");
            }

            return string.Join(" ", arguments.Where(arg => arg != null));
        }

        public static T DeserializeCommandLine<T>(this string me)
        {
            if(string.IsNullOrEmpty(me))
                return default;

            var regex = new Regex(COMMAND_LINE_EXPRESSION);

            var matches = regex.Matches(me)
                .Cast<Match>()
                .Select(m =>
                {
                    var value = m.Value;
                    if (value.StartsWith("\"") && value.EndsWith("\""))
                    {
                        return value.Substring(1, value.Length - 2);
                    }
                    return value;
                }).ToArray();
            
            return matches.DeserializeCommandLine<T>();
        }

        public static T DeserializeCommandLine<T>(this string[] me)
        {
            if(me == null)
                return default;

            try
            {
                return Parser.Default.ParseArguments<T>(me).Value;
            }
            catch (Exception e)
            {
                return default;
            }
        }

        private static string Check(object value)
        {
            var valueString = value.ToString();
            
            return valueString.Contains(" ") 
                ? $"\"{valueString}\"" 
                : valueString;
        }
    }
}
