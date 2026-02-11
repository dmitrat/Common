# OutWit.Common.Settings.Csv

## Overview

OutWit.Common.Settings.Csv is a CSV file storage provider for the OutWit.Common.Settings framework. It enables storing and loading application settings in tabular CSV format, ideal for scenarios where settings need to be easily editable in spreadsheet applications or processed by external tools.

## Features

### 1. Simple Integration
Use the `UseCsv()` extension method to configure CSV storage with conventional defaults:

```csharp
var manager = new SettingsBuilder()
    .UseCsv()  // Uses {AppContext.BaseDirectory}/Resources/settings.csv
    .RegisterContainer<NetworkSettings>()
    .Build();

manager.Merge();
manager.Load();
```

### 2. Custom File Paths
Specify custom paths for defaults and scope-specific files:

```csharp
var manager = new SettingsBuilder()
    .UseCsv("path/to/defaults.csv")
    .Build();
```

Or add individual CSV files for specific scopes:
```csharp
builder.UseCsvFile("config/global-settings.csv", SettingsScope.Global);
builder.UseCsvFile("config/user-settings.csv", SettingsScope.User);
```

### 3. CSV File Format
Settings are stored in a simple tabular format with the following columns:

```csv
Group,Key,Value,ValueKind,Tag,Hidden
NetworkSettings,ApiEndpoint,http://localhost:5000,ServiceUrl,,False
NetworkSettings,ProxyUrl,,Url,,False
NetworkSettings,ConnectionTimeout,00:00:30,TimeSpan,,False
NetworkSettings,MaxRetries,3,Integer,,False
```

**Columns:**
- `Group` - Settings group name
- `Key` - Setting key/property name
- `Value` - Serialized value
- `ValueKind` - Type identifier for deserialization
- `Tag` - Optional metadata (e.g., enum type for Enum values)
- `Hidden` - Whether to hide from UI

### 4. Group Metadata
Group metadata can be stored in a separate file (`settings.groups.csv`):

```csv
Group,DisplayName,Priority
NetworkSettings,Network,0
Security,Security Settings,5
```

### 5. Spreadsheet-Friendly
CSV format allows easy editing in Excel, Google Sheets, or any text editor.

## Installation

Install the package via NuGet:
```bash
Install-Package OutWit.Common.Settings.Csv
```

**Dependencies:**
- `OutWit.Common.Settings`
- `CsvHelper`

## License

Licensed under the Apache License, Version 2.0. See `LICENSE`.

## Attribution (optional)

If you use OutWit.Common.Settings.Csv in a product, a mention is appreciated (but not required), for example:
"Powered by OutWit.Common.Settings.Csv (https://ratner.io/)".

## Trademark / Project name

"OutWit" and the OutWit logo are used to identify the official project by Dmitry Ratner.

You may:
- refer to the project name in a factual way (e.g., "built with OutWit.Common.Settings.Csv");
- use the name to indicate compatibility (e.g., "OutWit.Common.Settings.Csv-compatible").

You may not:
- use "OutWit.Common.Settings.Csv" as the name of a fork or a derived product in a way that implies it is the official project;
- use the OutWit.Common.Settings.Csv logo to promote forks or derived products without permission.
