# OutWit.Common.MVVM.WPF.Sample

Sample application demonstrating the features of **OutWit.Common.MVVM.WPF** library.

## Features Demonstrated

### 1. Source Generator for DependencyProperty

**`Controls/DemoControl.cs`** shows how to use `[StyledProperty]` attribute:

```csharp
using OutWit.Common.MVVM.Attributes;

public partial class DemoControl : UserControl
{
    [StyledProperty(DefaultValue = "Untitled")]
    public string Title { get; set; }

    [StyledProperty(DefaultValue = 0, AffectsRender = true)]
    public int Counter { get; set; }

    [StyledProperty(DefaultValue = false, BindsTwoWayByDefault = true)]
    public bool IsHighlighted { get; set; }

    // Convention-based callback - automatically discovered!
    private static void OnIsHighlightedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // Handle change
    }
}
```

### 2. DelegateCommand with CommandManager

**`MainViewModel.cs`** demonstrates WPF-specific command with automatic CanExecute updates:

```csharp
using OutWit.Common.MVVM.WPF.Commands;

// WPF DelegateCommand uses CommandManager.RequerySuggested
IncrementCommand = new DelegateCommand(_ => Counter++);
DecrementCommand = new DelegateCommand(_ => Counter--, _ => Counter > 0);

// Trigger CanExecute re-evaluation
CommandManager.InvalidateRequerySuggested();
```

### 3. BindingProxy for DataGrid

**`MainWindow.xaml`** shows how to access ViewModel from DataGrid rows:

```xml
<Window.Resources>
    <utils:BindingProxy x:Key="ViewModelProxy" Data="{Binding}" />
</Window.Resources>

<DataGrid ItemsSource="{Binding Items}">
    <DataGridTemplateColumn>
        <DataTemplate>
            <!-- Access ViewModel command from inside DataGrid row -->
            <Button Command="{Binding Data.DeleteCommand, Source={StaticResource ViewModelProxy}}"
                    CommandParameter="{Binding}" />
        </DataTemplate>
    </DataGridTemplateColumn>
</DataGrid>
```

## Running the Sample

```bash
cd MVVM/OutWit.Common.MVVM.WPF.Sample
dotnet run
```

## Key Points

1. **Mark classes as `partial`** - Required for source generator
2. **Use `OutWit.Common.MVVM.Attributes` namespace** for attributes
3. **Convention callbacks** - `On{PropertyName}Changed` methods are auto-discovered
4. **No manual DependencyProperty code** - Everything is generated

## License

Non-Commercial License (NCL) - See LICENSE file.
