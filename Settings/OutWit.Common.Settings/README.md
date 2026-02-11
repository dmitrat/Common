# OutWit.Common.Settings

## Overview

OutWit.Common.Settings is a powerful, type-safe settings management library for .NET applications. It provides a declarative API with AOP-powered property interception, multi-scope storage (Default, User, Global), and seamless integration with various storage backends through a pluggable provider architecture.

## Features

### 1. Declarative Settings with AOP
Define settings as simple properties in a container class. The `[Setting]` attribute automatically intercepts getters and setters to read/write values from the settings manager.

#### Example
```csharp
public class ApplicationSettings : SettingsContainer
{
    public ApplicationSettings(ISettingsManager manager) : base(manager) { }

    [Setting("AppSettings")]
    public virtual string Language { get; set; } = null!;

    [Setting("AppSettings")]
    public virtual bool AutoSave { get; set; }

    [Setting("AppSettings")]
    public virtual int AutoSaveInterval { get; set; }
}
```

### 2. Multi-Scope Storage
Settings support three scopes with automatic value resolution:
- **Default** - Read-only defaults shipped with the application
- **User** - Per-user overrides (stored in user profile)
- **Global** - Machine-wide overrides (shared across users)

Values are resolved in order: User, Global, Default.

### 3. Fluent Builder API
Configure the settings manager with a clean, chainable API:

```csharp
var manager = new SettingsBuilder()
    .UseJson()                                // Use JSON storage format
    .RegisterContainer<ApplicationSettings>() // Register typed container
    .WithLogger(logger)                       // Optional logging
    .Build();

manager.Load();
```

### 4. Automatic Merge
The `Merge()` method synchronizes user storage with defaults - adding new keys, removing obsolete ones, and preserving existing user values:

```csharp
manager.Merge();  // Sync schema with defaults
manager.Load();   // Load values
manager.Save();   // Persist user changes
```

### 5. Built-in Serializers
Out-of-the-box support for common types:
- Primitives: `String`, `Integer`, `Double`, `Decimal`, `Long`, `Boolean`
- Collections: `StringList`, `IntegerList`, `DoubleList`, `EnumList`
- Special types: `DateTime`, `TimeSpan`, `Guid`, `Enum`, `Password`, `Url`, `ServiceUrl`, `Path`, `Folder`, `Language`

### 6. Custom Serializers
Extend the system with custom value types:

```csharp
public class ColorRgbSerializer : SettingsSerializerBase<ColorRgb>
{
    public override string ValueKind => "ColorRgb";

    protected override string Serialize(ColorRgb value)
        => $"{value.R},{value.G},{value.B}";

    protected override ColorRgb Deserialize(string value, string? tag)
    {
        var parts = value.Split(',');
        return new ColorRgb(
            byte.Parse(parts[0]),
            byte.Parse(parts[1]),
            byte.Parse(parts[2]));
    }
}

// Register during build
var manager = new SettingsBuilder()
    .AddSerializer(new ColorRgbSerializer())
    .UseJson()
    .Build();
```

### 7. Group Metadata
Organize settings into logical groups with display names and priorities:

```json
{
  "__groups__": {
    "AppSettings": {
      "displayName": "Application",
      "priority": 0
    },
    "Advanced": {
      "displayName": "Advanced Settings",
      "priority": 10
    }
  }
}
```

Or configure programmatically:
```csharp
builder.ConfigureGroup("AppSettings", priority: 0, displayName: "Application");
```

### 8. MemoryPack Serialization
Built-in support for MemoryPack binary serialization, enabling efficient settings transfer over network (e.g., WitRPC):

```csharp
// Client-side registration (no manager needed)
SettingsBuilder.RegisterMemoryPack(b => b.AddSerializer(new ColorRgbSerializer()));
```

## Installation

Install the package via NuGet:
```bash
Install-Package OutWit.Common.Settings
```

For specific storage formats, install the corresponding provider package:
- `OutWit.Common.Settings.Json` - JSON file storage
- `OutWit.Common.Settings.Csv` - CSV file storage
- `OutWit.Common.Settings.Database` - Entity Framework Core database storage

## License

Licensed under the Apache License, Version 2.0. See `LICENSE`.

## Attribution (optional)

If you use OutWit.Common.Settings in a product, a mention is appreciated (but not required), for example:
"Powered by OutWit.Common.Settings (https://ratner.io/)".

## Trademark / Project name

"OutWit" and the OutWit logo are used to identify the official project by Dmitry Ratner.

You may:
- refer to the project name in a factual way (e.g., "built with OutWit.Common.Settings");
- use the name to indicate compatibility (e.g., "OutWit.Common.Settings-compatible").

You may not:
- use "OutWit.Common.Settings" as the name of a fork or a derived product in a way that implies it is the official project;
- use the OutWit.Common.Settings logo to promote forks or derived products without permission.
