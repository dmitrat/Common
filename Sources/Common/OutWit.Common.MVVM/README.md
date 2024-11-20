## Simplifying DependencyProperty Management with the `Bindable` Aspect

When working with WPF, dealing with `DependencyProperty` can be tedious and error-prone. Typically, you need to:

1. Declare the static `DependencyProperty`:
   ```csharp
   public static readonly DependencyProperty ValueProperty =
       DependencyProperty.Register("Value", typeof(int), typeof(MyControl),
           new PropertyMetadata(0, new PropertyChangedCallback(OnValueChanged)));
   ```
2. Create a property for accessing the value:
   ```csharp
   public int Value
   {
      get => (int)GetValue(ValueProperty);
      set => SetValue(ValueProperty, value);
   }
   ```
This boilerplate code is verbose and introduces multiple opportunities for errors.

### Using `BindingUtils` for Cleaner Registration
The `BindingUtil`s utility makes `DependencyProperty` registration more concise:
```csharp
public static readonly DependencyProperty ValueProperty =
    BindingUtils.Register<MyControl, int>(nameof(Value), OnValueChanged); 
```
 
This utility reduces boilerplate and improves readability.

### Eliminating `GetValue` and `SetValue` with the `Bindable` Aspect
The `Bindable` aspect simplifies property definitions further by eliminating the need to explicitly call `GetValue` and `SetValue`. For example:

```csharp
[Bindable]
public int Value { get; set; }
```
This reduces the code significantly, making classes cleaner and easier to maintain.

### Customizing `DependencyProperty` Names
By default, the aspect assumes the DependencyProperty for a property named `[Name]` is declared as `[Name]Property`. For instance, a property named `Value` is expected to use `ValueProperty`.

If the `DependencyProperty` uses a custom name, you can specify it explicitly:
```csharp
[Bindable("CustomDependencyProperty")]
public int Value { get; set; }
```
### Key Benefits
- Reduces repetitive code.
- Ensures consistency and readability.
- Simplifies working with DependencyProperty in WPF applications.

For more details, check out [the article](https://ratner.io/2024/11/20/streamlining-net-development-with-practical-aspects/).
