# OutWit.Common.MVVM

Cross-platform MVVM library providing base components for building modern .NET applications with WPF, Avalonia, and Blazor.

## Features

- **Cross-Platform ViewModelBase**: Base class for view models with `INotifyPropertyChanged` support
- **Command Implementations**:
  - `RelayCommand`: Simple command with manual `CanExecuteChanged` raising
  - `DelegateCommand<T>`: Generic typed command
- **Collections**:
  - `SortedCollection<TKey, TValue>`: Sorted collection with change notifications
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

# Avalonia (coming soon)
dotnet add package OutWit.Common.MVVM.Avalonia
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

    private void Save()
    {
        // Save logic
    }

    private bool CanSave()
    {
        return !string.IsNullOrEmpty(Name);
    }

    private void OnNameChanged()
    {
        SaveCommand.RaiseCanExecuteChanged();
    }
}
```

### SortedCollection

Thread-safe sorted collection with change notifications:

```csharp
public class MyViewModel : ViewModelBase<IApplicationViewModel>
{
    public SortedCollection<int, Item> Items { get; }

    public MyViewModel(IApplicationViewModel appVm) : base(appVm)
    {
        Items = new SortedCollection<int, Item>(x => x.Id);
        
        // Subscribe to events
        Items.ItemsAdded += OnItemsAdded;
        Items.ItemsRemoved += OnItemsRemoved;
    }

    private void OnItemsAdded(object? sender, IReadOnlyCollection<Item>? items)
    {
        // Handle items added
    }
}
```

### ObservableSortedCollection

Observes property changes in collection items:

```csharp
public class Item : INotifyPropertyChanged
{
    private string m_name = "";
    
    public int Id { get; set; }
    
    public string Name
    {
        get => m_name;
        set
        {
            m_name = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class MyViewModel : ViewModelBase<IApplicationViewModel>
{
    public ObservableSortedCollection<int, Item> Items { get; }

    public MyViewModel(IApplicationViewModel appVm) : base(appVm)
    {
        Items = new ObservableSortedCollection<int, Item>(x => x.Id);
        
        // Listen for item property changes
        Items.CollectionContentChanged += OnItemPropertyChanged;
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Handle item property change
        // sender is the item that changed
        // e.PropertyName is the property that changed
    }
}
```

### DelegateCommand<T>

```csharp
public class MyViewModel : ViewModelBase<IApplicationViewModel>
{
    public DelegateCommand<string> SearchCommand { get; }

    public MyViewModel(IApplicationViewModel appVm) : base(appVm)
    {
        SearchCommand = new DelegateCommand<string>(
            execute: searchText => PerformSearch(searchText),
            canExecute: searchText => !string.IsNullOrEmpty(searchText));
    }

    private void PerformSearch(string? searchText)
    {
        // Search logic
    }
}
```

### SafeObservableCollection with IDispatcher

```csharp
public class MyViewModel : ViewModelBase<IApplicationViewModel>
{
    public SafeObservableCollection<Item> Items { get; }

    public MyViewModel(IApplicationViewModel appVm, IDispatcher dispatcher) 
        : base(appVm)
    {
        Items = new SafeObservableCollection<Item>(dispatcher);
    }

    public async Task LoadItemsAsync()
    {
        var items = await GetItemsFromDatabaseAsync();
        
        // Safe to call from background thread
        // Collection will marshal notifications to UI thread
        foreach (var item in items)
        {
            Items.Add(item);
        }
    }
}
```

## Performance Features

### .NET 9+ Lock Optimization

For .NET 9 and later, the collections use the new `System.Threading.Lock` type for improved performance:

```csharp
// Automatically uses Lock on .NET 9+, object on earlier versions
var collection = new SortedCollection<int, Item>(x => x.Id);
```

### Thread-Safety

All collections are thread-safe with fine-grained locking:
- `SortedCollection<TKey, TValue>` - Thread-safe reads and writes
- `ObservableSortedCollection<TKey, TValue>` - Thread-safe with separate subscription lock
- `SafeObservableCollection<T>` - Thread-safe with dispatcher marshalling

## Migration from v1.x

If you're upgrading from OutWit.Common.MVVM 1.x, see the [Migration Guide](MIGRATION_GUIDE.md).

Key changes:
- Split into cross-platform base and platform-specific packages
- New source generator-based property system
- `BindableAttribute` is now obsolete (WPF-specific, use `StyledPropertyAttribute`)
- **`SortedCollectionEx` renamed to `ObservableSortedCollection`** (old name still works but is obsolete)

## Breaking Changes

### Collection Naming (v2.0)

`SortedCollectionEx<TKey, TValue>` has been renamed to `ObservableSortedCollection<TKey, TValue>` for better clarity.

**Migration:**
```csharp
// Old (still works with warning)
var collection = new SortedCollectionEx<int, Item>(x => x.Id);

// New (recommended)
var collection = new ObservableSortedCollection<int, Item>(x => x.Id);
```

The old name is kept as a type alias for backward compatibility but will show an obsolete warning.

### Generic Constraints (v2.0)

Collections now have `notnull` constraints for better null safety:

```csharp
// Before
SortedCollection<int?, string?> collection;

// After - requires non-nullable types
SortedCollection<int, string> collection;
