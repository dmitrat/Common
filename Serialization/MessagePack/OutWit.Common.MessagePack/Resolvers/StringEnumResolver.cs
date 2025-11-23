using System;
using MessagePack;
using MessagePack.Formatters;
using OutWit.Common.Enums;
using OutWit.Common.MessagePack.Formatters;

namespace OutWit.Common.MessagePack.Resolvers
{
    /// <summary>
    /// A dynamic resolver that automatically finds and provides formatters 
    /// for any type inheriting from StringEnum.
    /// </summary>
    public class StringEnumResolver : IFormatterResolver
    {
        // Singleton instance
        public static readonly StringEnumResolver Instance = new();

        private StringEnumResolver()
        {
        }

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            return FormatterCache<T>.Formatter;
        }

        // Static generic cache ensures reflection runs only once per Type T
        private static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T> Formatter;

            static FormatterCache()
            {
                var t = typeof(T);

                // Check if T inherits from StringEnum<T>
                if (IsStringEnum(t))
                {
                    // Create instance of StringEnumFormatter<T>
                    var formatterType = typeof(StringEnumFormatter<>).MakeGenericType(t);
                    Formatter = (IMessagePackFormatter<T>)Activator.CreateInstance(formatterType)!;
                }
            }
        }

        private static bool IsStringEnum(Type type)
        {
            while (type != null && type != typeof(object))
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(StringEnum<>))
                {
                    return true;
                }
                type = type.BaseType!;
            }
            return false;
        }
    }
}
