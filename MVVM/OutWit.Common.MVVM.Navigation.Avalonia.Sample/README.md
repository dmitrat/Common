# Navigation sample — Avalonia

The Avalonia half of the navigation sample: views, the window, and the composition root.
Everything else — routes, guards, screens, the module — comes from
[`OutWit.Common.MVVM.Navigation.Sample.Core`](../OutWit.Common.MVVM.Navigation.Sample.Core/README.md),
which is also where the walkthrough of what to try lives.

```bash
dotnet run --project MVVM/OutWit.Common.MVVM.Navigation.Avalonia.Sample
```

## The whole platform-specific part

```csharp
services.AddSample();                                          // shared
services.AddAvaloniaNavigation(o => o.UseOverlayDialogs());     // this line

var provider = services.BuildServiceProvider();

RegisterViews(provider.GetRequiredService<IViewRegistry>());    // and this list
provider.UseAvaloniaViewLocator();

await provider.GetRequiredService<UiModules>().InitializeAsync(provider);
provider.AddSampleContributions();
provider.ValidateNavigation(throwOnProblems: Debugger.IsAttached);

desktop.MainWindow = new ShellWindow { DataContext = provider.GetRequiredService<ApplicationViewModel>().Shell };
await provider.GetRequiredService<INavigationService>().NavigateAsync(Routes.STUDIES);
```

Compare with [the WPF sample's `App.xaml.cs`](../OutWit.Common.MVVM.Navigation.WPF.Sample/App.xaml.cs).

## The markup worth reading

[`Views/ShellWindow.axaml`](Views/ShellWindow.axaml) has all three bindings the package exists for:

```xml
<!-- a menu built from a zone; the module's item appears without this file changing -->
<Menu ItemsSource="{Binding MenuFile.Items}">
  <Menu.Styles>
    <Style Selector="MenuItem" x:DataType="nav:ContributionItem">
      <Setter Property="Header" Value="{Binding Header}" />
      <Setter Property="Command" Value="{Binding Command}" />
      <Setter Property="ItemsSource" Value="{Binding Children}" />
    </Style>
  </Menu.Styles>
</Menu>

<!-- the rail; Classes.selected follows the outlet -->
<ItemsControl ItemsSource="{Binding NavigationBar.Items}" />

<!-- the outlet: the object, not a name -->
<n:NavigationOutlet Outlet="{Binding Main}" />
```

Dialogs use `UseOverlayDialogs()` here — a dimmed layer inside the window, no external
dependency. The WPF sample uses modal windows. Neither choice reaches the view models.
