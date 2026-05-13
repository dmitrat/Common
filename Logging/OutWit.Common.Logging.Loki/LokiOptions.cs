using System;
using System.Collections.Generic;

namespace OutWit.Common.Logging.Loki
{
    /// <summary>
    /// Configuration for a Loki backend. Read by <see cref="LokiHttpClient"/>
    /// and <see cref="LokiLogQueryProvider"/>.
    /// </summary>
    public sealed class LokiOptions
    {
        #region Constants

        private const int DEFAULT_MAX_RESULT_LIMIT = 1000;

        private static readonly TimeSpan DEFAULT_MAX_RANGE = TimeSpan.FromDays(7);

        #endregion

        #region Properties

        /// <summary>
        /// Base URL of the Loki HTTP endpoint, e.g. <c>http://loki:3100</c>.
        /// </summary>
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// Multi-tenant Loki tenant id; sent as the <c>X-Scope-OrgID</c> header.
        /// Leave <c>null</c> for single-tenant deployments.
        /// </summary>
        public string? TenantId { get; set; }

        /// <summary>Basic-auth username (Grafana Cloud, nginx fronting Loki). Optional.</summary>
        public string? Username { get; set; }

        /// <summary>Basic-auth password (paired with <see cref="Username"/>). Optional.</summary>
        public string? Password { get; set; }

        /// <summary>
        /// Stream selectors that are added to every query, e.g.
        /// <c>{ "service_name" = "WitIdentity" }</c>.
        /// </summary>
        public IReadOnlyDictionary<string, string>? DefaultLabels { get; set; }

        /// <summary>Maximum entries per query; Loki rejects requests beyond its server-side cap.</summary>
        public int MaxResultLimit { get; set; } = DEFAULT_MAX_RESULT_LIMIT;

        /// <summary>Maximum time range per query — protects against accidental full-history scans.</summary>
        public TimeSpan MaxRange { get; set; } = DEFAULT_MAX_RANGE;

        #endregion
    }
}
