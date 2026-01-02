# OutWit.Common.MVVM.Avalonia.Sample

Sample application demonstrating the features of **OutWit.Common.MVVM.Avalonia** library.

## Features Demonstrated

### 1. StyledProperty (Source Generated)

**`Controls/DemoStyledControl.cs`** shows how to use `[StyledProperty]` attribute:

```csharp
using OutWit.Common.MVVM.Attributes;

public partial class DemoStyledControl : UserControl
{
    [StyledProperty(DefaultValue = "Untitled")]
    public string Title { get; set; }

    [StyledProperty(DefaultValue = 0)]
    public int Counter { get; set; }

    [StyledProperty(DefaultValue = false, BindsTwoWayByDefault = true)]
    public bool IsHighlighted { get; set; }

    // Convention-based callback - automatically discovered!
    private void OnIsHighlightedChanged(AvaloniaPropertyChangedEventArgs<bool> e)
    {
        UpdateHighlight(e.NewValue.Value);
    }
}
```

### 2. DirectProperty (High Performance)

**`Controls/DemoDirectControl.cs`** shows `[DirectProperty]` for frequently changing values:

```csharp
public partial class DemoDirectControl : UserControl
{
    // DirectProperty doesn't participate in styling, but is faster
    [DirectProperty(DefaultValue = 0.0)]
    public double Progress { get; set; }

    [DirectProperty(DefaultValue = "", BindsTwoWayByDefault = true)]
    public string StatusText { get; set; }
}
```

### 3. RelayCommand (Cross-Platform)

**`MainViewModel.cs`** uses cross-platform RelayCommand:

```csharp
using OutWit.Common.MVVM.Commands;

// RelayCommand works on all platforms
IncrementCommand = new RelayCommand(_ => Counter++);
DecrementCommand = new RelayCommand(_ => Counter--, _ => Counter > 0);

// Manual CanExecute update
DecrementCommand.RaiseCanExecuteChanged();
```

## Property Types Comparison

| Type | Use Case | Style System | Performance |
|------|----------|--------------|-------------|
| `StyledProperty` | Most properties, themeable | Yes | Normal |
| `DirectProperty` | Frequently changing (animations, progress) | No | Better |

## Running the Sample

```bash
cd MVVM/OutWit.Common.MVVM.Avalonia.Sample
dotnet run
```

## Key Points

1. **Mark classes as `partial`** - Required for source generator
2. **Use `OutWit.Common.MVVM.Attributes` namespace** for attributes
3. **Choose property type wisely**:
   - Use `StyledProperty` for most cases
   - Use `DirectProperty` for performance-critical values
4. **Convention callbacks** - `On{PropertyName}Changed` methods are auto-discovered

## License

Non-Commercial License (NCL) - See LICENSE file.
