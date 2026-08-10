using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using OutWit.Common.Licensing.Abstract;

namespace OutWit.Common.Licensing.Crypto
{
    /// <summary>
    /// The wire form of a licence: <c>base64url(header).base64url(payload).base64url(signature)</c>.
    /// <para>
    /// <b>The signature covers the exact bytes of <c>header.payload</c> that the
    /// token itself carries</b> — verification never re-serialises anything.
    /// That single property removes the defining failure of the naive approach,
    /// where a signature is taken over a serialiser's output: there, adding one
    /// field to a model invalidates every licence ever issued, and the workaround
    /// is a string patch that lives forever. Here, adding fields is free and
    /// unknown fields from a newer issuer pass through untouched.
    /// </para>
    /// </summary>
    public sealed class LicenseToken
    {
        #region Constants

        private const int PART_COUNT = 3;

        #endregion

        #region Fields

        private static readonly JsonSerializerOptions JSON_OPTIONS = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        #endregion

        #region Constructors

        private LicenseToken(
            LicenseTokenHeader header,
            LicensePayload payload,
            byte[] signature,
            string signingInput,
            string raw,
            string headerJson,
            string payloadJson)
        {
            Header = header;
            Payload = payload;
            Signature = signature;
            SigningInput = signingInput;
            Raw = raw;
            HeaderJson = headerJson;
            PayloadJson = payloadJson;
        }

        #endregion

        #region Functions

        /// <summary>
        /// Builds the signing input for a header and payload. The caller signs
        /// the returned string and passes both to <see cref="Compose"/>.
        /// </summary>
        public static string BuildSigningInput(LicenseTokenHeader header, LicensePayload payload)
        {
            var encodedHeader = Base64Url.EncodeText(JsonSerializer.Serialize(header, JSON_OPTIONS));
            var encodedPayload = Base64Url.EncodeText(JsonSerializer.Serialize(payload, JSON_OPTIONS));

            return $"{encodedHeader}.{encodedPayload}";
        }

        /// <summary>Appends a signature to a signing input, producing the final token string.</summary>
        public static string Compose(string signingInput, byte[] signature)
        {
            return $"{signingInput}.{Base64Url.Encode(signature)}";
        }

        /// <summary>
        /// Parses a token without verifying it.
        /// <para>
        /// Returns <c>null</c> for anything malformed. Parsing deliberately says
        /// nothing about trust — a parsed token is only a claim, and it becomes a
        /// licence when a validator says so.
        /// </para>
        /// </summary>
        public static LicenseToken? Parse(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            var trimmed = token!.Trim();
            var parts = trimmed.Split('.');

            if (parts.Length != PART_COUNT)
                return null;

            var headerJson = Base64Url.DecodeText(parts[0]);
            var payloadJson = Base64Url.DecodeText(parts[1]);
            var signature = Base64Url.Decode(parts[2]);

            if (headerJson == null || payloadJson == null || signature == null)
                return null;

            try
            {
                var header = JsonSerializer.Deserialize<LicenseTokenHeader>(headerJson, JSON_OPTIONS);
                var payload = JsonSerializer.Deserialize<LicensePayload>(payloadJson, JSON_OPTIONS);

                if (header == null || payload == null)
                    return null;

                return new LicenseToken(header, payload, signature, $"{parts[0]}.{parts[1]}", trimmed, headerJson, payloadJson);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public override string ToString()
        {
            return Raw;
        }

        #endregion

        #region Properties

        /// <summary>The decoded header.</summary>
        public LicenseTokenHeader Header { get; }

        /// <summary>The decoded payload. A claim until a validator accepts it.</summary>
        public LicensePayload Payload { get; }

        /// <summary>Raw signature bytes.</summary>
        public byte[] Signature { get; }

        /// <summary>
        /// The exact <c>header.payload</c> string the signature covers, as
        /// received — not rebuilt.
        /// </summary>
        public string SigningInput { get; }

        /// <summary>The token exactly as it arrived.</summary>
        public string Raw { get; }

        /// <summary>The header's JSON, decoded from the token's own bytes.</summary>
        public string HeaderJson { get; }

        /// <summary>
        /// The payload's JSON, decoded from the token's own bytes.
        /// <para>
        /// Carried rather than re-serialised from <see cref="Payload"/>, and the
        /// difference is the whole point of the format: a newer issuer may add
        /// fields, and round-tripping through this build's model would silently
        /// drop exactly the fields somebody inspecting an unfamiliar licence
        /// most needs to see. This is what the signature covers.
        /// </para>
        /// </summary>
        public string PayloadJson { get; }

        #endregion
    }
}
