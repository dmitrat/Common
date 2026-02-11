# OutWit.Common.Settings.Samples.Service.UI

Blazor WebAssembly client for remote settings management.

## What This Sample Demonstrates

### ??? Web-Based Settings Editor

A responsive web UI for managing settings remotely:

- **MudBlazor Components** - Modern Material Design UI
- **Real-time Updates** - Immediate feedback on value changes
- **Type-aware Editors** - Automatic editor selection based on value type
- **Reset Functionality** - Reset individual values or entire groups

### ?? Features

| Feature | Description |
|---------|-------------|
| **Tabbed Groups** | Settings organized by group in tabs |
| **Type Editors** | Boolean (switch), Numeric (input), Enum (select), Color (RGB sliders) |
| **Change Indicator** | Modified values shown with bold text |
| **Reset Button** | Undo icon to reset individual values |
| **Save/Cancel** | Standard workflow with explicit save |

## UI Components

### SettingsPage.razor

The main page renders settings dynamically based on their type:

```razor
@switch (value)
{
    case SettingsValue<bool> v:
        <MudSwitch T="bool" Value="@v.Value" 
                   ValueChanged="@(val => OnValueChanged(...))" />
        break;
        
    case SettingsValue<int> v:
        <MudNumericField T="int" Value="@v.Value" 
                         ValueChanged="@(val => OnValueChanged(...))" />
        break;
        
    case SettingsValue<BoundedInt> v:
        <MudSlider T="int" Value="@v.Value.Value" 
                   Min="@v.Value.Min" Max="@v.Value.Max" />
        break;
        
    case SettingsValue<ColorRgb> v:
        <MudNumericField T="byte" Label="R" Value="@v.Value.R" ... />
        <MudNumericField T="byte" Label="G" Value="@v.Value.G" ... />
        <MudNumericField T="byte" Label="B" Value="@v.Value.B" ... />
        <MudPaper Style="@($"background:rgb({v.Value.R},{v.Value.G},{v.Value.B})")" />
        break;
}
```

### Client Configuration (Program.cs)

```csharp
// Register custom MemoryPack serializers for network transfer
SettingsBuilder.RegisterMemoryPack(b => b.AddCustomSerializers());

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddMudServices();

// Configure WebSocket connection to settings server
builder.Services.AddWitRpcChannel(options =>
{
    options.BaseUrl = "ws://localhost:5050";
    options.ApiPath = "api";
    options.ConfigureClient = client =>
    {
        client.WithMemoryPack();
        client.WithoutAuthorization();
    };
});
```

## Architecture

```
Program.cs
    ?
    ??? SettingsBuilder.RegisterMemoryPack()  // Custom type serializers
    ?
    ??? AddWitRpcChannel()                    // WebSocket client setup
            ?
            ?
    IChannelFactory (injected)
            ?
            ?
    SettingsPage.razor
            ?
            ??? ISettingsService (proxy)
                    ?
                    ??? GetGroupsAsync()
                    ??? GetValuesAsync(group)
                    ??? UpdateValueAsync(group, key, value)
                    ??? ResetValueAsync(group, key)
                    ??? SaveAsync()
```

## Running

### Prerequisites

Start the settings server first:

```bash
dotnet run --project Settings/Samples/Services/OutWit.Common.Settings.Samples.Service
```

### Start the Blazor Client

```bash
dotnet run --project Settings/Samples/Services/OutWit.Common.Settings.Samples.Service.UI
```

Open browser at `https://localhost:59065`.

## Files

| File | Purpose |
|------|---------|
| `Program.cs` | WebAssembly host configuration, WitRPC setup |
| `Pages/SettingsPage.razor` | Main settings editor page |
| `App.razor` | Blazor router |
| `MainLayout.razor` | MudBlazor layout with app bar |
| `_Imports.razor` | Global using statements |
| `wwwroot/index.html` | HTML host page |

## Dependencies

- **MudBlazor** - Material Design component library
- **OutWit.Communication.Client.Blazor** - WitRPC WebSocket client
- **MemoryPack** - Binary serialization for network transfer
