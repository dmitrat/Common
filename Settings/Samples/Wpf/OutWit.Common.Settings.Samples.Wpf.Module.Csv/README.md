# OutWit.Common.Settings.Samples.Wpf.Module.Csv

CSV-based settings module demonstrating tabular storage.

## What This Module Demonstrates

### ?? CSV Storage Provider

- Spreadsheet-compatible format
- Bulk editing with Excel/LibreOffice
- Simple key-value structure per row

### Settings Defined

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `ProxyEnabled` | Boolean | false | Use proxy server |
| `ProxyHost` | String | "" | Proxy server address |
| `ProxyPort` | Integer | 8080 | Proxy server port |
| `ConnectionTimeout` | TimeSpan | 00:00:30 | Connection timeout |
| `MaxRetries` | Integer | 3 | Maximum retry attempts |

## Storage Files

```
Resources/settings.csv  (defaults - embedded resource)
{UserProfile}/          (user overrides)
    ??? settings.csv
```

### Example CSV

```csv
Group,Key,Kind,Tag,Name,Value
NetworkSettings,ProxyEnabled,Boolean,,Proxy Enabled,false
NetworkSettings,ProxyHost,String,,Proxy Host,
NetworkSettings,ProxyPort,Integer,,Proxy Port,8080
NetworkSettings,ConnectionTimeout,TimeSpan,,Connection Timeout,00:00:30
NetworkSettings,MaxRetries,Integer,,Max Retries,3
```

## Usage

```csharp
var module = new NetworkModule();
module.Initialize();

// Create typed container bound to the manager
var settings = new NetworkSettings(module.Manager);
Console.WriteLine(settings.ProxyEnabled);       // false
Console.WriteLine(settings.ConnectionTimeout);  // 00:00:30
Console.WriteLine(settings.MaxRetries);         // 3

// Modify via typed container (intercepted by AOP)
settings.MaxRetries = 5;
module.Manager.Save();
```

## Module Structure

```csharp
public sealed class NetworkModule : INetworkModule
{
    public void Initialize()
    {
        Manager = new SettingsBuilder()
            .UseCsv()  // CSV storage provider
            .RegisterContainer<NetworkSettings>()
            .Build();

        Manager.Merge();
        Manager.Load();
    }

    public ISettingsManager Manager { get; private set; }
}

// Create a typed container anywhere:
var settings = new NetworkSettings(module.Manager);
settings.MaxRetries = 5;  // AOP intercepts and writes to manager
```

## When to Use CSV

- Settings need to be edited in spreadsheet applications
- Bulk import/export with external tools
- Simple flat structure without nesting
- Non-technical users editing settings
