# OutWit.Common.Settings.Samples.Service

WebSocket-based settings service for remote settings management.

## What This Sample Demonstrates

### ?? Remote Settings Access

Expose application settings over a WebSocket connection using WitRPC:

- **Real-time Sync** - Changes propagate instantly to connected clients
- **Network Protocol** - MemoryPack binary serialization for efficiency
- **Service Interface** - Clean contract-based API

### ?? Service Architecture

```
???????????????????????????????????????
?  Blazor WebAssembly Client          ?
?  (Service.UI)                       ?
?                                     ?
?  ???????????????????????????????   ?
?  ? ISettingsService (proxy)    ?   ?
?  ???????????????????????????????   ?
???????????????????????????????????????
                ? WebSocket (ws://localhost:5050)
                ?
???????????????????????????????????????
?  WebSocket Server                   ?
?  (Service)                          ?
?                                     ?
?  ???????????????????????????????   ?
?  ? SettingsServiceImpl         ?   ?
?  ?   ??? ApplicationModule     ?   ?
?  ?   ??? NetworkModule         ?   ?
?  ?   ??? AdvancedModule        ?   ?
?  ?   ??? SharedDatabaseModule  ?   ?
?  ???????????????????????????????   ?
???????????????????????????????????????
```

## Service Contract

```csharp
[WitRpcService]
public interface ISettingsService
{
    Task<IReadOnlyList<SettingsGroupInfo>> GetGroupsAsync();
    Task<IReadOnlyList<ISettingsValue>> GetValuesAsync(string group);
    Task UpdateValueAsync(string group, string key, string value);
    Task ResetValueAsync(string group, string key);
    Task ResetGroupAsync(string group);
    Task SaveAsync();
    Task ReloadAsync();
}
```

## Key Components

### SettingsServiceImpl

The main service implementation aggregates multiple settings managers:

```csharp
public class SettingsServiceImpl : ISettingsService
{
    private readonly List<ISettingsManager> _managers;

    public async Task<IReadOnlyList<SettingsGroupInfo>> GetGroupsAsync()
    {
        return _managers
            .SelectMany(m => m.Groups)
            .OrderBy(g => g.Priority)
            .ToList();
    }

    public async Task UpdateValueAsync(string group, string key, string value)
    {
        var manager = FindManagerForGroup(group);
        manager.SetValue(group, key, value);
    }
}
```

### Server Startup (Program.cs)

```csharp
var appModule = new ApplicationModule();
appModule.Initialize();

// ... initialize other modules ...

var service = new SettingsServiceImpl(
    appModule.Manager,
    netModule.Manager,
    advModule.Manager,
    sharedModule.Manager);

await WitRpcServer.RunAsync(options =>
{
    options.BaseUrl = "ws://localhost:5050";
    options.ApiPath = "api";
    options.RegisterService<ISettingsService>(service);
});
```

## Running

### Start the Server

```bash
dotnet run --project Settings/Samples/Services/OutWit.Common.Settings.Samples.Service
```

Server starts on `ws://localhost:5050/api`.

### Start the Client

```bash
dotnet run --project Settings/Samples/Services/OutWit.Common.Settings.Samples.Service.UI
```

Blazor app opens at `https://localhost:59065`.

## Files

| File | Purpose |
|------|---------|
| `Program.cs` | Server startup, module initialization |
| `Services/SettingsServiceImpl.cs` | ISettingsService implementation |
| `Properties/launchSettings.json` | Server launch configuration |

## Related Projects

- **Service.Contracts** - `ISettingsService` interface definition
- **Service.UI** - Blazor WebAssembly client
