# OutWit.Common.Settings.Json

## Overview

OutWit.Common.Settings.Json is a JSON file storage provider for the OutWit.Common.Settings framework. It enables storing and loading application settings in human-readable JSON format with support for group metadata, multi-scope storage, and automatic schema merging.

## Features

### 1. Simple Integration
Use the `UseJson()` extension method to configure JSON storage with conventional defaults:

```csharp
var manager = new SettingsBuilder()
    .UseJson()  // Uses {AppContext.BaseDirectory}/Resources/settings.json
    .RegisterContainer<ApplicationSettings>()
    .Build();

manager.Merge();
manager.Load();
```

### 2. Custom File Paths
Specify custom paths for defaults and scope-specific files:

```csharp
var manager = new SettingsBuilder()
    .UseJson("path/to/defaults.json")
    .Build();
```

Or add individual JSON files for specific scopes:
```csharp
builder.UseJsonFile("config/global-settings.json", SettingsScope.Global);
builder.UseJsonFile("config/user-settings.json", SettingsScope.User);
```

### 3. JSON File Format
Settings are stored as arrays keyed by group name with optional group metadata:

```json
{
  "__groups__": {
    "AppSettings": {
      "displayName": "Application",
      "priority": 0
    },
    "Notifications": {
      "displayName": "Notifications",
      "priority": 5
    }
  },
  "AppSettings": [
    {
      "key": "Theme",
      "value": "Light",
      "valueKind": "Enum",
      "tag": "MyApp.AppTheme, MyApp"
    },
    {
      "key": "Language",
      "value": "en-US",
      "valueKind": "Language"
    },
    {
      "key": "AutoSave",
      "value": "True",
      "valueKind": "Boolean"
    }
  ]
}
```

### 4. Multi-Scope Support
Automatically creates separate JSON files for User and Global scopes based on assembly path conventions:
- Default: `Resources/settings.json` (read-only)
- User: `{UserProfile}/.config/{AppName}/settings.json`
- Global: `{CommonAppData}/{AppName}/settings.json`

### 5. Readable and Editable
JSON format allows easy manual editing and version control of configuration files.

## Installation

Install the package via NuGet:
```bash
Install-Package OutWit.Common.Settings.Json
```

**Dependencies:**
- `OutWit.Common.Settings`

## License

Licensed under the Apache License, Version 2.0. See `LICENSE`.

## Attribution (optional)

If you use OutWit.Common.Settings.Json in a product, a mention is appreciated (but not required), for example:
"Powered by OutWit.Common.Settings.Json (https://ratner.io/)".

## Trademark / Project name

"OutWit" and the OutWit logo are used to identify the official project by Dmitry Ratner.

You may:
- refer to the project name in a factual way (e.g., "built with OutWit.Common.Settings.Json");
- use the name to indicate compatibility (e.g., "OutWit.Common.Settings.Json-compatible").

You may not:
- use "OutWit.Common.Settings.Json" as the name of a fork or a derived product in a way that implies it is the official project;
- use the OutWit.Common.Settings.Json logo to promote forks or derived products without permission.
