using OutWit.Common.Blazor.WitRPC.Extensions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OutWit.Common.Blazor.WitRPC.Encryption
{
    /// <summary>
    /// JSON converter that handles RSA parameters conversion between JWK format and server format.
    /// - Read: Manually deserializes JWK format (n, e, etc.) to properties with Base64Url to Base64 conversion
    /// - Write: Serializes using C# property names (Modulus, Exponent, etc.)
    /// </summary>
    internal class DualNameJsonConverter<T> : JsonConverter<T> where T : RSAParametersWeb, new()
    {
        // Map from JWK names to property names
        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize<T>(ref reader);
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            foreach (var property in typeof(T).GetProperties())
            {
                if (property.CanRead)
                {
                    var propertyValue = AdjustBase64(property.GetValue(value));
                    writer.WritePropertyName(property.Name);
                    JsonSerializer.Serialize(writer, propertyValue, property.PropertyType, options);
                }
            }

            writer.WriteEndObject();
        }

        private object? AdjustBase64(object? value)
        {
            if (value == null)
                return value;

            if (value is not string urlString)
                return value;

            if (string.IsNullOrEmpty(urlString))
                return value;

            return urlString.Base64UrlToBase64();

        }
    }
}
