# OutWit.Common.NewRelic

## Overview

OutWit.Common.NewRelic is a .NET client library for querying logs and telemetry data from New Relic via the [NerdGraph](https://docs.newrelic.com/docs/apis/nerdgraph/get-started/introduction-new-relic-nerdgraph/) (GraphQL) API. It converts high-level query objects into NRQL, sends them through NerdGraph, and parses the results into strongly-typed models.

Key capabilities:

- **Paginated log retrieval** with offset-based navigation
- **Full-text search** across log messages
- **Strongly-typed filters** — no raw NRQL strings required
- **Facet extraction** (distinct attribute values for filter dropdowns)
- **Log statistics** — counts and severity distribution over a time range
- **Data consumption monitoring** — actual GB ingested, free-tier projections, product-line breakdown

All models are `[MemoryPackable]` for efficient binary serialization (e.g. over WitRPC).

#### Install

```ps1
Install-Package OutWit.Common.NewRelic
```

or

```bash
dotnet add package OutWit.Common.NewRelic
```

## Target Frameworks

`net6.0` · `net7.0` · `net8.0` · `net9.0` · `net10.0`

## Getting Started

### 1. Configure the Client

```csharp
using OutWit.Common.NewRelic;
using OutWit.Common.NewRelic.Model;

var options = new NewRelicClientOptions
{
    ApiKey    = "NRAK-...",       // User API key (not license key)
    AccountId = 1234567
};

var httpClient = new NewRelicHttpClient(options);
var provider   = new NewRelicProvider(httpClient);
```

`NewRelicClientOptions` also exposes:

| Property | Default | Description |
|---|---|---|
| `Endpoint` | `https://api.newrelic.com/graphql` | NerdGraph endpoint (override for EU: `https://api.eu.newrelic.com/graphql`) |
| `DefaultPageSize` | `100` | Page size when the query does not specify one |
| `MaxPageSize` | `1000` | Upper clamp for any requested page size |

For unit testing you can inject your own `HttpClient`:

```csharp
var httpClient = new NewRelicHttpClient(new HttpClient(mockHandler), options);
```

### 2. Query Logs

#### By absolute time range

```csharp
var page = await provider.GetLogsAsync(
    from:     DateTime.UtcNow.AddHours(-1),
    to:       DateTime.UtcNow,
    filters:  new[] { NewRelicLogFilters.LevelAtLeast(NewRelicLogSeverity.Warning) },
    pageSize: 50
);

foreach (var entry in page.Items)
    Console.WriteLine($"[{entry.Level}] {entry.Timestamp:HH:mm:ss} {entry.Message}");

if (page.HasMore)
{
    // Fetch next page
    var next = await provider.GetLogsAsync(
        from: DateTime.UtcNow.AddHours(-1),
        to:   DateTime.UtcNow,
        filters: new[] { NewRelicLogFilters.LevelAtLeast(NewRelicLogSeverity.Warning) },
        pageSize: 50,
        offset: page.Offset + page.PageSize
    );
}
```

#### By lookback window (relative time)

```csharp
var page = await provider.GetRecentLogsAsync(
    lookback: TimeSpan.FromMinutes(15),
    filters:  new[] { NewRelicLogFilters.ServiceEquals("my-api") }
);
```

#### Full-text search

```csharp
var page = await provider.SearchAsync(
    text:     "NullReferenceException",
    lookback: TimeSpan.FromHours(1),
    extraFilters: new[] { NewRelicLogFilters.EnvironmentEquals("Production") }
);
```

#### Low-level: custom `NewRelicLogQuery`

```csharp
var query = new NewRelicLogQuery
{
    From           = DateTime.UtcNow.AddHours(-2),
    To             = DateTime.UtcNow,
    FullTextSearch = "timeout",
    Filters        = new[]
    {
        NewRelicLogFilters.ServiceIn("api-gateway", "auth-service"),
        NewRelicLogFilters.LevelAtLeast(NewRelicLogSeverity.Error)
    },
    PageSize  = 200,
    Offset    = 0,
    SortOrder = NewRelicLogSortOrder.Descending
};

var page = await provider.QueryAsync(query);
```

### 3. Build Filters

#### Low-level factory methods on `NewRelicLogFilter`

```csharp
NewRelicLogFilter.Eq("level", "Error")             // level = 'Error'
NewRelicLogFilter.NotEq("level", "Debug")           // level != 'Debug'
NewRelicLogFilter.Contains("message", "timeout")    // message LIKE '%timeout%'
NewRelicLogFilter.NotContains("message", "health")  // message NOT LIKE '%health%'
NewRelicLogFilter.In("level", "Error", "Critical")  // level IN ('Error', 'Critical')
NewRelicLogFilter.GreaterThan("timestamp", "...")    // timestamp > ...
NewRelicLogFilter.LessOrEqual("duration", "5000")   // duration <= 5000
```

#### Strongly-typed helpers via `NewRelicLogFilters`

The `NewRelicLogFilters` static class eliminates magic strings:

```csharp
// Severity
NewRelicLogFilters.LevelEquals(NewRelicLogSeverity.Error)
NewRelicLogFilters.LevelIn(NewRelicLogSeverity.Error, NewRelicLogSeverity.Critical)
NewRelicLogFilters.LevelAtLeast(NewRelicLogSeverity.Warning)   // Warning + Error + Critical + Fatal

// Message
NewRelicLogFilters.MessageContains("timeout")
NewRelicLogFilters.MessageNotContains("healthcheck")

// Service / Environment / Context
NewRelicLogFilters.ServiceEquals("my-api")
NewRelicLogFilters.ServiceIn("api-gateway", "auth-service")
NewRelicLogFilters.EnvironmentEquals("Production")
NewRelicLogFilters.SourceContextEquals("MyApp.Services.OrderService")
NewRelicLogFilters.SourceContextIn("MyApp.Services.OrderService", "MyApp.Services.PaymentService")

// Distributed tracing
NewRelicLogFilters.TraceIdEquals("abc123")
NewRelicLogFilters.SpanIdEquals("span-456")
```

### 4. Log Attributes

`NewRelicLogAttribute` defines the well-known log attributes with their common New Relic variations:

| Attribute | Primary name | Recognized variations |
|---|---|---|
| `Timestamp` | `timestamp` | — |
| `Level` | `level` | `log.level` |
| `Message` | `message` | — |
| `Host` | `hostname` | `host`, `host.name` |
| `ServiceName` | `service.name` | `serviceName` |
| `SourceContext` | `Message.Properties.SourceContext` | `SourceContext`, `logger`, `logger.name` |
| `Environment` | `environment` | `env` |
| `Exception` | `exception` | — |
| `TraceId` | `trace.id` | `traceId` |
| `SpanId` | `span.id` | `spanId` |

Each attribute supports `Is()`, `StartsWith()`, and `EndsWith()` matching (case-insensitive) against both the primary name and all variations.

### 5. Severity Levels

`NewRelicLogSeverity` maps standard log levels with a numeric ordering:

| Level | Numeric | 
|---|---|
| `Trace` | 0 |
| `Debug` | 1 |
| `Information` | 2 |
| `Warning` | 3 |
| `Error` | 4 |
| `Critical` | 5 |
| `Fatal` | 6 |

```csharp
// Get all levels >= Warning
var severe = NewRelicLogSeverity.LevelAtLeast(NewRelicLogSeverity.Warning);
// Returns: [Warning, Error, Critical, Fatal]
```

### 6. Distinct Attribute Values

Useful for populating filter dropdowns in a UI:

```csharp
var services = await provider.GetDistinctValuesAsync(
    from:      DateTime.UtcNow.AddDays(-7),
    to:        DateTime.UtcNow,
    attribute: NewRelicLogAttribute.ServiceName,
    limit:     500
);
// Returns: ["api-gateway", "auth-service", "worker", ...]
```

### 7. Find Offset by Timestamp

Navigate to a specific point in the log stream ("scroll to timestamp"):

```csharp
var query = new NewRelicLogQuery
{
    From      = DateTime.UtcNow.AddHours(-6),
    To        = DateTime.UtcNow,
    SortOrder = NewRelicLogSortOrder.Descending
};

long offset = await provider.FindOffsetAsync(query, targetTimestamp);
query.Offset = (int)offset;
var page = await provider.QueryAsync(query);
```

### 8. Log Statistics

Get severity distribution and averages for a time period:

```csharp
var stats = await provider.GetStatisticsAsync(
    from:    DateTime.UtcNow.AddDays(-1),
    to:      DateTime.UtcNow,
    filters: new[] { NewRelicLogFilters.ServiceEquals("my-api") }
);

Console.WriteLine($"Total:    {stats.TotalCount}");
Console.WriteLine($"Errors:   {stats.ErrorCount} ({stats.ErrorRate:F1}%)");
Console.WriteLine($"Warnings: {stats.WarningCount} ({stats.WarningRate:F1}%)");
Console.WriteLine($"Avg/day:  {stats.AverageLogsPerDay:F0}");
```

`NewRelicLogStatistics` provides these computed properties:

| Property | Description |
|---|---|
| `ErrorRate` | Percentage of Error/Critical/Fatal logs |
| `WarningRate` | Percentage of Warning logs |
| `InfoRate` | Percentage of Information logs |
| `DebugRate` | Percentage of Debug/Trace logs |
| `DurationDays` | Length of the period in days |
| `AverageLogsPerDay` | Total / days |
| `AverageErrorsPerDay` | Errors / days |
| `AverageWarningsPerDay` | Warnings / days |

### 9. Data Consumption (Billing)

Monitor actual data ingestion from New Relic's `NrConsumption` billing event:

```csharp
var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

var consumption = await provider.GetDataConsumptionAsync(
    from: monthStart,
    to:   DateTime.UtcNow
);

Console.WriteLine($"Total ingested: {consumption.TotalGigabytes:F2} GB");
Console.WriteLine($"Daily average:  {consumption.DailyAverageGigabytes:F2} GB/day");
Console.WriteLine($"Projected EOM:  {consumption.ProjectedEndOfMonthGigabytes:F2} GB");
Console.WriteLine($"Free tier used: {consumption.FreeTierUsagePercent:F1}%");
Console.WriteLine($"Free tier left: {consumption.FreeTierRemainingGB:F2} GB");

if (consumption.WillExceedFreeTier)
    Console.WriteLine($"? Projected overage: {consumption.ProjectedOverageGB:F2} GB");

// Breakdown by product line
Console.WriteLine($"  Logs:    {consumption.LogsGigabytes:F2} GB");
Console.WriteLine($"  Metrics: {consumption.MetricsGigabytes:F2} GB");
Console.WriteLine($"  Traces:  {consumption.TracesGigabytes:F2} GB");
Console.WriteLine($"  Events:  {consumption.EventsGigabytes:F2} GB");
```

## Result Models

### `NewRelicLogPage`

| Property | Type | Description |
|---|---|---|
| `Items` | `NewRelicLogEntry[]` | Log entries on the current page |
| `Offset` | `int` | Zero-based offset of this page |
| `PageSize` | `int` | Requested page size |
| `HasMore` | `bool` | `true` when `Items.Length == PageSize` (next page likely exists) |

### `NewRelicLogEntry`

| Property | Type | Description |
|---|---|---|
| `Timestamp` | `DateTime` | Log timestamp (UTC) |
| `Level` | `NewRelicLogSeverity?` | Parsed severity level |
| `Message` | `string?` | Log message text |
| `Exception` | `string?` | Exception details (message + stack trace) |
| `SourceContext` | `string?` | Logger / source context name |
| `ServiceName` | `string?` | Logical service name (`service.name`) |
| `Host` | `string?` | Host / machine / container name |
| `Environment` | `string?` | Environment name (dev/staging/prod) |
| `TraceId` | `string?` | Distributed trace identifier |
| `SpanId` | `string?` | Span identifier within a trace |

## Architecture

```
INewRelicProvider (interface)
  ??? NewRelicProvider (orchestration)
        ??? NewRelicHttpClient : RestClientBase (HTTP + GraphQL)
              ??? NrqlRequest : IRequestPost (NRQL ? GraphQL body)

NewRelicLogQuery ??? NrqlQueryBuilder.BuildNrql() ??? NRQL string
NerdGraph JSON   ??? NrqlResponseParser            ??? Typed models
```

## Dependencies

| Package | Purpose |
|---|---|
| `OutWit.Common.Rest` | Base HTTP client (`RestClientBase`) |
| `OutWit.Common.MemoryPack` | Binary serialization attributes for models |

## License

Licensed under the Apache License, Version 2.0. See `LICENSE`.

## Attribution (optional)

If you use OutWit.Common.NewRelic in a product, a mention is appreciated (but not required), for example:
"Powered by OutWit.Common.NewRelic (https://ratner.io/)".

## Trademark / Project name

"OutWit" and the OutWit logo are used to identify the official project by Dmitry Ratner.

You may:
- refer to the project name in a factual way (e.g., "built with OutWit.Common.NewRelic");
- use the name to indicate compatibility (e.g., "OutWit.Common.NewRelic-compatible").

You may not:
- use "OutWit.Common.NewRelic" as the name of a fork or a derived product in a way that implies it is the official project;
- use the OutWit.Common.NewRelic logo to promote forks or derived products without permission.
