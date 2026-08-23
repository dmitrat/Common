# OutWit.Common.MVVM.Navigation.Avalonia

The Avalonia half of [OutWit.Common.MVVM.Navigation](../OutWit.Common.MVVM.Navigation/README.md).
Runnable sample: [OutWit.Common.MVVM.Navigation.Avalonia.Sample](../OutWit.Common.MVVM.Navigation.Avalonia.Sample/README.md).


- **ViewLocator** — an `IDataTemplate` and the core's `IViewFactory`: the view registry first
  (the only path under trimming/AOT), then a naming convention searched in the view model's
  own assembly, so module assemblies are found. Created from DI so views may take dependencies.
- **NavigationOutlet** — a control that hosts an `INavigationOutlet` and *owns the views*:
  for Cached routes the view of each view model survives navigations (scroll position, column
  widths, expensive controls), for Transient routes it goes with the view model.
- **DialogHostWindow** / **DialogHostOverlay** — `IDialogHost` implementations without external
  dependencies: modal windows (nest) or a dimmed overlay on the active window (one at a time).
- **IApplicationResources** — lets a UI module add its styles and resource dictionaries.
- **AddAvaloniaNavigation()** — registers all of the above plus `AvaloniaDispatcher` as `IDispatcher`.

## Setup

```csharp
// App.axaml.cs, OnFrameworkInitializationCompleted
var services = new ServiceCollection();

services.AddNavigation(nav =>
{
    nav.AddRoute<StudiesViewModel>(Routes.STUDIES);
    nav.AddRoute<StudyViewModel>(Routes.STUDY, NavigationRouteMode.Transient);
});
services.AddAvaloniaNavigation(o => o.UseOverlayDialogs());     // or UseWindowDialogs() (default)
services.AddSingleton<ShellViewModel>();

var provider = services.BuildServiceProvider();
provider.UseAvaloniaViewLocator();                               // Application.DataTemplates
provider.ValidateNavigation(throwOnProblems: Debugger.IsAttached);

desktop.MainWindow = new ShellWindow { DataContext = provider.GetRequiredService<ShellViewModel>() };
await provider.GetRequiredService<INavigationService>().NavigateAsync(Routes.STUDIES);
```

```xml
<!-- ShellWindow.axaml -->
<Window xmlns:n="clr-namespace:OutWit.Common.MVVM.Navigation.Avalonia.Controls;assembly=OutWit.Common.MVVM.Navigation.Avalonia">
  <Grid ColumnDefinitions="Auto,*">
    <ItemsControl ItemsSource="{Binding NavigationBar.Items}" />     <!-- a zone -->
    <n:NavigationOutlet Grid.Column="1" Outlet="{Binding Main}" />   <!-- the outlet object, not a name -->
  </Grid>
</Window>
```

`ShellViewModel` exposes `Main` (`navigation.Outlet()`) and `NavigationBar`
(`contributions.Zone(Zones.NAVIGATION_BAR)`). No statics, no attached property with a region
name, no service lookup from a control.

## Views

| Convention (`ViewConvention`) | `App.ViewModels.StudyViewModel` → |
|---|---|
| `ViewModelsToViews` (default) | `App.Views.StudyView`, then `App.ViewModels.StudyView` |
| `SameNamespace` | `App.ViewModels.StudyView` |
| `None` | registry only |

Explicit registration always wins: `nav.AddView<StudyViewModel, StudyView>()` or
`context.Views.Register<StudyViewModel, StudyView>()` from a module.

The convention searches the **view model's own assembly**. Views that live in a different
assembly from their view models — a shared view model layer, a per-platform view layer — have
to be registered explicitly. That is also the only path that survives trimming.

## Dialogs

Both hosts route every UI-initiated close — the window's close button, a click on the
overlay backdrop, Escape — through the dialog's `CanCloseAsync`. Style the generated window
with the `navigation-dialog` class and the overlay with `navigation-dialog-overlay`.
For Material.Avalonia / DialogHost.Avalonia, implement `IDialogHost` and pass it to
`UseDialogHost<T>()`.

## License

Apache-2.0. Part of the [OutWit](https://github.com/dmitrat/Common) ecosystem.
