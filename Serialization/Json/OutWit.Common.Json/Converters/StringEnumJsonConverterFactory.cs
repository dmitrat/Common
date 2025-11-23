using OutWit.Common.Enums;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OutWit.Common.Json.Converters
{
    /// <summary>
    /// Universal JSON converter factory for all types inheriting from StringEnum.
    /// Handles serialization to string and deserialization from string.
    /// </summary>
    public class StringEnumJsonConverterFactory : JsonConverterFactory
    {
        // Determines if this factory can handle the requested type.
        public override bool CanConvert(Type typeToConvert)
        {
            // Check if the type inherits from StringEnum<T>
            return IsStringEnum(typeToConvert);
        }

        // Creates the specific converter instance for the concrete type T.
        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            // Find the specific StringEnum<T> base type
            var baseType = GetStringEnumBase(typeToConvert);
            if (baseType == null)
            {
                throw new InvalidOperationException($"Type {typeToConvert} does not inherit from StringEnum<T>.");
            }

            // Extract the generic argument T (e.g., OrderStatus)
            var enumType = baseType.GetGenericArguments()[0];

            // Create an instance of StringEnumJsonConverter<T>
            var converterType = typeof(StringEnumJsonConverter<>).MakeGenericType(enumType);
            return (JsonConverter)Activator.CreateInstance(converterType);
        }

        private static bool IsStringEnum(Type type) => GetStringEnumBase(type) != null;

        private static Type GetStringEnumBase(Type type)
        {
            while (type != null && type != typeof(object))
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(StringEnum<>))
                {
                    return type;
                }
                type = type.BaseType!;
            }
            return null;
        }

        /// <summary>
        /// The actual worker converter that handles StringEnum logic.
        /// </summary>
        private class StringEnumJsonConverter<T> : JsonConverter<T> where T : StringEnum<T>
        {
            public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.String)
                {
                    throw new JsonException($"Expected string for {typeof(T).Name}, but got {reader.TokenType}.");
                }

                string value = reader.GetString();

                if (value is null)
                {
                    return null;
                }

                // Using TryParse to handle invalid values gracefully (or throw custom JsonException)
                if (StringEnum<T>.TryParse(value, out var result))
                {
                    return result;
                }

                throw new JsonException($"Value '{value}' is not valid for enum {typeof(T).Name}.");
            }

            public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
            {
                if (value is null)
                {
                    writer.WriteNullValue();
                }
                else
                {
                    writer.WriteStringValue(value.Value);
                }
            }
        }
    }
}
