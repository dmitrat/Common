# OutWit.Common.Blazor.Logging

Blazor WebAssembly log-viewer component library built on **MudBlazor**.  
Provides a complete set of UI components for querying, filtering, and displaying logs from **New Relic** via the companion `OutWit.Common.NewRelic` provider.

| Target Framework |
|:----------------:|
| `net10.0`        |

## Features

| Area | Description |
|------|-------------|
| **Toolbar** | Date/time pickers, severity toggles (Error, Warning, Info, Debug), source multi-select, page-size selector, and pagination controls. |
| **Filter Tree** | Hierarchical filter tree with add / edit / duplicate / delete / disable operations. Each tree node applies a full-text or exclusion filter to the query chain. |
| **Log Table** | Virtualised MudBlazor table with colour-coded severity rows, column layout (Timestamp, Level, Source, Message), row selection, and scroll-to helpers. |
| **Detail Panel** | Displays full message with highlighted search terms, exception block, and context properties (Host, Env, TraceId, SpanId). Includes copy-to-clipboard. |
| **Statistics Dialog** | Shows log counts by severity, daily averages, error/warning rates, and New Relic data-consumption / free-tier status with progress bars. |
| **Multi-Colour Highlighting** | Each level in the filter chain gets a distinct highlight colour (up to 10 levels). Highlighting is cached and only rebuilt when the filter chain changes. |

## Architecture

```
???????????????????????????????????????????????????????
?  Views (.razor)                                     ?
?  LogsToolbar · LogsTable · LogsFilterTree           ?
?  LogsDetails · DialogEditLogFilter                  ?
?  DialogLogsStatistics                               ?
???????????????????????????????????????????????????????
?  ViewModels (inherit ViewModelBase)                 ?
?  LogsToolbarViewModel · LogsTableViewModel          ?
?  LogsFilterTreeViewModel · LogsDetailsViewModel     ?
?  DialogEditLogFilterViewModel                       ?
?  DialogLogsStatisticsViewModel                      ?
???????????????????????????????????????????????????????
?  Model           ?  Highlight       ?  Utils        ?
?  LogConditions   ?  HighlighterLog  ?  TimeConvert  ?
?  LogFilterNode   ?  HighlightLevel  ?               ?
?  LogFilterEdit   ?                  ?               ?
?  Result          ?                  ?               ?
???????????????????????????????????????????????????????
?  OutWit.Common.NewRelic  (data layer)               ?
?  OutWit.Common.MVVM.Blazor (MVVM infrastructure)    ?
???????????????????????????????????????????????????????
```

## Installation

```bash
dotnet add package OutWit.Common.Blazor.Logging
```

## Quick Start

### 1. Register Dependencies

The library expects `IDialogService`, `ISnackbar`, and `IJSRuntime` to be available (standard MudBlazor services).

### 2. Add the Components

```razor
<LogsToolbar Conditions="@_conditions"
             ConditionsChanged="OnConditionsChanged"
             CurrentPageHasMore="@_hasMore"
             IsLoading="@_loading"
             StatisticsRequested="OnShowStatistics" />

<LogsTable Entries="@_entries"
           SelectedEntry="@_selected"
           SelectedEntryChanged="OnEntrySelected"
           HighlightFunc="@_highlighter.Highlight"
           Busy="@_loading" />

<LogsFilterTree @bind-SelectedFilter="_selectedFilter"
                FiltersChanged="OnFiltersChanged"
                Busy="@_loading" />

<LogsDetails Entry="@_selected"
             HighlightFunc="@(text => _highlighter.Highlight(text, _selectedFilter))" />
```

### 3. Build Queries

```csharp
var query = _conditions.Query(_selectedFilter);
var result = await _newRelicProvider.GetLogs(query);
```

## Key Types

| Type | Description |
|------|-------------|
| `LogConditions` | Encapsulates all query state: date, time range, severity levels, sources, pagination. |
| `LogFilterNode` | Tree node supporting parent/child hierarchy, shallow/deep clone, highlight term extraction. |
| `LogFilterEditResult` | DTO returned by the edit-filter dialog. |
| `HighlighterLog` | Applies multi-colour `<span>` highlighting based on filter chain depth. |
| `HighlightLevel` | Represents a single highlight level with CSS class and compiled regex. |
| `TimeConversionUtils` | Extension methods converting between `TimeOnly` and `TimeSpan` for MudBlazor pickers. |

## CSS

Include `wwwroot/css/m3-logs.css` for toolbar, table, cell, and highlight styles.

```html
<link href="_content/OutWit.Common.Blazor.Logging/css/m3-logs.css" rel="stylesheet" />
```

## Dependencies

| Package | Version |
|---------|---------|
| `Microsoft.AspNetCore.Components.WebAssembly` | 10.0.2 |
| `MudBlazor` | 8.15.0 |
| `MudBlazor.FontIcons.MaterialSymbols` | 1.3.0 |
| `OutWit.Common.MVVM.Blazor` | _(project reference)_ |
| `OutWit.Common.NewRelic` | _(project reference)_ |

## Attribution

OutWit.Common.Blazor.Logging is part of the **OutWit** ecosystem.  
Copyright © 2020–2026 Dmitry Ratner.

## Trademarks

"OutWit" is a trademark of Dmitry Ratner. New Relic is a trademark of New Relic, Inc. MudBlazor is a trademark of its respective owners.

## License

Licensed under [Apache-2.0](LICENSE).
