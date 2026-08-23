# OutWit.Common.MVVM.Navigation.Modules

UI modules for [OutWit.Common.MVVM.Navigation](../OutWit.Common.MVVM.Navigation/README.md):
plugins that register services, routes, views and contributions. Built on
[OutWit.Common.Plugins](../../Plugins/OutWit.Common.Plugins/README.md), with the same two-phase
shape as every other OutWit plugin axis — services before the container is built, everything
else after.

A working module, compiled into the application rather than loaded from a folder, is in the
[navigation sample](../OutWit.Common.MVVM.Navigation.Sample.Core/README.md): it adds a route,
a navigation bar entry and a nested menu item without the shell knowing it exists.

## A module

```csharp
[WitPluginManifest("Summary")]
public class SummaryModule : UiModuleBase
{
    public override void Initialize(IServiceCollection services)
    {
        services.AddSingleton<ISummaryService, SummaryService>();      // phase one
    }

    protected override void OnInitialized(UiModuleContext context)     // phase two
    {
        context.Routes.Register<SummaryViewModel>(SummaryRoutes.GENERAL, metadata: new FeatureGate("Holter.Summary"));
        context.Views.Register<SummaryViewModel, SummaryView>();       // or rely on the naming convention

        context.Contributions.Add(new ContributionItem
        {
            Zone = Zones.NAVIGATION_BAR,
            Key = "Summary",
            Order = 200,
            Header = Resources.Summary,
            Icon = "ChartBox",
            RouteKey = SummaryRoutes.GENERAL
        });
    }
}
```

View models are not registered: navigation creates them with `ActivatorUtilities`, their
dependencies come from DI. Styles and resources go through the platform's
`IApplicationResources` (`context.Services.GetRequiredService<IApplicationResources>()`).

## The host

```csharp
services.AddNavigation(nav => ...);
services.AddAvaloniaNavigation();
services.AddUiModules();                                           // @Modules next to the app
// services.AddUiModules(o => o.Folder = "Plugins/UI");
// services.AddUiModules(o => { o.ScanFolder = false; o.AddModule<SummaryModule>(); });   // compiled in

var provider = services.BuildServiceProvider();
await provider.GetRequiredService<UiModules>().InitializeAsync(provider);
provider.ValidateNavigation();
```

Folder modules follow the plugin conventions: one sub-folder per module (`@Modules/summary.module/`)
with the module DLL, its `deps.json` and its dependencies. UI modules are loaded without isolated
load contexts — they share the UI framework and the navigation contracts with the host — so
they cannot be unloaded at run time.

A module that throws in either phase is recorded in `UiModules.Failures` and logged; the other
modules carry on. A folder that cannot be scanned at all (a bad manifest, a missing or circular
dependency) throws an `AggregateException` from `AddUiModules` — that is a deployment error and
fails fast.

## License

Apache-2.0. Part of the [OutWit](https://github.com/dmitrat/Common) ecosystem.
