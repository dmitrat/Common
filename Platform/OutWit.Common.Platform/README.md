# OutWit.Common.Platform

Cross-platform helpers for collecting machine system information (OS / CPU /
GPU / memory / storage / health), a stable hashed machine identity, and
semantic standard directories. Internally organised as a small **Strategy**
split per OS so that new platforms can be added by writing one class —
Windows, Linux, macOS and Android are bundled out of the box.

## Why

A distributed-compute platform needs every participating node to advertise
its capabilities (what OS, how much RAM, which GPU, how much temp space) and
its current health (CPU load, idle/active state) in a uniform shape that the
scheduler can match against activity requirements. This package is the
substrate that produces those shapes — without itself making any scheduling
decisions.

## Install

```bash
dotnet add package OutWit.Common.Platform
```

## Public surface

```csharp
PlatformKind           // enum: Windows / Linux / MacOS / Android / Unknown
PlatformDetector       // static GetCurrentPlatform()

ISystemProfileProvider // → SystemProfile { Os, Cpu, Memory, Gpus[], TempStorage }
ISystemHealthProvider  // → SystemHealthSnapshot { CpuLoadPercent, AvailableRamMb, GpuLoadPercent, IsUserActive, TimestampUtc }
IMachineIdentityProvider // → stable hashed machine identity (SHA-256 hex)
IStandardDirectoryProvider // → User data / Shared data / Cache / Logs / Config / Temp directories

SystemProfileProvider     : ISystemProfileProvider
SystemHealthProvider      : ISystemHealthProvider, IDisposable
MachineIdentityProvider   : IMachineIdentityProvider
StandardDirectoryProvider : IStandardDirectoryProvider
```

The default constructor on every provider auto-selects the per-OS probe for
the current host:

```csharp
var profile = await new SystemProfileProvider().CollectAsync();
var health  = await new SystemHealthProvider().CollectAsync();
var id      = await new MachineIdentityProvider(
                  new StandardDirectoryProvider("Acme", "MyApp")
              ).GetMachineIdentityAsync();
```

## Architecture

The library follows the **Strategy** pattern: the public providers
(`SystemProfileProvider`, `SystemHealthProvider`, `MachineIdentityProvider`)
are thin orchestrators that delegate all OS-specific reading to an internal
`IPlatformProbe` instance. One probe per OS, each in its own file.

```
SystemProfileProvider   ─┐
SystemHealthProvider    ─┼─→ IPlatformProbe ─→ PlatformProbeWindows
MachineIdentityProvider ─┘                  ─→ PlatformProbeLinux
                                            ─→ PlatformProbeMacOS
                                            ─→ PlatformProbeAndroid (: PlatformProbeLinux)
                                            ─→ PlatformProbeNull    (Unknown fallback)
```

Benefits:

- **Per-OS code lives in one file.** "How does macOS detect storage type?"
  is answered by opening `PlatformProbeMacOS.cs`, not by greping for
  `OperatingSystem.IsMacOS()` across three providers.
- **Adding a new platform** = one new `PlatformProbe*` class implementing
  `IPlatformProbe`, plus a switch arm in `PlatformProbeFactory`. No changes
  to provider orchestrators.
- **Testable in isolation.** Tests inject a fake probe through the providers'
  `internal` test-only constructors, exercise per-OS rules on any host, and
  verify disposal ownership.
- **The wire shape (`SystemProfile`, `SystemHealthSnapshot`) is stable.**
  Probes can change without breaking serialization.

## Continuous integration

The Platform suite is the only one in the OutWit.Common monorepo whose
behaviour varies per host OS, so its CI also varies: in addition to the
default `windows-latest` test run shared with every other package, the
test suite is also executed on `ubuntu-latest` and `macos-latest`
runners on every change under `Platform/**`. The Linux and macOS code
paths in the bundled probes are therefore validated end-to-end against
real OS facilities (`/proc`, `sysctl`, `vm_stat`, `diskutil`, …) on
every commit, not just unit-tested via fakes. See
[`.github/workflows/ci.yml`](../../.github/workflows/ci.yml)
(`run-platform-multi-os` job).

## Coverage matrix

What each bundled probe currently reports. **Empty / `Unknown` is a valid
answer** — providers never throw on missing data.

| Capability | Windows | Linux | macOS | Android | Unknown |
|---|---|---|---|---|---|
| CPU model name | Registry: `ProcessorNameString` | `/proc/cpuinfo` `model name` | `sysctl machdep.cpu.brand_string` | `/proc/cpuinfo` `Hardware` → `build.prop ro.board.platform` | empty |
| GPU list | WMI `Win32_VideoController` + driver registry | `lspci -mm` (no VRAM) | `system_profiler SPDisplaysDataType` | `build.prop` GLES/Vulkan hint (best-effort) | empty |
| Storage type | WMI `MSFT_PhysicalDisk` | `/sys/block/{dev}/queue/rotational` + nvme prefix | `diskutil info` text | `SSD` (mobile = solid-state by definition) | `Unknown` |
| CPU load % | `PerformanceCounter("Processor", "% Processor Time")` | `/proc/stat` delta | `top -l 1 -s 0` "idle" % | `/proc/stat` (inherited from Linux) | 0 |
| Available RAM MB | `PerformanceCounter("Memory", "Available MBytes")` | `/proc/meminfo` `MemAvailable` | `vm_stat` (free + inactive) | `/proc/meminfo` (inherited from Linux) | 0 |
| User active | `GetLastInputInfo` P/Invoke (5-min idle threshold) | `true` (no headless API) | `true` (no headless API) | `true` (needs Android SDK PowerManager) | `true` |
| Machine identity | Registry `MachineGuid` | `/etc/machine-id` / `/var/lib/dbus/machine-id` | `sysctl kern.uuid` | `build.prop ro.serialno` → Linux machine-id fallback | `null` (per-app GUID file) |

The Android probe stays **pure .NET** — it works on `net10.0` without an
`android` TFM. Bridging real Android APIs (`Build`, `PowerManager`,
`Settings.Secure.ANDROID_ID`, `EGL/Vulkan` for the GPU) is an app-level
concern; a consuming Android client can subclass `PlatformProbeAndroid` and
override the relevant methods.

> Note: `PlatformProbeAndroid` is `internal`, like all the other probes —
> if you want app-level overrides, file an issue and we can promote the
> shape to `public` along with a clean DI registration entry point.

## Adding a new platform

Today the supported set is Windows / Linux / macOS / Android / Unknown.
Adding (say) iOS or BSD looks like this:

1. Add the value to `PlatformKind` (e.g. `Ios`).
2. Add the corresponding branch to `OsPlatform` + `PlatformDetector`.
3. Write `Internal/PlatformProbeIos.cs : IPlatformProbe`. Return safe
   defaults for what the OS doesn't expose; implement the rest.
4. Add a `PlatformKind.Ios => new PlatformProbeIos()` arm to
   `PlatformProbeFactory.ForPlatform`.
5. Write `Internal/PlatformProbeIosTests.cs` exercising the new methods.

The public API (`ISystemProfileProvider`, `SystemProfile`, …) does **not**
change.

## License

Licensed under the Apache License, Version 2.0. See `LICENSE`.

## Attribution (optional)

If you use OutWit.Common.Platform in a product, a mention is appreciated
(but not required): "Powered by OutWit.Common.Platform (https://ratner.io/)".

## Trademark / Project name

"OutWit" and the OutWit logo are used to identify the official project by
Dmitry Ratner. You may refer to the project name in a factual way (e.g.,
"built with OutWit.Common.Platform") or to indicate compatibility. You may
not use the name as the name of a fork or derived product in a way that
implies it is the official project, nor use the OutWit logo to promote
forks or derived products without permission.
