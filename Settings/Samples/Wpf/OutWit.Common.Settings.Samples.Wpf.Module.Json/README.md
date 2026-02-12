# OutWit.Common.Settings.Samples.Wpf.Module.Json

JSON-based settings module demonstrating file storage.

## What This Module Demonstrates

### ?? JSON Storage Provider

- Human-readable settings format
- Easy manual editing and version control
- Standard JSON structure

### Settings Defined

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `Language` | String | "en" | Application language |
| `Theme` | Enum | Light | App theme (Light/Dark/System) |
| `AutoSave` | Boolean | true | Auto-save enabled |
| `AutoSaveInterval` | Integer | 300 | Auto-save interval in seconds |
| `NotificationsEnabled` | Boolean | true | Show notifications |
| `MaxVolume` | BoundedInt | 50 (0-100) | Maximum volume level |

## Storage Files

```
settings.json          (defaults - embedded resource)
{UserProfile}/         (user overrides)
    ??? settings.json
```

### Example JSON

```json
{
  "__groups__": {
    "AppSettings": {
      "displayName": "Application",
      "priority": 0
    }
  },
  "AppSettings": {
    "Language": {
      "kind": "String",
      "value": "en"
    },
    "Theme": {
      "kind": "Enum",
      "tag": "OutWit.Common.Settings.Samples.Wpf.Module.Json.AppTheme",
      "value": "Light"
    },
    "MaxVolume": {
      "kind": "BoundedInt",
      "value": "50;0;100"
    }
  }
}
```

## Usage

```csharp
var module = new ApplicationModule();
module.Initialize();

// Create typed container bound to the manager
var settings = new ApplicationSettings(module.Manager);
Console.WriteLine(settings.Language);   // "en"
Console.WriteLine(settings.Theme);      // AppTheme.Light
Console.WriteLine(settings.AutoSave);   // true

// Modify via typed container (intercepted by AOP)
settings.Theme = AppTheme.Dark;
module.Manager.Save();

// Or access via indexer directly
var value = module.Manager["AppSettings"]["Language"];
```

## Module Structure

```csharp
public sealed class ApplicationModule : IApplicationModule
{
    public void Initialize()
    {
        Manager = new SettingsBuilder()
            .AddCustomSerializers()   // BoundedInt, ColorRgb
            .UseJson()                // JSON storage provider
            .RegisterContainer<ApplicationSettings>()
            .Build();

        Manager.Merge();  // Sync user settings with defaults
        Manager.Load();   // Load values
    }

    public ISettingsManager Manager { get; private set; }
}

// Create a typed container anywhere:
var settings = new ApplicationSettings(module.Manager);
settings.Theme = AppTheme.Dark;  // AOP intercepts and writes to manager
```
