using System.Text.Json.Serialization;
using OutWit.Common.Abstract;
using OutWit.Common.Attributes;
using OutWit.Common.Values;

namespace OutWit.Common.Licensing.Abstract
{
    /// <summary>
    /// One binding factor as recorded in a licence: what was observed, and the
    /// hash of what it was.
    /// <para>
    /// Only the hash travels. A licence is not a secret — it is signed, not
    /// encrypted, and an operator is entitled to read what they were granted —
    /// so it must not carry a machine's raw MAC address or host name around in
    /// clear text.
    /// </para>
    /// </summary>
    public sealed class LicenseFactor : ModelBase
    {
        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not LicenseFactor other)
                return false;

            return Key.Is(other.Key)
                   && Hash.Is(other.Hash);
        }

        public override LicenseFactor Clone()
        {
            return new LicenseFactor
            {
                Key = Key,
                Hash = Hash
            };
        }

        #endregion

        #region Properties

        /// <summary>
        /// Factor name — a <c>MachineFactorKeys</c> constant, or a
        /// consumer-defined key such as <c>tenant</c> or <c>installId</c>.
        /// </summary>
        [ToString("key")]
        [JsonPropertyName("k")]
        public string Key { get; init; } = string.Empty;

        /// <summary>SHA-256 of the factor value, lower-case hex.</summary>
        [JsonPropertyName("h")]
        public string Hash { get; init; } = string.Empty;

        #endregion
    }
}
