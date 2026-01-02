# OutWit.Common.MVVM.WPF

WPF-specific MVVM components and utilities, including source generator for automatic DependencyProperty generation.

## Features

- **Source Generator for DependencyProperty**: Automatically generate DependencyProperty from attributes
- **WPF Commands**: `Command` and `DelegateCommand` with `CommandManager` integration
- **Binding Utilities**: Helper methods for DependencyProperty registration
- **Visual Tree Traversal**: Extension methods for navigating WPF visual tree
- **BindingProxy**: Freezable binding proxy for DataContext access
- **Legacy Support**: Obsolete `BindableAttribute` for backward compatibility

## Installation

```bash
dotnet add package OutWit.Common.MVVM.WPF
```

This automatically includes:
- `OutWit.Common.MVVM` (base cross-platform package)
- `OutWit.Common.MVVM.Abstractions` (attributes)
- `OutWit.Common.MVVM.WPF.Generator` (source generator)

## Quick Start

### Source Generator for DependencyProperty

The simplest way to create DependencyProperties:

```csharp
using System.Windows.Controls;
using OutWit.Common.MVVM.Attributes;

namespace MyApp.Controls
{
    public partial class CustomButton : Button
    {
        [StyledProperty(DefaultValue = "Click Me")]
        public string Label { get; set; }

        [StyledProperty(AffectsMeasure = true)]
        public double IconSize { get; set; }
    }
}
```

**Important**: Mark your class as `partial` to allow source generator to add code.

The generator automatically creates:
```csharp
// Generated code (you don't write this):
public static readonly DependencyProperty LabelProperty = ...;
public static readonly DependencyProperty IconSizeProperty = ...;
```

### Advanced Property Generation

```csharp
public partial class AdvancedControl : Control
{
    // Full explicit configuration
    [StyledProperty(
        DefaultValue = 100.0,
        AffectsMeasure = true,
        AffectsArrange = true,
        BindsTwoWayByDefault = true,
        OnChanged = nameof(OnWidthChanged))]
    public double CustomWidth { get; set; }

    private static void OnWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (AdvancedControl)d;
        // Handle change
    }
}
```

### Convention-Based Callbacks (NEW!)

The generator automatically discovers callback methods by naming convention:

```csharp
public partial class SmartControl : Control
{
    // No need to specify OnChanged - automatically discovered!
    [StyledProperty(DefaultValue = "Hello")]
    public string Title { get; set; }

    // Convention: On{PropertyName}Changed
    private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (SmartControl)d;
        // Handle title change
    }

    // No need to specify Coerce - automatically discovered!
    [StyledProperty(DefaultValue = 100.0)]
    public double Width { get; set; }

    // Convention: {PropertyName}Coerce
    private static object WidthCoerce(DependencyObject d, object value)
    {
        return Math.Max(0, (double)value); // Ensure non-negative
    }
}
```

**Benefits:**
- ? Less boilerplate code
- ? Cleaner attributes
- ? Compile-time safety
- ? Override with explicit parameter when needed

### Attached Properties

```csharp
public static partial class MyAttachedProperties
{
    [AttachedProperty(DefaultValue = false)]
    public static bool IsHighlighted { get; set; }
}

// Usage in XAML:
// <Button local:MyAttachedProperties.IsHighlighted="True" />
```

### WPF Commands

```csharp
using OutWit.Common.MVVM.WPF.Commands;

public class MyViewModel
{
    public DelegateCommand SaveCommand { get; }

    public MyViewModel()
    {
        SaveCommand = new DelegateCommand(
            execute: _ => Save(),
            canExecute: _ => CanSave());
    }

    private void Save() { }
    private bool CanSave() => true;
}
```

### Binding Utilities

Manual DependencyProperty registration (for when you can't use source generator):

```csharp
using OutWit.Common.MVVM.WPF.Utils;

public class MyControl : Control
{
    public static readonly DependencyProperty TextProperty = 
        BindingUtils.Register<MyControl, string>(nameof(Text), "Default");

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
}
```

### Visual Tree Traversal

```csharp
using OutWit.Common.MVVM.WPF.Utils;

// Find first child of specific type
var button = myPanel.FindFirstChildOf<Button>();

// Find with predicate
var redButton = myPanel.FindFirstChildOf<Button>(b => b.Background == Brushes.Red);

// Find all children
var allButtons = myPanel.FindAllChildrenOf<Button>();

// Find parent
var window = myButton.FindFirstParentOf<Window>();
```

### BindingProxy for DataContext Access

```xaml
<Window.Resources>
    <local:BindingProxy x:Key="Proxy" Data="{Binding}" />
</Window.Resources>

<DataGrid>
    <DataGrid.Columns>
        <DataGridTemplateColumn>
            <DataGridTemplateColumn.CellTemplate>
                <DataTemplate>
                    <!-- Access parent DataContext from inside DataGrid -->
                    <Button Command="{Binding Data.DeleteCommand, Source={StaticResource Proxy}}"
                            CommandParameter="{Binding}" />
                </DataTemplate>
            </DataGridTemplateColumn.CellTemplate>
        </DataGrid.Columns>
    </DataGrid>
</Window.Resources>
```

## StyledProperty Options

| Option | Type | Description |
|--------|------|-------------|
| `PropertyName` | `string` | Override property name (default: `{Name}Property`) |
| `DefaultValue` | `object` | Default value |
| `BindsTwoWayByDefault` | `bool` | Enable two-way binding by default |
| `AffectsMeasure` | `bool` | Invalidate measure on change |
| `AffectsArrange` | `bool` | Invalidate arrange on change |
| `AffectsRender` | `bool` | Invalidate render on change |
| `Inherits` | `bool` | Value inherited by child elements |
| `OnChanged` | `string` | PropertyChangedCallback method name |
| `Coerce` | `string` | CoerceValueCallback method name |

## Migration from Old BindableAttribute

See [Migration Guide](../OutWit.Common.MVVM/MIGRATION_GUIDE.md) for detailed instructions.

**Quick summary:**
1. Change `[Bindable]` to `[StyledProperty]`
2. Remove manual `DependencyProperty` declarations
3. Mark class as `partial`
4. Move options to attribute parameters

## Legacy BindableAttribute (Deprecated)

The old `BindableAttribute` using AspectInjector is still available but **deprecated**:

```csharp
[Obsolete("Use StyledPropertyAttribute instead")]
public class BindableAttribute : Attribute { }
```

**Recommendation**: Migrate to `StyledPropertyAttribute` for better IDE support and debugging.

## Related Packages

- `OutWit.Common.MVVM` - Cross-platform base
- `OutWit.Common.MVVM.Abstractions` - Attribute definitions
- `OutWit.Common.MVVM.WPF.Generator` - Source generator

## License

MIT License - see LICENSE file for details
