# OutWit.Common.Settings.Samples.Serializers

Custom type serializers for the OutWit.Common.Settings library.

## Overview

This project demonstrates how to extend the settings library with custom types that require special serialization and UI handling.

## Custom Types

### BoundedInt

An integer value constrained to a specific range, ideal for slider controls.

```csharp
[MemoryPackable]
public partial class BoundedInt
{
    public int Value { get; set; }
    public int Min { get; set; }
    public int Max { get; set; }
}
```

**Use case:** Volume levels (0-100), quality settings, any numeric value with bounds.

**UI:** Rendered as a slider with min/max constraints in both WPF and Blazor samples.

### ColorRgb

An RGB color represented as three byte components.

```csharp
[MemoryPackable]
public partial class ColorRgb
{
    public byte R { get; set; }
    public byte G { get; set; }
    public byte B { get; set; }
}
```

**Use case:** Theme colors, accent colors, any color configuration.

**UI:** Three sliders (R/G/B) with live color preview.

## Creating a Custom Serializer

### Step 1: Define the Serializer

```csharp
public sealed class SettingsSerializerBoundedInt : SettingsSerializerBase<BoundedInt>
{
    public override string ValueKind => "BoundedInt";

    public override BoundedInt Parse(string value, string tag)
    {
        var parts = value.Split(';');
        return new BoundedInt
        {
            Value = int.Parse(parts[0]),
            Min = int.Parse(parts[1]),
            Max = int.Parse(parts[2])
        };
    }

    public override string Format(BoundedInt value)
    {
        return $"{value.Value};{value.Min};{value.Max}";
    }

    public override bool AreEqual(BoundedInt a, BoundedInt b)
    {
        return a.Value == b.Value && a.Min == b.Min && a.Max == b.Max;
    }
}
```

### Step 2: Register the Serializer

```csharp
public static class SettingsBuilderCustomExtensions
{
    public static SettingsBuilder AddCustomSerializers(this SettingsBuilder builder)
    {
        return builder
            .RegisterSerializer<BoundedInt>(new SettingsSerializerBoundedInt())
            .RegisterSerializer<ColorRgb>(new SettingsSerializerColorRgb());
    }
}
```

### Step 3: Use in Settings Builder

```csharp
var manager = new SettingsBuilder()
    .AddCustomSerializers()  // Register custom types
    .UseJson()
    .RegisterContainer<MySettings>()
    .Build();
```

## MemoryPack Integration

For network serialization (e.g., WitRPC), register MemoryPack formatters:

```csharp
SettingsBuilder.RegisterMemoryPack(b => b.AddCustomSerializers());
```

This ensures custom types can be transmitted over WebSocket connections.

## Files

| File | Description |
|------|-------------|
| `Types/BoundedInt.cs` | Bounded integer type definition |
| `Types/ColorRgb.cs` | RGB color type definition |
| `Serialization/SettingsSerializerBoundedInt.cs` | BoundedInt serializer |
| `Serialization/SettingsSerializerColorRgb.cs` | ColorRgb serializer |
| `SettingsBuilderCustomExtensions.cs` | Extension methods for registration |
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
