# OutWit.Common.Licensing.MVVM

The licence panel, minus the view.

Every property, command and enablement rule a licence screen needs, already
computed — over [`OutWit.Common.Licensing`](https://www.nuget.org/packages/OutWit.Common.Licensing).

**No UI framework.** Not Avalonia, not WPF, not Blazor, not MudBlazor. Bring
your own view and bind it.

## Install

```bash
dotnet add package OutWit.Common.Licensing.MVVM
```

## Why a ViewModel and not a control

Licensing a product costs one options block, a thin view, and the enforcement
points that are genuinely its own business logic. The gate is a handful of
lines; **the panel is a screen** — eleven status arms, four operating modes, an
expiry that escalates, a fingerprint to read to support, a request to export and
a licence to paste. Written per product, the third one is where arms start going
missing.

So the panel ships as a ViewModel. The view stays yours, because a desktop app,
a CAD add-in and an admin page do not agree on what a screen looks like — and
they do not need to.

## Use it

```csharp
var licensing = Licensing.Create(options => options
    .ForProduct("WitSweep", ThisAssembly.Version)
    .WithKeyRing(LicenseKeyRing.FromJson(EmbeddedRing()))
    .WithBinding(new LicenseBindingProviderMachine())
    .WithStore(new LicenseStoreFile(licenceDirectory))
    .WithDemo(TimeSpan.FromDays(30))
    .WithPeriodicReload(TimeSpan.FromHours(6)));

var gateway = new LicenseGatewayLocal(licensing);

Panel = new LicensePanelViewModel<ApplicationViewModel>(
    applicationVm,
    gateway,
    clipboard,      // optional seam
    files,          // optional seam
    dispatcher);    // REQUIRED wherever there is a UI thread — see below
```

Then bind. `Mode`, `ModeText`, `Severity`, `StatusDetail`, `ExpiryText`,
`Fingerprint`, `Grants`, `Warnings`, `Installed`, `PastedToken`, and commands
for refresh, install, uninstall, request, copy and file transfer.

Hold it; do not inherit from it. A service page derives from a component base
and a desktop view model from its own application root — a panel that insisted
on being either could serve only one of them.

## The gateway is the joint

```csharp
public interface ILicenseGateway
{
    event LicenseSnapshotEventHandler SnapshotChanged;
    LicenseSnapshot Current { get; }
    Task<LicenseSnapshot> RefreshAsync();
    Task<LicenseInstallOutcome> InstallAsync(string token);
    Task<bool> RemoveAsync(string licenseId);
    Task<LicenseRequest> CreateRequestAsync(string? contact = null, string? notes = null);
}
```

`LicenseGatewayLocal` wraps an in-process `ILicenseService`. A remote
implementation puts the same panel in a browser, one round trip from a licence
it must never hold: the panel never learns which it has.

## The seams

| Seam | Without it |
|---|---|
| `ILicenseClipboard` | copy commands are **visibly disabled**, never silently inert |
| `ILicenseFileTransfer` | open and save commands likewise |
| `IDispatcher` | see below — **not optional** for a UI |

The dispatcher is required wherever there is a UI thread, despite being optional
in the signature. `StateChanged` is awaited by nobody: the runtime raises it
from wherever its own re-evaluation finished, and every await inside the
licensing core suppresses the synchronization context by design so a desktop
host can block on its first evaluation without deadlocking. The snapshot
therefore arrives on a thread-pool thread — after an ordinary install, not only
when a periodic reload is on. Omit the dispatcher only where there is no UI
thread to marshal onto.

## What it deliberately does not do

**It does not gate.** Products refuse different things for different reasons,
and a generic refusal tells a customer nothing about which axis they fell off.
The enforcement point stays hand-written, where a reviewer can read it.

**It does not draw.** See above.

## Licence

Apache-2.0.
