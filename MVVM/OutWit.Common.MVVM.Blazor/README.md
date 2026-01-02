# OutWit.Common.MVVM.Blazor

Blazor-specific MVVM components and utilities for building Blazor applications with the MVVM pattern.

## Features

- **ViewModelBase**: Base class combining `ComponentBase` with `INotifyPropertyChanged`
- **ViewModelBaseAsync**: Extended base with async lifecycle support and error handling
- **BlazorDispatcher**: `IDispatcher` implementation for UI thread invocation
- **SafeObservableCollection**: Collection with automatic `StateHasChanged` callbacks
- **Component Extensions**: Helper methods for Blazor components

## Installation

```bash
dotnet add package OutWit.Common.MVVM.Blazor
```

This automatically includes:
- `OutWit.Common.MVVM` (base cross-platform package)
- `OutWit.Common.Logging`

## Quick Start

### ViewModelBase

The simplest way to create a Blazor component with MVVM support:

```csharp
@inherits ViewModelBase

<h1>Counter: @Count</h1>
<button @onclick="IncrementCount" disabled="@Busy">
    @(Busy ? "Loading..." : "Increment")
</button>

@code {
    private int _count;
    
    public int Count
    {
        get => _count;
        set => SetProperty(ref _count, value);
    }

    private async Task IncrementCount()
    {
        await RunAsync(async () =>
        {
            await Task.Delay(500); // Simulate work
            Count++;
        });
    }
}
```

### ViewModelBase Features

```csharp
public class MyViewModel : ViewModelBase
{
    private string _name = "";
    private int _count;

    // Property with automatic change notification
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public int Count
    {
        get => _count;
        set => SetProperty(ref _count, value);
    }

    // Run async operation with automatic Busy state management
    public async Task LoadDataAsync()
    {
        await RunAsync(async () =>
        {
            var data = await FetchDataAsync();
            Name = data.Name;
        });
        // Busy is automatically set to true/false
        // StateHasChanged is called automatically after completion
    }

    // Safe execution with error handling
    public void SafeOperation()
    {
        // Returns default on exception
        var result = Check(() => int.Parse("invalid"), defaultValue: 0);
        
        // Returns false on exception
        var success = Check(() => RiskyOperation());
        
        // Swallows exception
        Check(() => MayThrow());
    }

    // React to property changes
    protected override void OnPropertyChanged(string? propertyName)
    {
        if (propertyName == nameof(Name))
        {
            // Handle name change
        }
    }
}
```

### ViewModelBaseAsync

Extended base class with async initialization and error handling:

```csharp
@inherits ViewModelBaseAsync

@if (HasError)
{
    <div class="alert alert-danger">@Error</div>
}
else if (Busy)
{
    <div>Loading...</div>
}
else
{
    <div>@Data</div>
}

@code {
    public string? Data { get; private set; }

    // Called during OnInitializedAsync - errors are caught automatically
    protected override async Task InitializeAsync()
    {
        Data = await LoadDataAsync();
    }

    // Optional: Handle initialization errors
    protected override void OnInitializationError(Exception ex)
    {
        // Log error, show notification, etc.
    }
}
```

### SafeObservableCollection

Collection that automatically notifies UI when modified:

```csharp
@inherits ViewModelBase

<ul>
    @foreach (var item in Items)
    {
        <li>@item</li>
    }
</ul>

@code {
    // Collection with automatic StateHasChanged callback
    public SafeObservableCollection<string> Items { get; private set; } = null!;

    protected override void OnInitialized()
    {
        // Pass StateHasChanged action to automatically refresh UI on changes
        Items = new SafeObservableCollection<string>(() => StateHasChanged());
        Items.Add("Item 1");
        Items.Add("Item 2");
    }

    private void AddItem(string item)
    {
        Items.Add(item); // UI automatically updates
    }
}
```

### Component Extensions

```csharp
using OutWit.Common.MVVM.Blazor.Utils;

// Force UI update
component.ForceUpdate();

// Run with busy state
await component.RunWithBusyAsync(
    () => _busy,
    v => _busy = v,
    async () => await DoWorkAsync()
);
```

## ViewModelBase API

| Member | Type | Description |
|--------|------|-------------|
| `Busy` | `bool` | Indicates if async operation is running |
| `PropertyChanged` | `event` | Fired when property value changes |
| `SetProperty<T>` | `method` | Sets property and raises PropertyChanged |
| `RaisePropertyChanged` | `method` | Manually raises PropertyChanged |
| `RunAsync` | `method` | Runs async operation with Busy management |
| `Check<T>` | `method` | Executes with error handling |
| `InvokeOnUIAsync` | `method` | Invokes on UI thread |

## ViewModelBaseAsync API

Includes all ViewModelBase members plus:

| Member | Type | Description |
|--------|------|-------------|
| `Error` | `string?` | Error message from initialization |
| `HasError` | `bool` | Indicates if there's an error |
| `InitializeAsync` | `method` | Override for async initialization |
| `OnInitializationError` | `method` | Called when initialization fails |
| `DisposeAsync` | `method` | Async dispose support |

## Design Considerations

### Why No Source Generator?

Unlike WPF and Avalonia, Blazor doesn't have a `DependencyProperty` or `StyledProperty` equivalent. Blazor uses:

- `[Parameter]` attribute for component parameters (built-in)
- `[CascadingParameter]` for cascading values (built-in)
- Standard C# properties with `INotifyPropertyChanged`

The existing Blazor infrastructure handles these patterns well, so a source generator isn't necessary.

### Differences from WPF/Avalonia

| Feature | WPF/Avalonia | Blazor |
|---------|--------------|--------|
| Property System | DependencyProperty/StyledProperty | Standard C# + INotifyPropertyChanged |
| UI Thread | Dispatcher | SynchronizationContext + InvokeAsync |
| Visual Tree | Yes | Render Tree (different model) |
| Data Binding | XAML Binding | Razor @bind |

## Related Packages

- `OutWit.Common.MVVM` - Cross-platform base classes
- `OutWit.Common.MVVM.WPF` - WPF-specific implementation
- `OutWit.Common.MVVM.Avalonia` - Avalonia-specific implementation

## License

Non-Commercial License (NCL) - Free for personal, educational, and research purposes.  
For commercial use, contact licensing@ratner.io.

See [LICENSE](LICENSE) for full details.
