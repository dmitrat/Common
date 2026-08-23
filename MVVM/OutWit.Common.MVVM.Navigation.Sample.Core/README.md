# Navigation sample — the shared half

The view models, services, routes, guards, contributions and the one UI module that both
sample applications run on. Nothing in this assembly references a UI framework.

- [`OutWit.Common.MVVM.Navigation.Avalonia.Sample`](../OutWit.Common.MVVM.Navigation.Avalonia.Sample/) — Avalonia views + composition root
- [`OutWit.Common.MVVM.Navigation.WPF.Sample`](../OutWit.Common.MVVM.Navigation.WPF.Sample/) — WPF views + composition root

Run either:

```bash
dotnet run --project MVVM/OutWit.Common.MVVM.Navigation.Avalonia.Sample
dotnet run --project MVVM/OutWit.Common.MVVM.Navigation.WPF.Sample
```

The two windows behave identically. That is the point: the difference between them is one DI
call and a list of view registrations.

## What the sample demonstrates

| Thing to try | What it shows |
|---|---|
| Click **Studies**, then immediately **Reports** while the list is still loading | The outlet is released the moment a screen is committed, so a slow `OnNavigatedToAsync` never blocks the next navigation. The abandoned load is cancelled — its token trips as soon as the outlet moves on. |
| Leave **Studies** and come back | `Cached`: the same view model *and the same view*, so the scroll position survives. `LoadCount` stays put — the screen checks `context.Kind` and reloads only on **Refresh**. |
| Open a study, then open another | `Transient`: a new view model per navigation, in its own DI scope, disposed when the next screen arrives. |
| Tick **unsaved changes**, then click another route | The screen's `INavigationGuard` awaits a dialog from inside `CanNavigateFromAsync`. Cancel → the navigation returns `Rejected` and nothing moved. |
| **Back** / **Forward** | The outlet's journal. Going back to a `Transient` route builds a fresh view model with the old parameters; going back to a `Cached` one shows the instance that was already there. |
| **Reports** in the rail, and **File → Open → Reports…** | Contributed by `ReportsModule`, compiled into the shared assembly. The shell's markup never names it: the module registered the route, the rail entry and the menu item. |
| **Audit** in the rail | The same, from a DLL the application does not reference. Its build output is staged into `@Modules/audit.module/` and `OutWit.Common.Plugins` finds it there at start-up; the screen prints the path it was loaded from. |
| **Settings → Toggle navigation lock**, then click anything | A global guard — one service, asked about every navigation in every outlet. This is what replaces Prism-era `LockNavigation`. |

## The shape of it

```
Routes.cs / Zones.cs          route keys and zone names — plain strings the app owns
Models/Study.cs               a ModelBase record of a recording
Services/StudyStore.cs        the data, with a deliberate delay so the async behaviour is visible
Guards/BusyGuard.cs           a global INavigationGuard
ViewModels/
  ApplicationViewModel.cs     the root view model; holds the services, not the screens
  ShellViewModel.cs           the window: the outlet object, the zones, Back/Forward/Refresh
  StudiesViewModel.cs         Cached  + INavigationAware
  StudyViewModel.cs           Transient + INavigationAware + INavigationGuard
  SettingsViewModel.cs        Cached, toggles the global guard
  ReportsViewModel.cs         belongs to the module
  ConfirmDialogViewModel.cs   IDialogAware<bool>
Modules/ReportsModule.cs      a UiModuleBase, compiled in
SampleComposition.cs          AddSample() / AddSampleContributions()
```

The second module is a separate assembly per platform —
`OutWit.Common.MVVM.Navigation.Sample.Module.Avalonia` and `...Module.WPF`. Each sample
references its own with `ReferenceOutputAssembly="false"`, so the application is built without
being able to see the module's types, and a build target stages the output into
`@Modules/audit.module/`:

```xml
<ProjectReference Include="..\...Sample.Module.Avalonia\...csproj"
                  ReferenceOutputAssembly="false" Private="false" />

<Target Name="StageSampleModules" AfterTargets="Build">
  <ItemGroup>
    <SampleModuleFiles Include="..\...Sample.Module.Avalonia\bin\$(Configuration)\$(TargetFramework)\**\*" />
  </ItemGroup>
  <Copy SourceFiles="@(SampleModuleFiles)" DestinationFolder="$(OutDir)@Modules\audit.module\%(RecursiveDir)" />
</Target>
```

That folder ends up carrying its own copies of the shared assemblies, which is the interesting
part: UI modules load into the **default** assembly context, so the loader reuses what the host
already has rather than loading a second `Avalonia.dll` and giving the module a view type the
host cannot use. Two consequences worth knowing — a module can hand the host real controls and
real view models, and a module cannot be unloaded at run time.

A module that ships screens is platform-specific, which is why there are two of them; the
module class, the route and the contribution are identical, and only the view differs.

`ApplicationViewModel` follows the house style — every view model derives from
`ViewModelBase<ApplicationViewModel>` — with one change navigation forces: it no longer
constructs the screens. The navigation service does that, from DI, so what the root holds is
the shared services the screens reach for.

## Two things the sample had to get right

**Views live with their platform, view models do not.** The naming convention
(`*.ViewModels.FooViewModel` → `*.Views.FooView`) searches the view model's *own assembly*.
Here the view models are shared and the views are not, so each application registers its pairs
explicitly through `IViewRegistry` — which is also the only path that survives trimming:

```csharp
views.Register<StudiesViewModel, StudiesView>();
```

**A dialog view model must implement `INotifyPropertyChanged`.** Its view is built before
`OnOpenedAsync` runs, so the title and message arrive as change notifications. A `[Notify]`
property on a class that does not implement the interface binds once to its default and then
goes quiet, with no error anywhere — `ConfirmDialogViewModel` derives from
`NotifyPropertyChangedBase` for exactly that reason. `ValidateNavigation()` reports this for
route view models at start-up.
