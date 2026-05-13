# OutWit.Common.Logging.Loki

Grafana Loki backend for [`OutWit.Common.Logging.Query`](../OutWit.Common.Logging.Query/).

## Contents

- `LokiHttpClient` — typed HttpClient over `/loki/api/v1/*`. Handles basic auth and the `X-Scope-OrgID` multi-tenancy header.
- `LokiOptions` — `BaseUrl`, `TenantId`, basic-auth creds, `DefaultLabels` (always applied as stream selectors), `MaxResultLimit`, `MaxRange`.
- `LokiLogQueryProvider : ILogQueryProvider` — translates `LogQuery` to LogQL via `LogQL/LogQlBuilder`, parses the streams / matrix responses back to `LogEntry` and `LogStatistics`.
- `Response/*` — JSON DTOs.

## LogQL mapping

| Neutral query | LogQL output |
|---|---|
| `DefaultLabels = { service_name = "WitIdentity" }` | `{service_name="WitIdentity"}` |
| `LogFilter` on stream label (`service.name`, `level`, …) | folded into the stream selector |
| `LogFilter` on JSON attribute | `... \| json \| attr op "value"` |
| `FullTextSearch = "passkey"` | `... \|~ "passkey"` |
| `From..To` | `start=<unix-ns>&end=<unix-ns>` |
| `PageSize` | `&limit=N` (capped to `LokiOptions.MaxResultLimit`) |
| `SortOrder` | `&direction=forward|backward` |

For `GetStatisticsAsync`, the provider issues `sum by (level) (count_over_time({...} | json [range]))` and aggregates per-level counts (Trace/Debug → DebugCount; Error/Critical/Fatal → ErrorCount).

## Known limitations

| Method | Behavior |
|---|---|
| `FindOffsetAsync` | Loki has no row-offset concept — returns `-1`. UI callers should narrow the time window instead. |
| `GetStorageInfoAsync` | Loki's HTTP API doesn't expose ingestion volume. Returns an all-null `LogStorageInfo`. Operators wanting quota data should integrate with the Loki admin metrics endpoint or Prometheus directly. |
| `GetDistinctValuesAsync` | Backed by `/loki/api/v1/label/{name}/values` — works for stream labels (cheap) but not for arbitrary JSON-body attributes. |

## Usage

```csharp
services.AddSingleton(new LokiOptions
{
    BaseUrl = "http://loki:3100",
    DefaultLabels = new Dictionary<string, string> { ["service_name"] = "WitIdentity" }
});
services.AddHttpClient<LokiHttpClient>();
services.AddSingleton<ILogQueryProvider, LokiLogQueryProvider>();
```

## License

Apache 2.0 — see `LICENSE`.
