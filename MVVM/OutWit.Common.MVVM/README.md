# OutWit.Common.MVVM

Cross-platform MVVM library providing base components for building modern .NET applications with WPF, Avalonia, and Blazor.

## Features

- **Cross-Platform ViewModelBase**: Base class for view models with `INotifyPropertyChanged` support
- **Command Implementations**:
  - `RelayCommand`: Simple command with manual `CanExecuteChanged` raising
  - `DelegateCommand<T>`: Generic typed command
- **Collections**:
  - `SortedCollection<TKey, TValue>`: Sorted collection with change notifications
  - `ObservableSortedCollection<TKey, TValue>`: Sorted collection observing item property changes
  - `SafeObservableCollection<T>`: Thread-safe observable collection
- **Table Models**: Data models for table views (`TableView`, `TableViewPage`, `TableViewRow`, etc.)
- **Abstractions**: `IDispatcher` for cross-platform thread marshalling

## Installation

```bash
dotnet add package OutWit.Common.MVVM
```

For platform-specific features:
```bash
# WPF
dotnet add package OutWit.Common.MVVM.WPF

# Avalonia
dotnet add package OutWit.Common.MVVM.Avalonia

# Blazor
dotnet add package OutWit.Common.MVVM.Blazor
```

## Quick Start

### ViewModelBase

```csharp
public class MyViewModel : ViewModelBase<IApplicationViewModel>
{
    private string m_name = "";

    public MyViewModel(IApplicationViewModel appVm) : base(appVm)
    {
    }

    public string Name
    {
        get => m_name;
        set
        {
            m_name = value;
            OnPropertyChanged();
        }
    }
}
```

### RelayCommand

```csharp
public class MyViewModel : ViewModelBase<IApplicationViewModel>
{
    public RelayCommand SaveCommand { get; }

    public MyViewModel(IApplicationViewModel appVm) : base(appVm)
    {
        SaveCommand = new RelayCommand(
            execute: _ => Save(),
            canExecute: _ => CanSave());
    }

    private void Save() { /* Save logic */ }
    
    private bool CanSave() => !string.IsNullOrEmpty(Name);

    private void OnNameChanged()
    {
        SaveCommand.RaiseCanExecuteChanged();
    }
}
```

### DelegateCommand<T>

```csharp
public DelegateCommand<string> SearchCommand { get; }

SearchCommand = new DelegateCommand<string>(
    execute: searchText => PerformSearch(searchText),
    canExecute: searchText => !string.IsNullOrEmpty(searchText));
```

### SortedCollection

Thread-safe sorted collection with change notifications:

```csharp
var items = new SortedCollection<int, Item>(x => x.Id);

items.ItemsAdded += (s, added) => Console.WriteLine($"Added {added.Count} items");
items.ItemsRemoved += (s, removed) => Console.WriteLine($"Removed {removed.Count} items");

items.Add(new Item { Id = 1, Name = "First" });
```

### ObservableSortedCollection

Observes property changes in collection items:

```csharp
var items = new ObservableSortedCollection<int, Item>(x => x.Id);

// Listen for item property changes
items.CollectionContentChanged += (sender, e) => 
{
    var item = sender as Item;
    Console.WriteLine($"Property {e.PropertyName} changed on item {item?.Id}");
};

items.Add(new Item { Id = 1, Name = "First" });
items[1].Name = "Updated"; // Triggers CollectionContentChanged
```

### SafeObservableCollection with IDispatcher

```csharp
// Thread-safe collection that marshals notifications to UI thread
var items = new SafeObservableCollection<Item>(dispatcher);

// Safe to call from background thread
await Task.Run(() => 
{
    foreach (var item in loadedItems)
    {
        items.Add(item); // UI updates happen on correct thread
    }
});
```

## Platform-Specific Packages

| Package | Platform | Features |
|---------|----------|----------|
| `OutWit.Common.MVVM.WPF` | WPF | DependencyProperty source generator, WPF commands, visual tree utilities |
| `OutWit.Common.MVVM.Avalonia` | Avalonia | StyledProperty/DirectProperty source generator, Avalonia utilities |
| `OutWit.Common.MVVM.Blazor` | Blazor | ViewModelBase for ComponentBase, async lifecycle support |

## Migration from v1.x

See the [Migration Guide](MIGRATION_GUIDE.md) for detailed instructions.

Key changes:
- Split into cross-platform base and platform-specific packages
- New source generator-based property system
- `BindableAttribute` is obsolete (use `StyledPropertyAttribute`)
- `SortedCollectionEx` renamed to `ObservableSortedCollection`

## Related Packages

- `OutWit.Common.MVVM.WPF` - WPF-specific implementation
- `OutWit.Common.MVVM.Avalonia` - Avalonia-specific implementation
- `OutWit.Common.MVVM.Blazor` - Blazor-specific implementation

## License

Non-Commercial License (NCL) - Free for personal, educational, and research purposes.  
For commercial use, contact licensing@ratner.io.

See [LICENSE](LICENSE) for full details.
