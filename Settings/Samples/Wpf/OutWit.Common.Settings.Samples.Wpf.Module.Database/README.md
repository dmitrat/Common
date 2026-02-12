# OutWit.Common.Settings.Samples.Wpf.Module.Database

Database-backed settings module demonstrating SQLite storage.

## What This Module Demonstrates

### ??? Database Storage Provider

- Transactional storage with ACID guarantees
- SQLite via Entity Framework Core
- Queryable settings (for advanced scenarios)
- Default values seeding

### Settings Defined

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `CacheSize` | Integer | 1024 | Cache size in MB |
| `LogLevel` | Enum | Info | Log verbosity (Debug/Info/Warning/Error) |
| `DebugMode` | Boolean | false | Enable debug mode |
| `AccentColor` | ColorRgb | (0,120,215) | UI accent color |

## Storage Files

```
settings.witdb           (defaults database)
{UserProfile}/           (user overrides)
    ??? settings.witdb
```

## Database Seeding

Unlike JSON/CSV which use embedded resources, database defaults must be seeded programmatically:

```csharp
public static class DatabaseSeeder
{
    public static void EnsureDefaults(string dbPath)
    {
        if (File.Exists(dbPath))
            return;

        using var provider = new DatabaseSettingsProvider(
            dbPath, SettingsScope.Default);
        
        provider.Upsert("AdvancedSettings", "CacheSize", 
            new SettingsRecord("CacheSize", "Integer", null, "Cache Size", "1024"));
        
        provider.Upsert("AdvancedSettings", "AccentColor",
            new SettingsRecord("AccentColor", "ColorRgb", null, "Accent Color", "0;120;215"));
    }
}
```

## Usage

```csharp
// Seed defaults first
DatabaseSeeder.EnsureDefaults(SettingsPathResolver.GetDefaultsPath(".db"));

var module = new AdvancedModule();
module.Initialize();

// Create typed container bound to the manager
var settings = new AdvancedSettings(module.Manager);
Console.WriteLine(settings.LogLevel);     // LogLevel.Info
Console.WriteLine(settings.AccentColor);  // ColorRgb(0, 120, 215)

// Modify via typed container (intercepted by AOP)
settings.LogLevel = LogLevel.Debug;
settings.AccentColor = new ColorRgb(255, 0, 0);
module.Manager.Save();
```

## Module Structure

```csharp
public sealed class AdvancedModule : IAdvancedModule
{
    public void Initialize()
    {
        // Seed defaults if needed
        DatabaseSeeder.EnsureDefaults(
            SettingsPathResolver.GetDefaultsPath(".db"));

        Manager = new SettingsBuilder()
            .AddCustomSerializers()
            .UseDatabase()  // Database storage provider
            .RegisterContainer<AdvancedSettings>()
            .Build();

        Manager.Merge();
        Manager.Load();
    }

    public ISettingsManager Manager { get; private set; }
}

// Create a typed container anywhere:
var settings = new AdvancedSettings(module.Manager);
settings.LogLevel = LogLevel.Debug;  // AOP intercepts and writes to manager
```

## When to Use Database

- Settings require transactional integrity
- Large number of settings (thousands+)
- Need to query settings programmatically
- Concurrent access from multiple processes
