using System;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Logging.NewRelic.Model;
using OutWit.Common.Logging.Query;
using OutWit.Common.Logging.Query.Model;

namespace OutWit.Common.Logging.NewRelic.Interfaces
{
    /// <summary>
    /// NerdGraph-backed log provider. Implements the neutral
    /// <see cref="ILogQueryProvider"/> contract (used by any consumer that wants
    /// to swap backends) and adds NewRelic-specific extras — currently
    /// <see cref="GetDataConsumptionAsync"/> which surfaces the billing-style
    /// per-data-type breakdown that other backends cannot supply.
    /// </summary>
    public interface INewRelicProvider : ILogQueryProvider
    {
        /// <summary>
        /// Retrieves ACTUAL data consumption from New Relic billing data — what
        /// the operator sees in the Data Management UI. Use it to monitor free
        /// tier usage, month-to-date consumption, projected end-of-month usage,
        /// and the breakdown by data type (Logs / Metrics / Traces / Events).
        /// </summary>
        /// <remarks>
        /// Backed by NRQL <c>FROM NrConsumption SELECT sum(GigabytesIngested) ...</c>.
        /// For backend-neutral storage state, use the
        /// <see cref="ILogQueryProvider.GetStorageInfoAsync"/> method inherited
        /// from <see cref="ILogQueryProvider"/>.
        /// </remarks>
        Task<NewRelicDataConsumption> GetDataConsumptionAsync(
            DateTime from, DateTime to,
            CancellationToken cancellationToken = default);
    }
}
