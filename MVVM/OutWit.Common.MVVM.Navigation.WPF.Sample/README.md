# Navigation sample — WPF

The WPF half of the navigation sample: views, the window, and the composition root.
Everything else — routes, guards, screens, the module — comes from
[`OutWit.Common.MVVM.Navigation.Sample.Core`](../OutWit.Common.MVVM.Navigation.Sample.Core/README.md),
which is also where the walkthrough of what to try lives.

```bash
dotnet run --project MVVM/OutWit.Common.MVVM.Navigation.WPF.Sample
```

This is the sample to read if the question is "can we drop Prism and Unity without leaving
WPF": nothing here is Avalonia-shaped, and the view models are the same assembly the Avalonia
sample binds.

## The whole platform-specific part

```csharp
services.AddSample();                                        // shared
services.AddWpfNavigation();                                  // this line

var provider = services.BuildServiceProvider();

RegisterViews(provider.GetRequiredService<IViewRegistry>());  // and this list
provider.UseWpfViewLocator(this);

await provider.GetRequiredService<UiModules>().InitializeAsync(provider);
provider.AddSampleContributions();
provider.ValidateNavigation(throwOnProblems: Debugger.IsAttached);

MainWindow = new ShellWindow { DataContext = provider.GetRequiredService<ApplicationViewModel>().Shell };
MainWindow.Show();

await provider.GetRequiredService<INavigationService>().NavigateAsync(Routes.STUDIES);
```

Compare with [the Avalonia sample's `App.axaml.cs`](../OutWit.Common.MVVM.Navigation.Avalonia.Sample/App.axaml.cs).

## The markup worth reading

[`Views/ShellWindow.xaml`](Views/ShellWindow.xaml):

```xml
<!-- a menu built from a zone; ItemContainerStyle carries the bindings -->
<Menu ItemsSource="{Binding MenuFile.Items}">
  <Menu.ItemContainerStyle>
    <Style TargetType="MenuItem">
      <Setter Property="Header" Value="{Binding Header}" />
      <Setter Property="Command" Value="{Binding Command}" />
      <Setter Property="ItemsSource" Value="{Binding Children}" />
    </Style>
  </Menu.ItemContainerStyle>
</Menu>

<!-- the rail; a DataTrigger on IsSelected does the highlight -->
<ItemsControl ItemsSource="{Binding NavigationBar.Items}" />

<!-- the outlet: the object, not a name -->
<n:NavigationOutlet Outlet="{Binding Main}" />
```

Dialogs are modal windows. A non-Window view is wrapped in one, and
[`App.xaml`](App.xaml) styles that wrapper through the resource key
`OutWit.Navigation.DialogWindow`. For nested content elsewhere use `<n:ViewPresenter
ViewModel="{Binding Widget}" />`, or point a `ContentControl` at the locator:
`ContentTemplateSelector="{StaticResource OutWit.Navigation.ViewLocator}"`.
