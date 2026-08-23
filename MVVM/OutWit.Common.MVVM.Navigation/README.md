# OutWit.Common.MVVM.Navigation

ViewModel-first navigation for MVVM applications — the regions, navigation and dialog part of
Prism, without the container, the statics and the region adapters. This is the core package:
contracts and the platform-neutral implementation.

| Package | What it adds |
|---|---|
| **OutWit.Common.MVVM.Navigation** | contracts, navigation service, journal, guards, zones, dialogs |
| [OutWit.Common.MVVM.Navigation.Avalonia](../OutWit.Common.MVVM.Navigation.Avalonia/README.md) | view locator, `NavigationOutlet` control, window/overlay dialog hosts |
| [OutWit.Common.MVVM.Navigation.WPF](../OutWit.Common.MVVM.Navigation.WPF/README.md) | the same for WPF: locator + template selector, outlet control, `ViewPresenter`, modal-window host |
| [OutWit.Common.MVVM.Navigation.Modules](../OutWit.Common.MVVM.Navigation.Modules/README.md) | UI modules loaded from a folder or compiled in |
| [OutWit.Common.MVVM.Navigation.Avalonia.DialogHost](../OutWit.Common.MVVM.Navigation.Avalonia.DialogHost/README.md) | optional: dialogs through DialogHost.Avalonia, for Material.Avalonia applications |

Runnable sample, one set of view models bound from both frameworks:
[Sample.Core](../OutWit.Common.MVVM.Navigation.Sample.Core/README.md) ·
[Avalonia](../OutWit.Common.MVVM.Navigation.Avalonia.Sample/README.md) ·
[WPF](../OutWit.Common.MVVM.Navigation.WPF.Sample/README.md).

## Concepts

| Concept | What it is | Prism equivalent |
|---|---|---|
| **Outlet** | a named place showing one view model, with a journal | region + `RequestNavigate` |
| **Route** | key → view model type, creation mode, default outlet, metadata | `RegisterForNavigation` |
| **NavigationParameters** | immutable parameter set | `NavigationParameters` |
| **NavigationContext** | outlet, route, parameters and why (New/Back/Forward/Refresh) | `NavigationContext` |
| **Guard** | the right to refuse — on a view model, or a global service | `IConfirmNavigationRequest` |
| **Zone** | a named, ordered, observable collection of contributions | a region used as `region.Add()` |
| **ContributionItem** | a module's menu item / nav bar entry / toolbar button | a view added to a region |
| **Dialog** | a modal view model with a typed result | `IDialogService` |
| **Progress dialog** | a long operation behind a modal, with delay and minimum-duration rules | `RunLongProcess`-style helpers |

Three principles: view models, never views, drive navigation; there is no service locator and
no static entry point; everything is asynchronous and cancellable.

## Quick start

```csharp
services.AddNavigation(nav =>                                    // once — a second call replaces the first
{
    nav.AddOutlet("Inspector");                                  // Main is there by default
    nav.AddRoute<StudiesViewModel>(Routes.STUDIES);
    nav.AddRoute<StudyViewModel>(Routes.STUDY, NavigationRouteMode.Transient);
    nav.AddGuard<LicenseGuard>();                                // asked about every navigation
    nav.HistoryDepth = 20;
});
services.AddAvaloniaNavigation(o => o.UseOverlayDialogs());      // or AddWpfNavigation

var provider = services.BuildServiceProvider();
provider.ValidateNavigation(throwOnProblems: Debugger.IsAttached);

await provider.GetRequiredService<INavigationService>().NavigateAsync(Routes.STUDIES);
```

```csharp
public class StudyViewModel : ViewModelBase<ApplicationViewModel>, INavigationAware, INavigationGuard
{
    public StudyViewModel(ApplicationViewModel app, IStudyService studies) : base(app) { ... }

    public async Task OnNavigatedToAsync(NavigationContext context, CancellationToken cancellation)
    {
        // the screen is already visible; the token trips if the user moves on
        Study = await m_studies.LoadAsync(context.Parameters.Get<int>("id"), cancellation);
    }

    public Task OnNavigatedFromAsync(NavigationContext context, CancellationToken cancellation) => Task.CompletedTask;

    public Task<bool> CanNavigateToAsync(NavigationContext context, CancellationToken cancellation) => Task.FromResult(true);

    public async Task<bool> CanNavigateFromAsync(NavigationContext context, CancellationToken cancellation)
    {
        if (!IsDirty) return true;
        var answer = await m_dialogs.ShowAsync<DiscardChangesViewModel, bool>(cancellation: cancellation);
        return answer.IsConfirmed && answer.Value;
    }
}
```

View models are created with `ActivatorUtilities` — their dependencies come from DI, the view
models themselves are not registered. `Cached` routes keep one instance per outlet for the
life of the application; `Transient` routes get a fresh instance in its own DI scope for every
navigation, and the instance and the scope are disposed when the next one is shown.

## How a navigation runs

1. Route and outlet are resolved; unknown → `RouteNotFound` / `OutletNotFound`.
2. Already showing the route with equal parameters (Cached, new navigation) → `Unchanged`.
3. The outlet's slot is taken. A navigation still before its point of no return is displaced
   (`Cancelled`); one past it is waited for.
4. Global guards are asked `CanNavigateFromAsync`, then the current view model.
5. Global guards are asked `CanNavigateToAsync` — before the target view model exists.
6. The target view model is created (or taken from the cache) and asked `CanNavigateToAsync`.
7. **Point of no return.** From here the navigation commits.
8. Current view model: `OnNavigatedFromAsync`. Outlet content, route, parameters and journal
   change. The previous Transient view model is disposed. `Navigated` is raised.
9. **The slot is released.** Only then does the target's `OnNavigatedToAsync` run.

Step 9 is the one worth knowing about. A screen that loads for two seconds does not hold the
outlet for two seconds: it is already on screen, the navigation bar is live, and the next
navigation can start immediately. When it does, the abandoned screen's token is cancelled — so
`OnNavigatedToAsync` should pass it to whatever it awaits, and a navigation that is superseded
mid-load reports `Cancelled` while the newer one reports `Success`.

The whole pipeline runs on the UI thread; `NavigateAsync` may be called from anywhere.

### Navigating from inside a navigation

Redirecting from `OnNavigatedToAsync` — "this screen decided you belong elsewhere" — works,
including when awaited, because the slot is already free by then:

```csharp
public async Task OnNavigatedToAsync(NavigationContext context, CancellationToken cancellation)
{
    if (!m_session.IsSignedIn)
        await m_navigation.NavigateAsync(Routes.SIGN_IN);   // fine
}
```

A **guard** cannot do that for its own outlet: guards run while the outlet is held, so such a
call would wait for a slot its own caller owns. It is refused with `Failed` and a logged
explanation rather than deadlocking. Guards may navigate other outlets freely.

## Zones and contributions

```csharp
contributions.Add(new ContributionItem
{
    Zone = Zones.NAVIGATION_BAR,
    Key = "Summary",
    Order = 200,
    Header = Resources.Summary,
    Icon = "ChartBox",
    RouteKey = SummaryRoutes.GENERAL
});
```

```xml
<ItemsControl ItemsSource="{Binding NavigationBar.Items}" />
```

An item with a `RouteKey` gets a `Command` that navigates; `IsSelected` follows what the
outlet shows; `ParentKey` nests items into menus whichever order the modules arrive in.
Presentation state (`Header`, `IsEnabled`, `IsChecked`, …) belongs to the module and notifies.

## Dialogs

```csharp
public class RenameViewModel : NotifyPropertyChangedBase, IDialogAware<string>
{
    public event DialogCloseRequestedEventHandler<string>? CloseRequested;

    public Task OnOpenedAsync(NavigationParameters parameters, CancellationToken cancellation) { ... }
    public Task<bool> CanCloseAsync(DialogResult<string> result, CancellationToken cancellation) => Task.FromResult(true);

    private void Ok() => CloseRequested?.Invoke(DialogResult<string>.Confirmed(Name));
    private void Cancel() => CloseRequested?.Invoke(DialogResult<string>.Cancelled());
}

var result = await dialogs.ShowAsync<RenameViewModel, string>(new NavigationParameters(("name", current)));
if (result.IsConfirmed) Rename(result.Value);
```

Every close attempt — the view model's request, the window's close button, a click on the
overlay backdrop, `IDialogService.Close()` — goes through `CanCloseAsync`. Cancellation through
the token does not ask. Whether dialogs nest is the host's property: windows do, an overlay
layer does not.

The base class is not decoration: a dialog's view is built **before** `OnOpenedAsync` runs, so
whatever that method sets reaches the screen as a change notification. A `[Notify]` property on
a class that does not implement `INotifyPropertyChanged` binds once to its default and then
goes quiet, with no error anywhere.

## Long operations

A progress dialog is a dialog with timing rules, so it has its own contract rather than being
something every screen re-implements:

```csharp
var result = await progress.RunAsync(async (reporter, cancellation) =>
{
    for (var step = 1; step <= total; step++)
    {
        cancellation.ThrowIfCancellationRequested();
        await ImportAsync(step, cancellation);
        reporter.Report($"Importing {step} of {total}…", step / (double)total);
    }

    return total;
}, new ProgressOptions { Title = "Import" });

if (result.IsCompleted)      Show($"imported {result.Value}");
else if (result.IsCancelled) Show("cancelled");
else                         Show(result.Error!.Message);
```

The two durations are the point. An operation that finishes within `Delay` (400 ms by default)
never shows a dialog at all; one that does show it keeps it up for at least `MinimumDuration`
(600 ms), so a borderline operation does not flash. `RunAsync` never throws at the caller — a
failure comes back as `Error`.

Cancel, Escape and a click on the backdrop all mean the same thing: ask the operation to stop.
The dialog stays up until it actually has, so a screen never appears before its work has let
go of whatever it was holding. The work itself runs on the calling context — an operation that
would block the UI thread must do its own `Task.Run`, because no dialog can repaint a thread
that is busy.

The platform packages ship a plain view for it; register your own for `ProgressDialogViewModel`
and everything else stays the same.

## Start-up validation

`provider.ValidateNavigation()` runs after the modules have initialized and reports, as logged
warnings and a returned list:

- a route whose outlet nobody declared;
- a route with no view (asking the platform's `IViewFactory`, so the naming convention counts);
- a contribution pointing at an unregistered route or an undeclared outlet;
- a route view model carrying `[Notify]` without `INotifyPropertyChanged` — the silent one.

`throwOnProblems: true` under `Debugger.IsAttached` turns all of that into a start-up failure
instead of something the user finds by clicking.

## Testing

The core has no UI dependency. Register `DispatcherImmediate` (from `OutWit.Common.MVVM`) as
`IDispatcher` — `AddNavigation` does so when nothing else is registered — and fake
`IViewFactory` / `IDialogHost` for dialog tests. See `OutWit.Common.MVVM.Navigation.Tests`.

## License

Apache-2.0. Part of the [OutWit](https://github.com/dmitrat/Common) ecosystem.
