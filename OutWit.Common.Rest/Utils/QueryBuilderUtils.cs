using OutWit.Common.Exceptions;
using OutWit.Common.Rest.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OutWit.Common.Rest.Utils
{
    public static class QueryBuilderUtils
    {
        #region Constants

        private const char DOUBLE_QUOTES = '"';

        private const string JSON_MEDIA_TYPE = "application/json";

        private static readonly JsonSerializerOptions DEFAULT_SERIALIZER_OPTIONS = new()
        {
            // Makes property name matching case-insensitive, similar to Newtonsoft's default
            PropertyNameCaseInsensitive = true,
            // Handles enums as strings globally
            Converters = { new JsonStringEnumConverter() }
        };

        private static readonly JsonSerializerOptions CONTENT_SERIALIZER_OPTIONS = new()
        {
            // Equivalent to Newtonsoft's NullValueHandling.Ignore
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

        #endregion


        #region Serialization

        public static async Task<TValue> DeserializeAsync<TValue>(this HttpResponseMessage me)
            where TValue : class
        {
            await using var stream = await me.Content.ReadAsStreamAsync();
            me.EnsureSuccessStatusCode();

            var value = await JsonSerializer.DeserializeAsync<TValue>(stream, DEFAULT_SERIALIZER_OPTIONS);

            if (value == null)
                throw new ExceptionOf<RestClient>("Unable to deserialize JSON response message.");

            return value;
        }

        #endregion

        #region Messages

        public static HttpRequestMessage ToRequestMessage(this IRequestMessage me)
        {
            var message = new HttpRequestMessage
            {
                RequestUri = me.Build(),
                Content = me.BuildContent(),
                Method = me.Method
            };

            message.Headers.Authorization = me.BuildHeader();

            return message;
        }

        #endregion

        #region Content

        public static HttpContent? JsonContent(this object me)
        {
            try
            {
                return new StringContent(JsonSerializer.Serialize(me, CONTENT_SERIALIZER_OPTIONS), Encoding.UTF8, JSON_MEDIA_TYPE);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        #endregion

    }
}
