# OutWit.Common.Configuration

## Overview

OutWit.Common.Configuration provides a fluent API for building and binding `Microsoft.Extensions.Configuration` settings in .NET applications. It simplifies loading JSON configuration files with environment-specific overrides and binding entire configuration trees to strongly-typed settings classes via a single call.

Built on top of `Microsoft.Extensions.Configuration`, the library adds two things the standard API does not provide out of the box:

1. **Assembly-relative file resolution** — configuration files are resolved relative to the assembly directory, not `Environment.CurrentDirectory`. This is especially useful for plugin architectures and test runners.
2. **`[ConfigSection]` attribute** — allows mapping individual properties of a settings class to arbitrary configuration section names, so the POCO shape does not have to mirror the JSON structure.

#### Install

```ps1
Install-Package OutWit.Common.Configuration
```

or

```bash
dotnet add package OutWit.Common.Configuration
```

## Target Frameworks

| TFM | Microsoft.Extensions.Configuration |
|---|---|
| `net10.0`, `netstandard2.0` | 10.0.x |
| `net9.0`, `net8.0` | 9.0.x |
| `net7.0`, `net6.0` | 8.0.x |

## Features

### 1. Fluent Configuration Builder

Build an `IConfiguration` instance from JSON files with optional environment-specific overrides using a fluent API:

```csharp
using OutWit.Common.Configuration;

// Minimal — loads appsettings.json from the assembly directory
var config = ConfigurationUtils
    .For(Assembly.GetExecutingAssembly())
    .Build();
```

The full builder chain:

```csharp
var config = ConfigurationUtils
    .For(Assembly.GetExecutingAssembly())   // base path = assembly directory
    .WithFileName("mysettings")             // default is "appsettings"
    .WithEnvironment(ConfigurationEnvironment.Development)
    .Build();
```

This resolves two files from the directory where the calling assembly DLL is located:

1. `mysettings.json` (optional, `reloadOnChange: true`)
2. `mysettings.Development.json` (optional, `reloadOnChange: true`)

Values from the environment file override the base file, following the standard `Microsoft.Extensions.Configuration` layering convention.

The environment can also be specified as a plain string:

```csharp
var config = ConfigurationUtils
    .For(Assembly.GetExecutingAssembly())
    .WithEnvironment("Staging")
    .Build();
```

### 2. Attribute-Based Section Binding

The `BindSettings<T>()` extension method creates a new `T` and populates each public read-write property from the corresponding `IConfigurationSection`. By default, the section name equals the property name. Use the `[ConfigSection]` attribute to override it:

```csharp
public class AppSettings
{
    // Binds from section "AppName" (matches property name)
    public string AppName { get; set; }

    // Binds from section "ConnectionStrings" instead of "Database"
    [ConfigSection("ConnectionStrings")]
    public ConnectionDetails Database { get; set; }

    // Binds from section "Logging:LogLevel" (nested path)
    [ConfigSection("Logging:LogLevel")]
    public string LogLevel { get; set; }
}

public class ConnectionDetails
{
    public string DefaultConnection { get; set; }
}
```

Usage:

```csharp
var settings = config.BindSettings<AppSettings>();

Console.WriteLine(settings.AppName);                    // from "AppName" section
Console.WriteLine(settings.Database.DefaultConnection); // from "ConnectionStrings" section
Console.WriteLine(settings.LogLevel);                   // from "Logging:LogLevel" section
```

**How it works under the hood:**

1. Creates a new instance of `T` via the parameterless constructor.
2. Enumerates all public instance properties with both a getter and a setter.
3. For each property, reads the `[ConfigSection("...")]` attribute; falls back to the property name.
4. Calls `IConfiguration.GetSection(name).Get(propertyType)` to bind the section.
5. Properties whose sections do not exist in the configuration are left at their default values.

### 3. Supported Environments

The `ConfigurationEnvironment` enum provides the standard set of well-known environment names:

| Value | Typical usage |
|---|---|
| `Development` | Local dev machines, hot reload, verbose logging |
| `Production` | Live deployments, minimal logging |
| `Test` | Automated test runners, in-memory or mock services |

These can be passed directly to `WithEnvironment()`:

```csharp
.WithEnvironment(ConfigurationEnvironment.Production)
```

## API Reference

| Class / Method | Description |
|---|---|
| `ConfigurationUtils.For(Assembly)` | Creates a `ConfigurationInfo` builder anchored to the assembly's directory |
| `.WithFileName(string)` | Sets the base config file name (default: `"appsettings"`) |
| `.WithEnvironment(string)` | Sets the environment name for overlay files |
| `.WithEnvironment(ConfigurationEnvironment)` | Same, using the enum |
| `.Build()` | Builds and returns `IConfiguration` |
| `IConfiguration.BindSettings<T>()` | Binds configuration sections to a new `T` instance using reflection and `[ConfigSection]` |
| `ConfigSectionAttribute` | Attribute that overrides the configuration section name for a property |

## License

Licensed under the Apache License, Version 2.0. See `LICENSE`.

## Attribution (optional)

If you use OutWit.Common.Configuration in a product, a mention is appreciated (but not required), for example:
"Powered by OutWit.Common.Configuration (https://ratner.io/)".

## Trademark / Project name

"OutWit" and the OutWit logo are used to identify the official project by Dmitry Ratner.

You may:
- refer to the project name in a factual way (e.g., "built with OutWit.Common.Configuration");
- use the name to indicate compatibility (e.g., "OutWit.Common.Configuration-compatible").

You may not:
- use "OutWit.Common.Configuration" as the name of a fork or a derived product in a way that implies it is the official project;
- use the OutWit.Common.Configuration logo to promote forks or derived products without permission.
