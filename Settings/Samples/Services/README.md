# Settings Samples - Services

Remote settings management via WebSocket using WitRPC.

## Overview

This folder contains a client-server solution for remote settings access:

| Project | Type | Description |
|---------|------|-------------|
| `Service.Contracts` | Class Library | Shared `ISettingsService` interface |
| `Service` | Console App | WebSocket server exposing settings |
| `Service.UI` | Blazor WASM | Web-based settings editor client |

## Architecture

```
???????????????????????      WebSocket       ???????????????????????
?                     ????????????????????????                     ?
?   Service.UI        ?   ws://localhost:    ?   Service           ?
?   (Blazor WASM)     ?       5050/api       ?   (Console)         ?
?                     ?                      ?                     ?
?  ?????????????????  ?                      ?  ?????????????????  ?
?  ? ISettings-    ?  ?    MemoryPack        ?  ? ISettings-    ?  ?
?  ? Service       ????????Binary????????????????? Service       ?  ?
?  ? (proxy)       ?  ?    Serialization     ?  ? (impl)        ?  ?
?  ?????????????????  ?                      ?  ?????????????????  ?
?         ?           ?                      ?         ?           ?
?         ?           ?                      ?         ?           ?
?  MudBlazor UI       ?                      ?  Settings Modules   ?
?                     ?                      ?  ??? JSON           ?
???????????????????????                      ?  ??? CSV            ?
                                             ?  ??? Database       ?
        ?                                    ?  ??? SharedDB       ?
        ?                                    ???????????????????????
        ?
   Service.Contracts
   (shared interface)
```

## Quick Start

### 1. Start the Server

```bash
cd Settings/Samples/Services/OutWit.Common.Settings.Samples.Service
dotnet run
```

Server listens on `ws://localhost:5050/api`.

### 2. Start the Client

```bash
cd Settings/Samples/Services/OutWit.Common.Settings.Samples.Service.UI
dotnet run
```

Open `https://localhost:59065` in your browser.

## Service Contract

```csharp
[WitRpcService]
public interface ISettingsService
{
    // Get all available setting groups
    Task<IReadOnlyList<SettingsGroupInfo>> GetGroupsAsync();
    
    // Get all values in a group
    Task<IReadOnlyList<ISettingsValue>> GetValuesAsync(string group);
    
    // Update a single value
    Task UpdateValueAsync(string group, string key, string value);
    
    // Reset a value to default
    Task ResetValueAsync(string group, string key);
    
    // Reset all values in a group
    Task ResetGroupAsync(string group);
    
    // Persist changes
    Task SaveAsync();
    
    // Reload from storage
    Task ReloadAsync();
}
```

## Key Concepts Demonstrated

1. **WitRPC** - Type-safe RPC over WebSocket
2. **MemoryPack** - High-performance binary serialization
3. **Custom Type Serializers** - BoundedInt, ColorRgb work over the network
4. **Blazor WebAssembly** - .NET in the browser
5. **MudBlazor** - Material Design component library

## Projects

### Service.Contracts

Shared interface definition. Referenced by both server and client.

### Service

Console application that:
- Initializes settings modules (JSON, CSV, Database, SharedDatabase)
- Aggregates all managers into `SettingsServiceImpl`
- Hosts WitRPC WebSocket server

### Service.UI

Blazor WebAssembly application that:
- Connects to the server via WebSocket
- Displays settings in a tabbed MudBlazor UI
- Provides type-specific editors for each setting
