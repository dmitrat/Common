using System.Text.Json.Serialization;
using OutWit.Common.Abstract;
using OutWit.Common.Licensing.Keys;
using OutWit.Common.Values;

namespace OutWit.Common.Licensing.Abstract
{
    /// <summary>
    /// The token header: which key signed this, and with what.
    /// <para>
    /// <see cref="Algorithm"/> is carried so a verifier can <b>check</b> it
    /// against the key ring — never so it can choose. The algorithm is a
    /// property of the key; a header that disagrees with the registered key is
    /// a rejected licence, not an instruction.
    /// </para>
    /// </summary>
    public sealed class LicenseTokenHeader : ModelBase
    {
        #region Constants

        /// <summary>Token type marker — "OutWit Licence".</summary>
        public const string TOKEN_TYPE = "OWL";

        #endregion

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not LicenseTokenHeader other)
                return false;

            return Algorithm.Is(other.Algorithm)
                   && KeyId.Is(other.KeyId)
                   && Type.Is(other.Type);
        }

        public override LicenseTokenHeader Clone()
        {
            return new LicenseTokenHeader
            {
                Algorithm = Algorithm,
                KeyId = KeyId,
                Type = Type
            };
        }

        #endregion

        #region Properties

        /// <summary>Algorithm the signature claims to use.</summary>
        [JsonPropertyName("alg")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LicenseAlgorithm Algorithm { get; init; } = LicenseAlgorithm.None;

        /// <summary>
        /// Identifier of the signing key. Rotation and per-product-line scoping
        /// both hang off this: without it, one leaked key would compromise every
        /// licence ever issued, for every product, unfixably.
        /// </summary>
        [JsonPropertyName("kid")]
        public string KeyId { get; init; } = string.Empty;

        /// <summary>Token type. Always <see cref="TOKEN_TYPE"/>.</summary>
        [JsonPropertyName("typ")]
        public string Type { get; init; } = TOKEN_TYPE;

        #endregion
    }
}
