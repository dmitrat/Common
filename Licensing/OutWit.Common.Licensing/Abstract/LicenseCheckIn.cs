using System.Text.Json.Serialization;
using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Common.Licensing.Abstract
{
    /// <summary>
    /// Opt-in periodic confirmation that a licence is still active in the
    /// issuing registry — the only mechanism by which an offline licence can be
    /// genuinely revoked.
    /// <para>
    /// <b>Absent means the product contacts nothing, ever.</b> That is the
    /// default, and it is what keeps an air-gapped deployment fully functional
    /// forever. Present, it is chosen per licence at issue time, so a customer
    /// who bought on-prem for isolation is never surprised by it.
    /// </para>
    /// <para>
    /// The grace window is not a courtesy. Without it, a network outage or a
    /// holiday weekend would take down a production cluster over a check that
    /// was only ever meant to catch a cancelled contract.
    /// </para>
    /// </summary>
    public sealed class LicenseCheckIn : ModelBase
    {
        #region Constants

        public const int DEFAULT_EVERY_DAYS = 7;
        public const int DEFAULT_GRACE_DAYS = 30;

        #endregion

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not LicenseCheckIn other)
                return false;

            return Url.Is(other.Url)
                   && EveryDays.Is(other.EveryDays)
                   && GraceDays.Is(other.GraceDays);
        }

        public override LicenseCheckIn Clone()
        {
            return new LicenseCheckIn
            {
                Url = Url,
                EveryDays = EveryDays,
                GraceDays = GraceDays
            };
        }

        #endregion

        #region Properties

        /// <summary>Registry endpoint to confirm against.</summary>
        [JsonPropertyName("url")]
        public string Url { get; init; } = string.Empty;

        /// <summary>How often to confirm, in days.</summary>
        [JsonPropertyName("everyDays")]
        public int EveryDays { get; init; } = DEFAULT_EVERY_DAYS;

        /// <summary>
        /// How many days of continuous failure to tolerate before the licence
        /// stops being honoured.
        /// </summary>
        [JsonPropertyName("graceDays")]
        public int GraceDays { get; init; } = DEFAULT_GRACE_DAYS;

        #endregion
    }
}
