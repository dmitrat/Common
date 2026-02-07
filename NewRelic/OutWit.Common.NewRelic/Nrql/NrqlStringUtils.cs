using System;
using System.Globalization;
using System.Text.Json;
using OutWit.Common.NewRelic.Model;

namespace OutWit.Common.NewRelic.Nrql
{
#if DEBUG
    public
#else
    internal
#endif
    static class NrqlStringUtils
    {
        public static string ToNrqlTimestamp(DateTime dto)
        {
            return $"'{dto.ToString("yyyy-MM-dd HH:mm:ss +0000", CultureInfo.InvariantCulture)}'";
        }

        public static long ToNrqlEpoch(DateTime dto)
        {
            return new DateTimeOffset(dto.ToUniversalTime()).ToUnixTimeMilliseconds();
        }

        public static string ToNrqlLiteralFromString(string value)
        {
            if (bool.TryParse(value, out var b))
                return b ? "true" : "false";

            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                return d.ToString(CultureInfo.InvariantCulture);

            if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dto))
                return $"'{dto.ToString("yyyy-MM-dd HH:mm:ss +0000", CultureInfo.InvariantCulture)}'"; 

            return $"'{EscapeSingleQuoted(value)}'"; 
        }

        public static string EscapeSingleQuoted(string input)
        {
            return input.Replace("\\", "\\\\").Replace("'", "\\'");
        }

        public static bool EqualsAnyIgnoreCase(this string? value, params string[] options)
        {
            if (value is null)
                return false;

            foreach (var option in options)
            {
                if (value.Equals(option, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public static string? AsString(this JsonElement me)
        {
            switch (me.ValueKind)
            {
                case JsonValueKind.Null:
                    return null;
                case JsonValueKind.False:
                    return "false";
                case JsonValueKind.True:
                    return "true";
                default:
                    return me.ToString();
            }
        }
    }
}
