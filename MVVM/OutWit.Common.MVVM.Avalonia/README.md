# OutWit.Common.MVVM.Avalonia

Avalonia-specific MVVM components and utilities, including source generator for automatic StyledProperty, DirectProperty, and AttachedProperty generation.

## Features

- **Source Generator for Properties**: Automatically generate StyledProperty, DirectProperty, and AttachedProperty from attributes
- **AvaloniaDispatcher**: `IDispatcher` implementation for UI thread invocation
- **Binding Utilities**: Helper methods for property registration
- **Visual Tree Traversal**: Extension methods for navigating Avalonia visual tree
- **BindingProxy**: Binding proxy for DataContext access
- **DataTemplate Utilities**: Helper methods for DataTemplate creation

## Installation

```bash
dotnet add package OutWit.Common.MVVM.Avalonia
```

This automatically includes:
- `OutWit.Common.MVVM` (base cross-platform package)
- `OutWit.Common.MVVM.Avalonia.Generator` (source generator)
- `OutWit.Common.Logging`

## Quick Start

### Source Generator for StyledProperty

The simplest way to create StyledProperties:

```csharp
using Avalonia.Controls;
using OutWit.Common.MVVM.Avalonia.Attributes;

namespace MyApp.Controls
{
    public partial class CustomButton : Button
    {
        [StyledProperty(DefaultValue = "Click Me")]
        public string Label { get; set; }

        [StyledProperty(BindsTwoWayByDefault = true)]
        public bool IsPressed { get; set; }
    }
}
```

**Important**: Mark your class as `partial` to allow source generator to add code.

The generator automatically creates:
```csharp
// Generated code (you don't write this):
public static readonly StyledProperty<string> LabelProperty = 
    AvaloniaProperty.Register<CustomButton, string>(nameof(Label), "Click Me");
public static readonly StyledProperty<bool> IsPressedProperty = 
    AvaloniaProperty.Register<CustomButton, bool>(nameof(IsPressed), defaultBindingMode: BindingMode.TwoWay);
```

### DirectProperty for Performance

Use DirectProperty for frequently changing values (better performance, no style system participation):

```csharp
public partial class CounterControl : Control
{
    [DirectProperty(DefaultValue = 0)]
    public int Counter { get; set; }

    [DirectProperty(BindsTwoWayByDefault = true)]
    public string Text { get; set; }
}
```

Generated code:
```csharp
// Backing field is generated automatically
private int m_counter = 0;
public static readonly DirectProperty<CounterControl, int> CounterProperty = 
    AvaloniaProperty.RegisterDirect<CounterControl, int>(
        nameof(Counter), 
        o => o.m_counter, 
        (o, v) => o.m_counter = v,
        unsetValue: 0);
```

### Attached Properties

```csharp
using OutWit.Common.MVVM.Avalonia.Attributes;

public static partial class MyAttachedProperties
{
    [AttachedProperty(DefaultValue = false)]
    public static bool IsHighlighted { get; set; }

    [AttachedProperty(DefaultValue = 1.0, Inherits = true)]
    public static double Opacity { get; set; }
}

// Usage in AXAML:
// <Button local:MyAttachedProperties.IsHighlighted="True" />
```

Generated code includes Get/Set methods:
```csharp
public static bool GetIsHighlighted(AvaloniaObject obj) => obj.GetValue(IsHighlightedProperty);
public static void SetIsHighlighted(AvaloniaObject obj, bool value) => obj.SetValue(IsHighlightedProperty, value);
```

### Convention-Based Callbacks

The generator automatically discovers callback methods by naming convention:

```csharp
public partial class SmartControl : Control
{
    // No need to specify OnChanged - automatically discovered!
    [StyledProperty(DefaultValue = "Hello")]
    public string Title { get; set; }

    // Convention: On{PropertyName}Changed
    private void OnTitleChanged(AvaloniaPropertyChangedEventArgs<string> e)
    {
        // Handle title change
    }

    // Convention: {PropertyName}Coerce
    private string TitleCoerce(AvaloniaObject sender, string value)
    {
        return value?.Trim() ?? "";
    }
}
```

### Visual Tree Traversal

```csharp
using OutWit.Common.MVVM.Avalonia.Utils;

// Find first child of specific type
var button = myPanel.FindFirstChildOf<Button>();

// Find all children
var allButtons = myPanel.FindAllChildrenOf<Button>();

// Find parent
var window = myButton.FindFirstParentOf<Window>();
```

### BindingProxy for DataContext Access

```xml
<UserControl.Resources>
    <local:BindingProxy x:Key="Proxy" Data="{Binding}" />
</UserControl.Resources>

<DataGrid Items="{Binding Items}">
    <DataGrid.Columns>
        <DataGridTemplateColumn>
            <DataTemplate>
                <Button Command="{Binding Data.DeleteCommand, Source={StaticResource Proxy}}"
                        CommandParameter="{Binding}" />
            </DataTemplate>
        </DataGridTemplateColumn>
    </DataGrid.Columns>
</DataGrid>
```

### AvaloniaDispatcher

```csharp
using OutWit.Common.MVVM.Avalonia.Abstractions;

// Get dispatcher for current thread
var dispatcher = AvaloniaDispatcher.UIThread;

// Invoke on UI thread
dispatcher.Invoke(() => UpdateUI());

// Async invoke
await dispatcher.InvokeAsync(() => UpdateUI());
```

## Property Types Comparison

| Type | Use Case | Style System | Performance |
|------|----------|--------------|-------------|
| `StyledProperty` | Most properties | Yes | Normal |
| `DirectProperty` | Frequently changing values | No | Better |
| `AttachedProperty` | Properties on other objects | Yes | Normal |

## StyledProperty Options

| Option | Type | Description |
|--------|------|-------------|
| `PropertyName` | `string` | Override property name (default: `{Name}Property`) |
| `DefaultValue` | `object` | Default value |
| `BindsTwoWayByDefault` | `bool` | Enable two-way binding by default |
| `Inherits` | `bool` | Value inherited by child elements |
| `OnChanged` | `string` | Property changed callback method name |
| `Coerce` | `string` | Coerce value callback method name |

## DirectProperty Options

| Option | Type | Description |
|--------|------|-------------|
| `PropertyName` | `string` | Override property name |
| `DefaultValue` | `object` | Default value (also used as unset value) |
| `BindsTwoWayByDefault` | `bool` | Enable two-way binding by default |
| `OnChanged` | `string` | Property changed callback method name |

## AttachedProperty Options

| Option | Type | Description |
|--------|------|-------------|
| `PropertyName` | `string` | Override property name |
| `DefaultValue` | `object` | Default value |
| `Inherits` | `bool` | Value inherited by child elements |
| `OnChanged` | `string` | Property changed callback method name |
| `Coerce` | `string` | Coerce value callback method name |

## Related Packages

- `OutWit.Common.MVVM` - Cross-platform base classes
- `OutWit.Common.MVVM.WPF` - WPF-specific implementation
- `OutWit.Common.MVVM.Blazor` - Blazor-specific implementation

## License

Non-Commercial License (NCL) - Free for personal, educational, and research purposes.  
For commercial use, contact licensing@ratner.io.

See [LICENSE](LICENSE) for full details.
