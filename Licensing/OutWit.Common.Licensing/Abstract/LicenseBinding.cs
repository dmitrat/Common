using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using OutWit.Common.Abstract;
using OutWit.Common.Attributes;
using OutWit.Common.Collections;
using OutWit.Common.Licensing.Binding;
using OutWit.Common.Values;

namespace OutWit.Common.Licensing.Abstract
{
    /// <summary>
    /// What a licence is tied to, and how much of it has to still match.
    /// <para>
    /// The threshold is the whole point. Hardware drifts one component at a
    /// time — a replaced network card, a re-imaged disk — and an all-or-nothing
    /// identity turns every such event into a dead licence and a support
    /// ticket. Requiring <see cref="Threshold"/> of the recorded
    /// <see cref="Factors"/> absorbs drift while still failing on a genuinely
    /// different machine.
    /// </para>
    /// </summary>
    public sealed class LicenseBinding : ModelBase
    {
        #region Functions

        /// <summary>A licence tied to nothing — valid on any host.</summary>
        public static LicenseBinding None()
        {
            return new LicenseBinding
            {
                Kind = LicenseBindingKind.None,
                Threshold = 0,
                Factors = Array.Empty<LicenseFactor>()
            };
        }

        #endregion

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not LicenseBinding other)
                return false;

            return Kind.Is(other.Kind)
                   && Threshold.Is(other.Threshold)
                   && Factors.Is(other.Factors);
        }

        public override LicenseBinding Clone()
        {
            return new LicenseBinding
            {
                Kind = Kind,
                Threshold = Threshold,
                Factors = Factors.Select(factor => factor.Clone()).ToList()
            };
        }

        #endregion

        #region Properties

        /// <summary>What kind of thing the licence is tied to.</summary>
        [ToString]
        [JsonPropertyName("kind")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LicenseBindingKind Kind { get; init; } = LicenseBindingKind.None;

        /// <summary>
        /// How many of <see cref="Factors"/> must match. Zero means the binding
        /// imposes nothing.
        /// </summary>
        [ToString("of")]
        [JsonPropertyName("threshold")]
        public int Threshold { get; init; }

        /// <summary>The factors recorded when the licence was issued.</summary>
        [JsonPropertyName("factors")]
        public IReadOnlyList<LicenseFactor> Factors { get; init; } = Array.Empty<LicenseFactor>();

        #endregion
    }
}
