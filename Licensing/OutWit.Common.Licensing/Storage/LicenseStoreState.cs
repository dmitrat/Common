using System;
using System.Text.Json.Serialization;
using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Common.Licensing.Storage
{
    /// <summary>
    /// The small sidecar the licensing runtime keeps next to the licences.
    /// <para>
    /// Unlike a licence, this is <b>not</b> signed and is written by the product
    /// itself. It holds only what cannot be derived from the licences: when this
    /// installation was first seen, and the furthest point in time it has
    /// already observed.
    /// </para>
    /// </summary>
    public sealed class LicenseStoreState : ModelBase
    {
        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not LicenseStoreState other)
                return false;

            return FirstRunUtc.Is(other.FirstRunUtc)
                   && HighWaterMarkUtc.Is(other.HighWaterMarkUtc);
        }

        public override LicenseStoreState Clone()
        {
            return new LicenseStoreState
            {
                FirstRunUtc = FirstRunUtc,
                HighWaterMarkUtc = HighWaterMarkUtc
            };
        }

        #endregion

        #region Properties

        /// <summary>
        /// When this installation first ran. Anchors the demo term, so deleting
        /// the licences alone does not restart it.
        /// </summary>
        [JsonPropertyName("firstRunUtc")]
        public DateTime FirstRunUtc { get; set; }

        /// <summary>
        /// The furthest instant ever observed. With no network to ask, this is
        /// the only thing standing between an offline licence and a clock wound
        /// back to before it expired.
        /// </summary>
        [JsonPropertyName("highWaterMarkUtc")]
        public DateTime HighWaterMarkUtc { get; set; }

        #endregion
    }
}
