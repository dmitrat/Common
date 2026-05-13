# OutWit.Common.Logging.Query

Vendor-neutral log query model and provider abstraction. Lets any application talk to a log backend (NewRelic, Loki, on-disk files, …) through a single interface, swapping concrete implementations without touching the consumer code.

## Contents

### Interface

- `ILogQueryProvider` — eight async methods covering:
  - `QueryAsync(LogQuery)` — full query with filters / time / paging
  - `GetLogsAsync(from, to, filters)` / `GetRecentLogsAsync(lookback, filters)` — convenience shortcuts
  - `SearchAsync(text, lookback)` — free-text message search
  - `GetDistinctValuesAsync(from, to, attribute)` — facet values for filter dropdowns
  - `FindOffsetAsync(query, timestamp)` — scroll-to-timestamp
  - `GetStatisticsAsync(from, to, filters)` — per-level counts
  - `GetStorageInfoAsync()` — used bytes / quota / total entries / vendor-specific breakdown

### Model (`OutWit.Common.Logging.Query.Model`)

- `LogQuery` / `LogPage` / `LogEntry` / `LogFilter` (+ factory helpers `LogFilter.Eq/NotEq/Contains/...`)
- `LogStatistics` — per-level counts with computed rates
- `LogStorageInfo` — neutral storage view (`UsedBytes`, `LimitBytes`, `TotalEntries`, `PeriodFrom/To`, `Breakdown` for vendor extras)
- Enums: `LogFilterOperator`, `LogSortOrder`, `LogSeverity` (`Trace`..`Fatal`), `LogAttribute` (canonical names + per-vendor aliases)
- `LogFilters` static helpers — `LevelEquals(LogSeverity)`, `MessageContains(text)`, `ServiceEquals(name)`, etc.

All model types inherit `ModelBase` (value-comparison via `Is()`, immutable updates via `With()`) and are MemoryPack-friendly for WitRPC transport.

## Backends

| Backend | Package |
|---|---|
| NewRelic (NerdGraph / NRQL) | `OutWit.Common.Logging.NewRelic` |
| Grafana Loki (HTTP / LogQL) | `OutWit.Common.Logging.Loki` |
| Local Serilog JSON files | _planned for `OutWit.Shared.Logging.File`_ |

## Usage

```csharp
// Register a vendor-specific impl in DI:
services.AddSingleton<ILogQueryProvider, NewRelicLogQueryProvider>();   // or LokiLogQueryProvider, ...

// Consume the neutral interface anywhere in your app:
public sealed class LogsViewModel
{
    public LogsViewModel(ILogQueryProvider logs) { ... }

    private async Task LoadAsync()
    {
        var page = await m_logs.GetRecentLogsAsync(
            TimeSpan.FromHours(1),
            filters: [LogFilters.LevelAtLeast(LogSeverity.Warning)]);

        Entries = page.Items;
    }
}
```

## License

Apache 2.0 — see `LICENSE`.
