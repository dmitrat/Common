# Migration Guide: From BindableAttribute to StyledPropertyAttribute

## Overview

The `OutWit.Common.MVVM` library has been refactored to support cross-platform development (WPF, Avalonia, and potentially Blazor). The old `BindableAttribute` based on AspectInjector is now deprecated in favor of the new `StyledPropertyAttribute` that uses Roslyn Source Generators.

## Why Migrate?

### Benefits of StyledPropertyAttribute

1. **Better IDE Support**: Source generators provide full IntelliSense and code navigation
2. **Easier Debugging**: Generated code is visible and can be stepped through
3. **Compile-Time Safety**: Errors are caught during compilation, not at runtime
4. **Cross-Platform**: Works with both WPF and Avalonia
5. **No Runtime Overhead**: All code is generated at compile-time

### Limitations of Old BindableAttribute

- Runtime weaving complexity
- Limited IDE support
- Debugging difficulties
- WPF-only support

## Migration Steps

### Step 1: Update Package References

**Old (WPF-specific):**
```xml
<PackageReference Include="OutWit.Common.MVVM" Version="1.x.x" />
```

**New (Cross-platform):**
```xml
<ItemGroup>
    <PackageReference Include="OutWit.Common.MVVM.WPF" Version="2.x.x" />
</ItemGroup>
```

### Step 2: Update Using Directives

**Old:**
```csharp
using OutWit.Common.MVVM.Aspects;
```

**New:**
```csharp
using OutWit.Common.MVVM.Attributes;
```

### Step 3: Update Property Declarations

#### Simple Property

**Old:**
```csharp
public partial class MyControl : Control
{
    public static readonly DependencyProperty TextProperty = 
        BindingUtils.Register<MyControl, string>(nameof(Text));

    [Bindable]
    public string Text { get; set; }
}
```

**New:**
```csharp
public partial class MyControl : Control
{
    [StyledProperty]
    public string Text { get; set; }
}
```

#### Property with Default Value

**Old:**
```csharp
public static readonly DependencyProperty TextProperty = 
    BindingUtils.Register<MyControl, string>(nameof(Text), "Default");

[Bindable]
public string Text { get; set; }
```

**New:**
```csharp
[StyledProperty(DefaultValue = "Default")]
public string Text { get; set; }
```

#### Property with Metadata Options

**Old:**
```csharp
public static readonly DependencyProperty WidthProperty = 
    BindingUtils.Register<MyControl, double>(
        nameof(Width), 
        FrameworkPropertyMetadataOptions.AffectsMeasure, 
        100.0);

[Bindable]
public double Width { get; set; }
```

**New:**
```csharp
[StyledProperty(DefaultValue = 100.0, AffectsMeasure = true)]
public double Width { get; set; }
```

#### Property with PropertyChanged Callback

**Old:**
```csharp
public static readonly DependencyProperty TextProperty = 
    BindingUtils.Register<MyControl, string>(
        nameof(Text), 
        OnTextChanged);

[Bindable]
public string Text { get; set; }

private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
{
    var control = (MyControl)d;
    control.HandleTextChanged();
}
```

**New (Convention-Based - Recommended):**
```csharp
[StyledProperty]
public string Text { get; set; }

// Method automatically discovered by naming convention: On{PropertyName}Changed
private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
{
    var control = (MyControl)d;
    control.HandleTextChanged();
}
```

**New (Explicit):**
```csharp
// Use this if your method has a different name
[StyledProperty(OnChanged = nameof(HandleTextChanged))]
public string Text { get; set; }

private static void HandleTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
{
    var control = (MyControl)d;
    control.HandleTextChanged();
}
```

#### Attached Property

**Old:**
```csharp
public static readonly DependencyProperty IsHighlightedProperty = 
    BindingUtils.Attach<MyHelper, bool>(nameof(IsHighlighted));

public static bool GetIsHighlighted(DependencyObject obj)
{
    return (bool)obj.GetValue(IsHighlightedProperty);
}

public static void SetIsHighlighted(DependencyObject obj, bool value)
{
    obj.SetValue(IsHighlightedProperty, value);
}
```

**New:**
```csharp
public static class MyHelper
{
    [AttachedProperty]
    public static bool IsHighlighted { get; set; }
}
```

### Step 4: Remove Manual DependencyProperty Declarations

With the new approach, you **don't need** to manually declare `DependencyProperty` fields. The source generator creates them automatically.

**Remove these lines:**
```csharp
public static readonly DependencyProperty TextProperty = ...
```

### Step 5: Mark Class as Partial

The source generator needs to add code to your class, so it must be marked as `partial`:

**Old:**
```csharp
public class MyControl : Control
{
    // ...
}
```

**New:**
```csharp
public partial class MyControl : Control
{
    // ...
}
```

## Complete Migration Example

### Before (Old Approach)

```csharp
using System.Windows;
using System.Windows.Controls;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.WPF.Utils;

namespace MyApp.Controls
{
    public class CustomButton : Button
    {
        public static readonly DependencyProperty LabelProperty = 
            BindingUtils.Register<CustomButton, string>(
                nameof(Label), 
                FrameworkPropertyMetadataOptions.AffectsMeasure,
                "Click Me",
                OnLabelChanged);

        [Bindable]
        public string Label { get; set; }

        public static readonly DependencyProperty IsHighlightedProperty = 
            BindingUtils.Register<CustomButton, bool>(
                nameof(IsHighlighted), 
                FrameworkPropertyMetadataOptions.AffectsRender);

        [Bindable]
        public bool IsHighlighted { get; set; }

        private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var button = (CustomButton)d;
            button.UpdateLabel();
        }

        private void UpdateLabel()
        {
            // Update logic
        }
    }
}
```

### After (New Approach)

```csharp
using System.Windows;
using System.Windows.Controls;
using OutWit.Common.MVVM.Attributes;

namespace MyApp.Controls
{
    public partial class CustomButton : Button
    {
        [StyledProperty(
            DefaultValue = "Click Me",
            AffectsMeasure = true,
            OnChanged = nameof(OnLabelChanged))]
        public string Label { get; set; }

        [StyledProperty(AffectsRender = true)]
        public bool IsHighlighted { get; set; }

        private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var button = (CustomButton)d;
            button.UpdateLabel();
        }

        private void UpdateLabel()
        {
            // Update logic
        }
    }
}
```

## Available StyledProperty Options

| Property | Type | Description |
|----------|------|-------------|
| `PropertyName` | `string` | Override the generated property name (default: `{PropertyName}Property`) |
| `DefaultValue` | `object` | Default value for the property |
| `BindsTwoWayByDefault` | `bool` | Property binds two-way by default (WPF only) |
| `AffectsMeasure` | `bool` | Changes affect layout measure pass |
| `AffectsArrange` | `bool` | Changes affect layout arrange pass |
| `AffectsRender` | `bool` | Changes affect rendering |
| `Inherits` | `bool` | Property value is inherited by child elements |
| `OnChanged` | `string` | Method name for PropertyChangedCallback |
| `Coerce` | `string` | Method name for CoerceValueCallback (WPF only) |

## Convention-Based Callback Discovery

The source generator automatically discovers callback methods by naming convention, eliminating the need to explicitly specify them in the attribute:

### PropertyChanged Callback Convention

**Convention:** `On{PropertyName}Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)`

**Example:**
```csharp
[StyledProperty]
public string Title { get; set; }

// Automatically discovered - no need for OnChanged parameter
private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
{
    var control = (MyControl)d;
    // Handle title change
}
```

### Coerce Callback Convention

**Convention:** `{PropertyName}Coerce(DependencyObject d, object value)`

**Example:**
```csharp
[StyledProperty]
public double Width { get; set; }

// Automatically discovered - no need for Coerce parameter
private static object WidthCoerce(DependencyObject d, object value)
{
    var width = (double)value;
    return Math.Max(0, width); // Ensure non-negative
}
```

### Benefits

1. **Less Boilerplate**: No need to specify `OnChanged` or `Coerce` parameters
2. **Cleaner Code**: Attribute stays simple with just property options
3. **Compile-Time Safety**: Typos in method names are caught at compile time
4. **Flexible**: Override convention by specifying explicit parameter when needed

### When to Use Explicit Parameters

Use explicit `OnChanged` or `Coerce` parameters when:
- Method has a non-conventional name for clarity
- Multiple properties share the same callback
- Callback is inherited from base class

**Example:**
```csharp
[StyledProperty(OnChanged = nameof(HandleDimensionChanged))]
public double Width { get; set; }

[StyledProperty(OnChanged = nameof(HandleDimensionChanged))]
public double Height { get; set; }

private static void HandleDimensionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
{
    // Common handler for both Width and Height
}
```

## Troubleshooting

### Generated Code Not Appearing

1. Clean and rebuild your solution
2. Ensure class is marked as `partial`
3. Check that `OutWit.Common.MVVM.WPF.Generator` is referenced
4. Look for compilation errors in the Error List

### IntelliSense Not Working

1. Close and reopen the file
2. Restart Visual Studio
3. Delete `bin` and `obj` folders and rebuild

### Property Not Binding

1. Verify the property name matches the attribute usage
2. Check that the class inherits from `DependencyObject`
3. Ensure the generated property field exists (check in metadata or generated files)

## Cross-Platform Considerations

The same `StyledPropertyAttribute` can be used for both WPF and Avalonia:

- Reference `OutWit.Common.MVVM.WPF` for WPF projects
- Reference `OutWit.Common.MVVM.Avalonia` for Avalonia projects (when available)

Some options are platform-specific:
- `BindsTwoWayByDefault` - WPF only
- `Coerce` - WPF only

## Support and Questions

For issues or questions:
- GitHub: https://github.com/dmitrat/Common
- Issues: https://github.com/dmitrat/Common/issues
