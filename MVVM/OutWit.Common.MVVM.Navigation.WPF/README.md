# OutWit.Common.MVVM.Navigation.WPF

The WPF half of [OutWit.Common.MVVM.Navigation](../OutWit.Common.MVVM.Navigation/README.md).
It lets a WPF application drop Prism and Unity while staying on WPF — the infrastructure
changes, the UI framework does not.

Runnable sample: [OutWit.Common.MVVM.Navigation.WPF.Sample](../OutWit.Common.MVVM.Navigation.WPF.Sample/README.md),
which binds the same view models the Avalonia sample does.

- **ViewLocator** — the core's `IViewFactory` and a `DataTemplateSelector`: the view registry
  first (the only path under trimming/AOT), then a naming convention searched in the view
  model's own assembly. Created from DI so views may take dependencies. Its templates hold a
  `ViewPresenter`, so DI-built views work inside any `ContentControl` or `ItemsControl`.
- **NavigationOutlet** — a control that hosts an `INavigationOutlet` and *owns the views*:
  for Cached routes the view of each view model survives navigations (scroll position, column
  widths, expensive controls), for Transient routes it goes with the view model.
- **ViewPresenter** — shows the view of any view model: nested content, zone widgets.
- **DialogHostWindow** — `IDialogHost` over modal windows; dialogs nest; the close button goes
  through the dialog's `CanCloseAsync`.
- **IApplicationResources** — lets a UI module merge its resource dictionaries.
- **AddWpfNavigation()** — registers all of the above plus `WpfDispatcher` as `IDispatcher`.

## Setup

```csharp
// App.xaml.cs
protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);

    var services = new ServiceCollection();
    services.AddNavigation(nav =>
    {
        nav.AddRoute<StudiesViewModel>(Routes.STUDIES);
        nav.AddRoute<StudyViewModel>(Routes.STUDY, NavigationRouteMode.Transient);
    });
    services.AddWpfNavigation();                          // or AddWpfNavigation(o => o.UseDialogHost<MyMaterialHost>())
    services.AddSingleton<ShellViewModel>();

    var provider = services.BuildServiceProvider();
    provider.UseWpfViewLocator();                         // Application.Resources["OutWit.Navigation.ViewLocator"]
    provider.ValidateNavigation(throwOnProblems: Debugger.IsAttached);

    MainWindow = new ShellWindow { DataContext = provider.GetRequiredService<ShellViewModel>() };
    MainWindow.Show();

    _ = provider.GetRequiredService<INavigationService>().NavigateAsync(Routes.STUDIES);
}
```

```xml
<!-- ShellWindow.xaml -->
<Window xmlns:n="https://schemas.outwit.io/navigation">
  <Grid>
    <Grid.ColumnDefinitions>
      <ColumnDefinition Width="Auto" /><ColumnDefinition Width="*" />
    </Grid.ColumnDefinitions>
    <ItemsControl ItemsSource="{Binding NavigationBar.Items}" />          <!-- a zone -->
    <n:NavigationOutlet Grid.Column="1" Outlet="{Binding Main}" />        <!-- the outlet object, not a name -->
  </Grid>
</Window>
```

Nested content anywhere: `<n:ViewPresenter ViewModel="{Binding Widget}" />` or
`<ContentControl Content="{Binding Widget}" ContentTemplateSelector="{StaticResource OutWit.Navigation.ViewLocator}" />`.

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

`DialogHostWindow` wraps a non-Window view in a modal window centred on its owner; define a
`Style` with key `OutWit.Navigation.DialogWindow` in the application resources to restyle it.
A view that is a `Window` is shown as is. For MaterialDesignThemes' DialogHost, implement
`IDialogHost` and pass it to `UseDialogHost<T>()`.

## License

Apache-2.0. Part of the [OutWit](https://github.com/dmitrat/Common) ecosystem.
