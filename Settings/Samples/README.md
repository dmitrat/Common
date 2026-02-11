# OutWit.Common.Settings Samples

This folder contains sample applications demonstrating the capabilities of the **OutWit.Common.Settings** library.

## Sample Projects Overview

### ?? Serializers
**Project:** `OutWit.Common.Settings.Samples.Serializers`

Demonstrates how to create custom type serializers for settings values:
- `BoundedInt` - Integer with min/max constraints, displayed as a slider
- `ColorRgb` - RGB color type with dedicated editor

### ??? WPF Application
**Project:** `OutWit.Common.Settings.Samples.Wpf`

A complete WPF desktop application showcasing:
- Settings window with tabbed groups
- Custom editors for different value types (text, numeric, boolean, enum, color, slider)
- Real-time value updates with change detection
- Reset to defaults functionality

### ?? Remote Settings Service
**Projects:** `OutWit.Common.Settings.Samples.Service` + `OutWit.Common.Settings.Samples.Service.UI`

Client-server architecture for remote settings management:
- **Service** - WebSocket server exposing settings via WitRPC
- **Service.UI** - Blazor WebAssembly client with MudBlazor UI

## Storage Providers Demonstrated

| Module | Provider | Format | Use Case |
|--------|----------|--------|----------|
| `Wpf.Module.Json` | JSON | `.json` files | Human-readable, easy to edit |
| `Wpf.Module.Csv` | CSV | `.csv` files | Spreadsheet editing, bulk updates |
| `Wpf.Module.Database` | Database | `.witdb` (SQLite) | Transactional, queryable |
| `Wpf.Module.SharedDatabase` | Shared Database | Single `.witdb` | Multi-user with per-user isolation |

## Running the Samples

### WPF Application
```bash
dotnet run --project Settings/Samples/Wpf/OutWit.Common.Settings.Samples.Wpf
```

### Remote Settings (requires both projects)
```bash
# Terminal 1 - Start the server
dotnet run --project Settings/Samples/Services/OutWit.Common.Settings.Samples.Service

# Terminal 2 - Start the Blazor client
dotnet run --project Settings/Samples/Services/OutWit.Common.Settings.Samples.Service.UI
```

## Key Concepts Demonstrated

1. **AOP-based Property Interception** - Settings properties are automatically intercepted
2. **Multi-scope Storage** - Default, Global, and User scopes with proper merging
3. **Custom Serializers** - Extend the library with your own types
4. **Provider Abstraction** - Same API, different storage backends
5. **Remote Access** - Settings can be exposed over network protocols
