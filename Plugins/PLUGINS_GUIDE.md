# OutWit Plugin Architecture — Author & Host Guide

> A practical, end-to-end guide for writing plugins for any OutWit host (and for building hosts that load them). The companion to the API-level [`OutWit.Common.Plugins` README](OutWit.Common.Plugins/README.md), which describes the loader's surface; this document describes the **whole pattern** — packaging, deployment, host integration, conventions, and known gotchas.

## Table of Contents

1. [Who is this guide for](#1-who-is-this-guide-for)
2. [The plugin story in one page](#2-the-plugin-story-in-one-page)
3. [Concepts](#3-concepts)
4. [Writing a plugin — step by step](#4-writing-a-plugin--step-by-step)
5. [Writing a host — step by step](#5-writing-a-host--step-by-step)
6. [Per-plugin configuration](#6-per-plugin-configuration)
7. [Distribution: NuGet packaging](#7-distribution-nuget-packaging)
8. [Consumer-side reference handling](#8-consumer-side-reference-handling)
9. [Testing plugins](#9-testing-plugins)
10. [Quick reference](#10-quick-reference)
11. [Reference implementations](#11-reference-implementations)
12. [FAQ](#12-faq)

---

## 1. Who is this guide for

Two audiences, two workflows:

| Audience | What you need from this guide |
|---|---|
| **Plugin authors** — community devs extending an OutWit host (WitIdentity, WitEngine, a third-party Norav.Records consumer, …) | §3 concepts → §4 writing a plugin → §6 config → §7 distribution |
| **Host authors** — building a service that delegates work to plugins | §3 concepts → §5 writing a host → §10 quick reference |

If you only want to call the loader API, see [`OutWit.Common.Plugins/README.md`](OutWit.Common.Plugins/README.md). This guide goes further: how to ship a plugin as a NuGet package, how the consumer's build process picks it up, what file layout the loader expects, how shared dependencies behave, etc.

---

## 2. The plugin story in one page

**The deal**: an OutWit host (e.g. `WitIdentity`) defines a contract — "send an email", "query a log backend". A plugin author publishes a NuGet package that implements that contract for one vendor (Resend, SMTP, NewRelic, Loki, …). The host operator runs:

```bash
dotnet add package OutWit.Shared.Email.Provider.Resend
```

…and on the next build, the plugin's DLLs land in the host's output under `@Email/resend.module/`. At runtime the host's plugin loader scans that folder, finds the plugin's manifest, registers its services in DI, and the host can now send email via Resend without any code change.

**The mechanism**, in five layers:

```
┌──────────────────────────────────────────────────────────┐
│ Plugin author                                             │
│   • writes class : WitPluginBase, IEmailProviderPlugin    │
│   • marks it with [WitPluginManifest("Resend", ...)]      │
│   • csproj: NuspecFile + CopyPluginToOutputDirectory tgt  │ ← stages locally
│   • nuspec: build/.targets + <category>/<module>/**          │ ← shapes the package
│   • dotnet pack → publishes Resend.module.nupkg           │
└──────────────────────────────────────────────────────────┘
                          ▼
┌──────────────────────────────────────────────────────────┐
│ NuGet package                                             │
│   build/Resend.targets    ← auto-imported on Add Package  │
│   buildTransitive/...     ← same, for transitive ref      │
│   plugins/resend.module/  ← the plugin DLLs + deps.json   │
│   lib/net10.0/Resend.dll  ← formal "library for TFM"      │
└──────────────────────────────────────────────────────────┘
                          ▼
┌──────────────────────────────────────────────────────────┐
│ Host (e.g. WitIdentity)                                   │
│   • <PackageReference Include="...Plugin.Resend" />        │
│   • build/Resend.targets runs AfterTargets="Build"         │ ← copies into output
│   • output: bin/.../@Email/resend.module/<DLLs>         │
│   • Startup: services.AddEmailPlugins("@Email");         │ ← scans folder
│   • Plugin loader picks up the manifest, loads the DLL,    │
│     calls Initialize(services) then OnInitialized(sp).     │
└──────────────────────────────────────────────────────────┘
```

That's the entire story. The rest of this document fills in the details.

---

## 3. Concepts

### 3.1 Plugin, module, host

| Term | Meaning |
|---|---|
| **Host** | The application that hosts plugins. References `OutWit.Common.Plugins` (the loader). Defines a plugin contract (typically a marker interface like `IEmailProviderPlugin : IWitPlugin`). |
| **Plugin** | A .NET class implementing `IWitPlugin` (usually via `WitPluginBase`) decorated with `[WitPluginManifest]`. One plugin per assembly is typical. |
| **Module** | The deployed *folder* containing a plugin's DLLs plus its transitive dependencies — `resend.module/`, `loki.module/`, etc. The host loader scans a parent directory (`@Email/`) and treats each subfolder as one module. |
| **Manifest** | The `[WitPluginManifest]` attribute on the plugin class, plus optional `[WitPluginDependency]` attributes. Read at discovery time by the loader. |

### 3.2 Two-phase initialization

Every plugin participates in two passes over its lifetime:

```csharp
public abstract class WitPluginBase : IWitPlugin
{
    // Pass 1: register services. Called before the host builds its IServiceProvider.
    public virtual void Initialize(IServiceCollection services) { }

    // Pass 2: do work. Called after the host has built the IServiceProvider — you
    // can resolve services here (including services registered by OTHER plugins).
    public virtual void OnInitialized(IServiceProvider serviceProvider) { }

    // Cleanup: called on Dispose/Unload (only if isolated contexts are enabled).
    public virtual void OnUnloading() { }
}
```

**Why two phases**: plugin A may want to wire into a registry owned by plugin B. If both registered everything in one pass, A would call B before B exists. The host's flow is therefore:

```
foreach plugin: plugin.Initialize(services)   // all plugins register services
serviceProvider = services.BuildServiceProvider()
foreach plugin: plugin.OnInitialized(sp)      // plugins discover each other
```

`Initialize` is the *only* place you may touch `IServiceCollection` (it's frozen after Build). `OnInitialized` is the *only* place you should resolve services (the collection isn't built yet during `Initialize`).

### 3.3 Module folder layout

A built plugin module is a flat folder containing:

```
@Email/resend.module/
├── OutWit.Shared.Email.Provider.Resend.dll   ← the plugin entry point
├── OutWit.Shared.Email.Provider.Resend.deps.json
├── Resend.dll                              ← vendor SDK
├── OutWit.Common.Email.dll                 ← OutWit base abstraction
├── …all other transitive deps the plugin needs to run…
└── appsettings.json                        ← optional plugin-private config
```

Why all the transitive DLLs sit in the module folder rather than in the host's main output: **plugin assembly resolution** is local to the module. The plugin loader uses an `AssemblyLoadContext` that probes the plugin's folder first. This isolates plugin dependencies — version 1.4 of `Resend.dll` in `resend.module/` doesn't collide with a different version another plugin might ship.

#### One root folder per plugin category

Every host picks a **root folder** the loader scans. The host is free to use a single shared root (`@Plugins/`) or split by category (`@Email/`, `@Logging/`, `@Database/`). The OutWit ecosystem follows the **category-per-root** convention:

| Plugin category | Root folder | Plugin contract |
|---|---|---|
| Email transports | `@Email/` | `IEmailProviderPlugin` |
| Log query backends | `@Logging/` | `ILogProviderPlugin` |
| Database providers | `@Database/` | `IDatabaseProviderPlugin` |

Benefits over a single mixed root:

- **No module-name collisions** — `null.module/` inside `@Email/` is unambiguous; if it were under `@Plugins/` next to a Log-side `null.module/`, the two would fight for the same path.
- **Loader I/O scoped to one category** — `WitPluginLoader<IEmailProviderPlugin>` reads only `@Email/`, not every plugin in the system.
- **Operator-facing clarity** — `ls @Email/` is self-explanatory; `rm -rf @Logging/` cleanly removes a whole category.

The convention is enforced at the plugin's side: its `build/*.targets` writes to `$(OutputPath)@Email/<key>.module/` (or whichever category the plugin belongs to). Hosts wire `WitPluginLoader<TPlugin>` to the matching root in their startup code.

Norav.Records uses the same shape with its own category names (`@Providers/` for record format plugins, `@Encryptors/` for encryption plugins). The pattern is the same — one root per plugin contract.

### 3.4 Isolated assembly load contexts

`WitPluginLoader<T>` takes a `useIsolatedContexts` flag. Two modes:

| `useIsolatedContexts` | When to use | Trade-off |
|---|---|---|
| `true` (default) | When you want to **unload** plugins at runtime (hot reload, per-tenant unloading) | Plugin assemblies are loaded into a dedicated `AssemblyLoadContext`. Disposing the loader unloads them. |
| `false` | When you don't need unloading — the simpler case | Plugins share the default load context. Lower complexity, no `Unload()` support. |

For most server-side hosts the answer is `false`: plugins are loaded once at startup and the host runs until process exit. Norav.Records, WitIdentity, WitEngine all use `false`.

### 3.5 Plugin discovery (what the loader actually scans)

The loader:

1. Lists every `*.dll` in the given module folder, recursively.
2. Deduplicates by simple filename (a shared dependency that ships in two modules is loaded once for metadata inspection).
3. For each candidate, opens it in a `MetadataLoadContext` (read-only, no actual code execution) and looks for types decorated with `[WitPluginManifest]` that implement `T` (the loader's generic parameter).
4. Builds a topological ordering using `[WitPluginDependency]` declarations + priorities, detects cycles + missing deps + version mismatches.
5. For each plugin in order, *actually* loads the assembly into the runtime `AssemblyLoadContext`, instantiates the plugin class via reflection, returns it.

Everything from step 5 forward is what the host iterates in `foreach plugin: plugin.Initialize(services)`.

---

## 4. Writing a plugin — step by step

We'll build a fictional **Mailgun email plugin** for WitIdentity. The pattern transfers to any host.

### 4.1 Set up the project

```bash
mkdir OutWit.Shared.Email.Provider.Mailgun
cd OutWit.Shared.Email.Provider.Mailgun
dotnet new classlib -f net10.0
```

`OutWit.Shared.Email.Provider.Mailgun.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <Version>1.0.0</Version>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>

    <!-- Self-contained plugin runtime: copy NuGet deps next to the plugin DLL so
         WitPluginLoader resolves the full closure from the module folder alone. -->
    <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>

    <Description>Mailgun email transport plugin for OutWit hosts (WitIdentity, WitEngine, …).</Description>
    <PackageTags>OutWit;Email;Plugin;Mailgun</PackageTags>

    <!-- Plugin packages use a hand-rolled nuspec instead of the SDK Pack output. -->
    <NuspecFile>$(MSBuildProjectName).nuspec</NuspecFile>
    <NuspecProperties>version=$(Version)</NuspecProperties>
    <IncludeSymbols>false</IncludeSymbols>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="OutWit.Common.Plugins.Abstractions" Version="1.1.*" />
    <PackageReference Include="OutWit.Common.Email" Version="1.0.*" />
    <PackageReference Include="OutWit.Shared.Email.Providers" Version="1.0.*" />
    <!-- Vendor SDK -->
    <PackageReference Include="RestSharp" Version="112.1.0" />
  </ItemGroup>

  <!--
    Stage the plugin into the solution's local @Email folder after build.
    AfterTargets="Build" ensures NuGet-resolved deps are already in TargetDir
    by the time we glob *.dll. The Condition guards against running this when
    MSBuild has no SolutionDir (i.e. during `dotnet pack` from outside a solution).
  -->
  <Target Name="CopyPluginToOutputDirectory"
          AfterTargets="Build"
          Condition="'$(SolutionDir)' != '' And '$(SolutionDir)' != '*Undefined*'">
    <ItemGroup>
      <PluginFiles Include="$(TargetDir)*.dll" />
      <PluginFiles Include="$(TargetDir)$(TargetName).deps.json"
                   Condition="Exists('$(TargetDir)$(TargetName).deps.json')" />
      <PluginFiles Include="$(MSBuildThisFileDirectory)appsettings.json"
                   Condition="Exists('$(MSBuildThisFileDirectory)appsettings.json')" />
    </ItemGroup>

    <Copy SourceFiles="@(PluginFiles)"
          DestinationFolder="$(SolutionDir)@Email\$(Configuration)\mailgun.module\%(RecursiveDir)"
          SkipUnchangedFiles="true"
          OverwriteReadOnlyFiles="true" />
  </Target>

</Project>
```

Two important details:

- **`NuspecFile`** — we override the default Pack with a hand-written `.nuspec` (see §7). Otherwise `dotnet pack` would produce a regular library package, which is not what we want.
- **`CopyPluginToOutputDirectory` MSBuild target** — when the plugin is built *inside* a solution that also contains a host (developer workflow), this target lays the plugin files out in the solution's `@Email/<Config>/mailgun.module/` folder. The host's own build then picks them up (see §5.4). The `Condition` ensures this is a no-op when `dotnet pack` builds the project in isolation.

### 4.2 Define the plugin class

`MailgunEmailProviderPlugin.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.Email;
using OutWit.Common.Plugins.Abstractions;
using OutWit.Common.Plugins.Abstractions.Attributes;
using OutWit.Shared.Email.Providers;

namespace OutWit.Shared.Email.Provider.Mailgun
{
    [WitPluginManifest("Mailgun Email Provider", Version = "1.0.0")]
    public sealed class MailgunEmailProviderPlugin : WitPluginBase, IEmailProviderPlugin
    {
        #region IEmailProviderPlugin

        public string Key => "Mailgun";

        #endregion

        #region IWitPlugin

        public override void Initialize(IServiceCollection services)
        {
            // Read this plugin's own appsettings.json — see §6.
            var config = PluginConfiguration.Load(typeof(MailgunEmailProviderPlugin).Assembly);

            var apiKey = config["Mailgun:ApiKey"]
                ?? throw new InvalidOperationException(
                    "Mailgun:ApiKey is not configured (set via appsettings.json or Mailgun__ApiKey env var).");
            var domain = config["Mailgun:Domain"]
                ?? throw new InvalidOperationException("Mailgun:Domain is not configured.");

            services.AddHttpClient<MailgunHttpClient>();
            services.AddSingleton(new MailgunOptions(apiKey, domain));
            services.AddSingleton<IEmailTransport, MailgunEmailTransport>();
        }

        public override void OnInitialized(IServiceProvider serviceProvider)
        {
            // Most email plugins have nothing to do here — the transport is
            // already registered. Use this pass if you need to wire into a
            // registry owned by another plugin or by the host.
        }

        #endregion
    }
}
```

Anatomy:

- Inherits **`WitPluginBase`** (provides default no-op `OnUnloading`, default `Priority=0`).
- Implements **`IEmailProviderPlugin`** — the host's contract (lives in `OutWit.Shared.Email.Providers`). The discriminator `Key` lets the host pick *this* plugin when its operator sets `Email__ProviderKey=Mailgun`.
- Decorated with **`[WitPluginManifest("display name", Version = "x.y.z")]`** — required for the loader to recognize the class as a plugin.
- The `Initialize` method registers everything the transport needs *and* the `IEmailTransport` itself. The host resolves `IEmailTransport` later and doesn't care which plugin provided it.

### 4.3 The actual transport implementation

`MailgunEmailTransport.cs` (sketch — vendor-specific details elided):

```csharp
public sealed class MailgunEmailTransport : IEmailTransport
{
    private readonly MailgunHttpClient m_http;
    private readonly MailgunOptions m_options;

    public MailgunEmailTransport(MailgunHttpClient http, MailgunOptions options)
    {
        m_http = http;
        m_options = options;
    }

    public async Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        try
        {
            var messageId = await m_http.SendAsync(m_options.Domain, message, ct);
            return EmailSendResult.Success(messageId);
        }
        catch (UnauthorizedException ex)
        {
            return EmailSendResult.Failure(EmailFailureKind.AuthFailure, ex.Message);
        }
        catch (RateLimitException ex)
        {
            return EmailSendResult.Failure(EmailFailureKind.RateLimited, ex.Message);
        }
        // ... map other vendor errors to EmailFailureKind variants ...
    }
}
```

**Critical**: the transport translates vendor failures into the neutral `EmailFailureKind` enum. That's how the host decides whether to retry, alert, or give up — without knowing which vendor failed.

### 4.4 Stage and verify locally

```bash
dotnet build -c Release
ls @Email/Release/mailgun.module/
# OutWit.Shared.Email.Provider.Mailgun.dll
# OutWit.Shared.Email.Provider.Mailgun.deps.json
# RestSharp.dll
# OutWit.Common.Email.dll
# OutWit.Common.Plugins.Abstractions.dll
# OutWit.Shared.Email.Providers.dll
# appsettings.json
```

If the host is built next, its `PreBuild` target (§5.4) will copy this folder into the host's output. Or you can hand-copy for a one-off test:

```bash
cp -r @Email/Release/mailgun.module /path/to/host/bin/Release/net10.0/@Email/
```

---

## 5. Writing a host — step by step

If you're building a service that *consumes* plugins, the host side is responsible for three things:

1. **Define the contract** — a marker interface inheriting `IWitPlugin`.
2. **Load plugins** — wire `WitPluginLoader` into your DI setup.
3. **Copy modules to the output** — for in-solution development.

### 5.1 Define the contract

A marker interface keeps plugin categories separate. If a host has both email plugins and log plugins, two interfaces means two `WitPluginLoader<T>` instances each filtering for its own category:

```csharp
namespace OutWit.Shared.Email.Providers
{
    public interface IEmailProviderPlugin : IWitPlugin
    {
        /// <summary>Discriminator chosen by an operator, e.g. "Resend", "Mailgun", "Smtp".</summary>
        string Key { get; }
    }
}
```

`IWitPlugin` brings in `Initialize` / `OnInitialized` / `OnUnloading`; the sub-interface adds the host-specific shape (here: a `Key` for `ProviderKey`-based selection).

### 5.2 Plugin loader integration

Wrap the loader so callers see a single extension method:

```csharp
public static class EmailServiceCollectionExtensions
{
    public static IServiceCollection AddEmailPlugins(this IServiceCollection services,
        string? moduleFolder = null,
        ILogger? logger = null)
    {
        moduleFolder ??= Path.Combine(AppContext.BaseDirectory, "@Email");

        var loader = new WitPluginLoader<IEmailProviderPlugin>(
            moduleFolder,
            useIsolatedContexts: false,
            logger: logger);

        loader.Load();

        services.AddSingleton(loader);
        foreach (var plugin in loader.Plugins)
            plugin.Initialize(services);

        return services;
    }
}
```

And in `Program.cs`:

```csharp
builder.Services.AddEmailPlugins();
var app = builder.Build();

// Second-pass init: now that the IServiceProvider exists, let each plugin
// look up cross-plugin dependencies.
var loader = app.Services.GetRequiredService<WitPluginLoader<IEmailProviderPlugin>>();
foreach (var plugin in loader.Plugins)
    plugin.OnInitialized(app.Services);

await app.RunAsync();
```

### 5.3 Selecting a single active plugin

Most hosts want exactly one provider active at runtime, picked by config. Pattern:

```csharp
// In Startup, before AddEmailPlugins:
var providerKey = configuration["Email:ProviderKey"] ?? "Null";

// Inside the plugin's Initialize you decide whether to register your transport
// based on whether YOU were selected:
public override void Initialize(IServiceCollection services)
{
    // The host's loader registered the plugin instance into DI under IEmailProviderPlugin —
    // every plugin's Initialize runs unconditionally. To respect the selection, gate the
    // IEmailTransport registration on the configured key:
    var selectedKey = HostEnvironment.GetSelectedKey("Email");
    if (!string.Equals(selectedKey, Key, StringComparison.OrdinalIgnoreCase))
        return;
    // ...register transport...
}
```

Alternative: register every plugin's transport keyed by name and have the host resolve the active one via `IServiceProviderIsKeyedService` (.NET 8+). Norav.Records does the registry pattern instead — every record provider registers, and the host picks the right one by file extension at runtime.

### 5.4 PreBuild copy for in-solution development

When a host and one of its plugins live in the same solution, the plugin's `CopyPluginToOutputDirectory` target stages files into `<solution>/@Email/<Config>/<key>.module/`. The host needs a matching `PreBuild` target to copy those into its own output directory:

```xml
<!-- In the host csproj -->
<Target Name="PreBuild" BeforeTargets="Build">
  <ItemGroup>
    <PluginModuleFiles Include="$(MSBuildThisFileDirectory)..\@Email\$(Configuration)\**\*" />
  </ItemGroup>
  <Copy SourceFiles="@(PluginModuleFiles)"
        DestinationFolder="$(TargetDir)@Email\%(RecursiveDir)"
        SkipUnchangedFiles="true" />
</Target>

<Target Name="PostPublish" AfterTargets="Publish">
  <ItemGroup>
    <PluginModulePublishFiles Include="$(MSBuildThisFileDirectory)..\@Email\$(Configuration)\**\*" />
  </ItemGroup>
  <Copy SourceFiles="@(PluginModulePublishFiles)"
        DestinationFolder="$(PublishDir)@Email\%(RecursiveDir)"
        SkipUnchangedFiles="true" />
</Target>
```

When the host is built *outside* the plugin's solution and pulls plugins via NuGet, the package's own `build/*.targets` (see §7) takes over — same end state in `$(OutputPath)@Email/<module>/`. The host doesn't care which path put the files there.

---

## 6. Per-plugin configuration

Each plugin owns its own `appsettings.json`, deployed inside its module folder:

```
@Email/mailgun.module/
├── OutWit.Shared.Email.Provider.Mailgun.dll
├── appsettings.json          ← plugin-private config
└── …deps…
```

A typical `appsettings.json` for the Mailgun plugin:

```json
{
  "Mailgun": {
    "ApiKey": "",
    "Domain": "",
    "Region": "us"
  }
}
```

The plugin reads this file in `Initialize` using standard .NET configuration:

```csharp
public override void Initialize(IServiceCollection services)
{
    var assemblyDir = Path.GetDirectoryName(typeof(MailgunEmailProviderPlugin).Assembly.Location)!;
    var config = new ConfigurationBuilder()
        .SetBasePath(assemblyDir)
        .AddJsonFile("appsettings.json", optional: true)
        .AddEnvironmentVariables()     // ← standard binding: Mailgun__ApiKey overrides Mailgun:ApiKey
        .Build();

    var apiKey = config["Mailgun:ApiKey"];
    // ...
}
```

**Conventions for plugin config**:

- **Keep secrets out of `appsettings.json`** — ship the file with blank values. Real values come from env vars at deploy time.
- **Use the standard double-underscore env-var convention** — `Mailgun__ApiKey` overrides `Mailgun:ApiKey` automatically via `AddEnvironmentVariables()`. No manual `Environment.GetEnvironmentVariable` calls.
- **No product prefix** — `Mailgun__ApiKey`, not `WIT_IDENTITY_MAILGUN_API_KEY`. The same plugin DLL ships into multiple OutWit products; each container provides its own value.
- **Section name = plugin Key** — `[Mailgun]` for `Key="Mailgun"`, `[Resend]` for `Key="Resend"`. Makes the env-var name predictable.

The host **never** reads a plugin's `appsettings.json`. That file is part of the plugin's private contract with its operator.

---

## 7. Distribution: NuGet packaging

A plugin ships as a NuGet package with a specific layout that triggers automatic file copy on the consumer side.

### 7.1 The nuspec

`OutWit.Shared.Email.Provider.Mailgun.nuspec`, sitting next to the csproj:

```xml
<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://schemas.microsoft.com/packaging/2012/06/nuspec.xsd">
  <metadata>
    <id>OutWit.Shared.Email.Provider.Mailgun</id>
    <version>$version$</version>
    <authors>Your Name</authors>
    <description>Mailgun email transport plugin for OutWit hosts.</description>
    <copyright>Copyright © 2026 Your Name</copyright>
    <license type="expression">Apache-2.0</license>
    <tags>OutWit Email Plugin Mailgun</tags>
  </metadata>
  <files>
    <file src="build\OutWit.Shared.Email.Provider.Mailgun.targets"
          target="build" />
    <file src="build\OutWit.Shared.Email.Provider.Mailgun.targets"
          target="buildTransitive" />
    <file src="..\..\@Email\Release\mailgun.module\**\*"
          target="plugins\mailgun.module" />
    <file src="bin\Release\net10.0\OutWit.Shared.Email.Provider.Mailgun.dll"
          target="lib\net10.0" />
  </files>
</package>
```

Four file entries, four jobs:

| Pack source | Pack target | Why |
|---|---|---|
| `build\<id>.targets` | `build/` | NuGet auto-imports this targets file into a consumer that adds your package as a `PackageReference`. |
| `build\<id>.targets` | `buildTransitive/` | Same file, but auto-imported when your package is referenced *transitively* through another package. |
| `..\..\@Email\Release\mailgun.module\**\*` | `plugins/mailgun.module/` | The actual plugin module folder — everything staged by the csproj's `CopyPluginToOutputDirectory`. |
| `bin\Release\net10.0\<id>.dll` | `lib/net10.0/` | Formal "library for this TFM" entry — required for NuGet metadata sanity. See §8 for what consumers do about the compile reference this implies. |

The `$version$` placeholder is filled in by MSBuild via `<NuspecProperties>version=$(Version)</NuspecProperties>` in the csproj.

### 7.2 The consumer-side targets file

`build/OutWit.Shared.Email.Provider.Mailgun.targets` (lives next to the csproj, packaged into the .nupkg):

```xml
<Project>
  <Target Name="_CopyMailgunPlugin" AfterTargets="Build">
    <ItemGroup>
      <_MailgunFiles Include="$(MSBuildThisFileDirectory)..\plugins\mailgun.module\**\*" />
    </ItemGroup>
    <Copy SourceFiles="@(_MailgunFiles)"
          DestinationFolder="$(OutputPath)@Email\mailgun.module\%(RecursiveDir)"
          SkipUnchangedFiles="true" />
  </Target>

  <Target Name="_CopyMailgunPluginPublish" AfterTargets="Publish">
    <ItemGroup>
      <_MailgunPubFiles Include="$(MSBuildThisFileDirectory)..\plugins\mailgun.module\**\*" />
    </ItemGroup>
    <Copy SourceFiles="@(_MailgunPubFiles)"
          DestinationFolder="$(PublishDir)@Email\mailgun.module\%(RecursiveDir)"
          SkipUnchangedFiles="true" />
  </Target>
</Project>
```

The path math: when imported via NuGet, `$(MSBuildThisFileDirectory)` resolves to `<global-packages>/<package>/<version>/build/`. So `..\plugins\mailgun.module\**\*` matches the files we packed under `plugins/mailgun.module/`.

Two targets — Build and Publish — because hosts that publish via `dotnet publish` need the plugin in `$(PublishDir)`, not just `$(OutputPath)`.

### 7.3 The pack command

Once the nuspec is in place:

```bash
dotnet build -c Release   # produces @Email/Release/mailgun.module/...
dotnet pack -c Release    # produces OutWit.Shared.Email.Provider.Mailgun.<version>.nupkg
```

The csproj's `NuspecFile` property tells `dotnet pack` to use your file instead of generating one.

### 7.4 The README

`Directory.Build.props` typically ships a shared root README into every package via:

```xml
<PropertyGroup>
  <PackageReadmeFile>README.md</PackageReadmeFile>
</PropertyGroup>
<ItemGroup>
  <None Include="$(MSBuildThisFileDirectory)README.md" Pack="true" PackagePath="\" />
</ItemGroup>
```

For plugin packages this works — every plugin in the family ships the same family README. If your plugin needs its own README, override `<PackageReadmeFile>` per-project and point to a local file.

---

## 8. Consumer-side reference handling

When a host adds a plugin via `dotnet add package`, NuGet's default behavior puts `lib/net10.0/<plugin>.dll` into the consumer's *compile references*. For a plugin this is usually unwanted — the host should only see the abstractions, not the concrete plugin classes.

Three viable patterns:

### Pattern A — accept the unused reference

```xml
<PackageReference Include="OutWit.Shared.Email.Provider.Mailgun" Version="1.0.0" />
```

The host's compile-time graph gains `OutWit.Shared.Email.Provider.Mailgun.dll` as a reference. It's never imported by any `using`, so it sits unused. **Pro**: zero ceremony. **Con**: the unused reference shows up in `dotnet list package` output and the host has slightly more compile-time noise.

### Pattern B — exclude the compile reference, keep the build targets

```xml
<PackageReference Include="OutWit.Shared.Email.Provider.Mailgun" Version="1.0.0">
  <ExcludeAssets>compile;runtime</ExcludeAssets>
  <IncludeAssets>build</IncludeAssets>
</PackageReference>
```

Tells NuGet: "import the build targets, ignore the lib." The plugin's targets file still runs (copying the module folder to the output), but the host never gets a compile-time reference to the plugin assembly. **Pro**: clean host compile graph; the host only references the abstractions, exactly as the plugin pattern intends. **Con**: every consumer has to remember this incantation per `PackageReference`.

### Pattern C — drop the `lib/` entry from the nuspec

The plugin author simply doesn't ship `lib/<TFM>/<plugin>.dll`. NuGet still accepts the package (the `build/` entries are enough), and there's no compile reference to suppress on the consumer side.

```xml
<!-- nuspec: NO lib/net10.0 entry -->
<files>
  <file src="build\<id>.targets" target="build" />
  <file src="build\<id>.targets" target="buildTransitive" />
  <file src="..\..\@Email\Release\<module>\**\*" target="plugins\<module>" />
</files>
```

**Pro**: consumers just `dotnet add package` with no ceremony. **Con**: tooling that scans NuGet packages for "real" library content may flag the package as content-only or library-less. Some private NuGet feeds add warnings.

**Recommendation**: **Pattern A** for OutWit's own first-party plugins (the noise is negligible inside our own product trees). **Pattern B** for community-published plugins or for hosts that want strict isolation. Pattern C is fine for small/experimental plugin sets — the rest of the ecosystem mostly settles on A or B.

---

## 9. Testing plugins

### 9.1 In-solution integration tests

The host's test project should follow the same `PreBuild` pattern as the host itself, so the test harness sees plugins in its output folder:

```xml
<!-- HostTests.csproj -->
<Target Name="PreBuild" BeforeTargets="Build">
  <ItemGroup>
    <PluginModuleFiles Include="$(MSBuildThisFileDirectory)..\@Email\$(Configuration)\**\*" />
  </ItemGroup>
  <Copy SourceFiles="@(PluginModuleFiles)"
        DestinationFolder="$(TargetDir)@Email\%(RecursiveDir)"
        SkipUnchangedFiles="true" />
</Target>
```

Then write tests against the loader's contract:

```csharp
[TestFixture]
public class EmailPluginIntegrationTests
{
    [Test]
    public async Task MailgunPluginIsDiscoveredAndRegistersTransportTest()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(BuildConfiguration());
        services.AddEmailPlugins(moduleFolder: Path.Combine(AppContext.BaseDirectory, "@Email"));
        var sp = services.BuildServiceProvider();

        var loader = sp.GetRequiredService<WitPluginLoader<IEmailProviderPlugin>>();
        foreach (var plugin in loader.Plugins)
            plugin.OnInitialized(sp);

        var transport = sp.GetRequiredService<IEmailTransport>();
        Assert.That(transport, Is.InstanceOf<MailgunEmailTransport>());
    }
}
```

### 9.2 Unit tests for the plugin class itself

You can unit-test a plugin's behavior without going through the loader. The plugin class is just a regular .NET class:

```csharp
[Test]
public void InitializeRegistersTransportWhenApiKeyConfiguredTest()
{
    Environment.SetEnvironmentVariable("Mailgun__ApiKey", "test-key");
    Environment.SetEnvironmentVariable("Mailgun__Domain", "example.com");

    var plugin = new MailgunEmailProviderPlugin();
    var services = new ServiceCollection();

    plugin.Initialize(services);

    var sp = services.BuildServiceProvider();
    Assert.That(sp.GetService<IEmailTransport>(), Is.InstanceOf<MailgunEmailTransport>());
}
```

### 9.3 Vendor mocking

For testing the actual transport logic without hitting a live vendor:

- **HTTP-based vendors** (Resend, Mailgun, Loki, NewRelic) → inject a custom `HttpMessageHandler` into the typed `HttpClient`. See `OutWit.Common.Logging.Loki.Tests` for a worked example using `HttpMessageHandlerStub`.
- **SMTP vendors** → run [MailHog](https://github.com/mailhog/MailHog) or [Papercut-SMTP](https://github.com/ChangemakerStudios/Papercut-SMTP) in a docker-compose service and point the plugin at `localhost`.
- **SDK-based vendors** without HTTP abstraction → mock the SDK's interface (Moq, NSubstitute).

---

## 10. Quick reference

### Module folder layout

```
@Email/                       ← parent folder the host scans (configurable)
├── resend.module/              ← one folder per plugin; name typically matches Key
│   ├── <plugin>.dll
│   ├── <plugin>.deps.json
│   ├── <vendor-sdk>.dll
│   ├── …transitive deps…
│   └── appsettings.json        ← plugin-private config
├── mailgun.module/
│   └── …
└── null.module/                ← every host should ship a zero-config fallback
    └── …
```

### Naming conventions

| Thing | Convention | Example |
|---|---|---|
| Plugin assembly | `OutWit.<Product>.<Category>.Plugin.<Vendor>` | `OutWit.Shared.Email.Provider.Resend` |
| Plugin class | `<Vendor><Category>ProviderPlugin` | `ResendEmailProviderPlugin` |
| Plugin folder | `<vendor-lowercase>.module` | `resend.module` |
| Plugin Key | `<Vendor>` (PascalCase or as documented by host) | `"Resend"`, `"Mailgun"`, `"NewRelic"` |
| Config section | matches Key | `[Resend]`, `[Mailgun]` |
| Env-var override | `<Section>__<Key>` | `Resend__ApiToken` |

### Lifecycle

```
Discovery        Initialize        BuildServiceProvider        OnInitialized        … runtime …        OnUnloading
   │                  │                       │                       │                                       │
   │                  │                       │                       │                                       │
   ▼                  ▼                       ▼                       ▼                                       ▼
loader.Load()    foreach plugin           services.BuildSp()       foreach plugin                       Dispose / unload
                   .Initialize(svc)                                   .OnInitialized(sp)
```

### Required attributes

| Attribute | Required | Notes |
|---|---|---|
| `[WitPluginManifest("DisplayName", Version = "x.y.z")]` | **Yes** | One per plugin class. `Priority` and `Description` are optional. |
| `[WitPluginDependency("OtherPluginName", MinimumVersion = "x.y.z")]` | No | Declares a dependency on another plugin by name. The loader resolves load order. |

### csproj boilerplate (plugin)

```xml
<CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>

<NuspecFile>$(MSBuildProjectName).nuspec</NuspecFile>
<NuspecProperties>version=$(Version)</NuspecProperties>
<IncludeSymbols>false</IncludeSymbols>

<Target Name="CopyPluginToOutputDirectory"
        AfterTargets="Build"
        Condition="'$(SolutionDir)' != '' And '$(SolutionDir)' != '*Undefined*'">
  <ItemGroup>
    <PluginFiles Include="$(TargetDir)*.dll" />
    <PluginFiles Include="$(TargetDir)$(TargetName).deps.json"
                 Condition="Exists('$(TargetDir)$(TargetName).deps.json')" />
    <PluginFiles Include="$(MSBuildThisFileDirectory)appsettings.json"
                 Condition="Exists('$(MSBuildThisFileDirectory)appsettings.json')" />
  </ItemGroup>
  <Copy SourceFiles="@(PluginFiles)"
        DestinationFolder="$(SolutionDir)@Email\$(Configuration)\<key>.module\%(RecursiveDir)"
        SkipUnchangedFiles="true" />
</Target>
```

### nuspec boilerplate (plugin)

```xml
<files>
  <file src="build\<id>.targets" target="build" />
  <file src="build\<id>.targets" target="buildTransitive" />
  <file src="..\..\@Email\Release\<key>.module\**\*" target="<category>\<key>.module" />
  <file src="bin\Release\net10.0\<id>.dll" target="lib\net10.0" />
</files>
```

### build/*.targets boilerplate (plugin)

```xml
<Project>
  <Target Name="_Copy<Name>Plugin" AfterTargets="Build">
    <ItemGroup>
      <_<Name>Files Include="$(MSBuildThisFileDirectory)..\plugins\<key>.module\**\*" />
    </ItemGroup>
    <Copy SourceFiles="@(_<Name>Files)"
          DestinationFolder="$(OutputPath)@Email\<key>.module\%(RecursiveDir)" />
  </Target>
  <Target Name="_Copy<Name>PluginPublish" AfterTargets="Publish">
    <ItemGroup>
      <_<Name>PubFiles Include="$(MSBuildThisFileDirectory)..\plugins\<key>.module\**\*" />
    </ItemGroup>
    <Copy SourceFiles="@(_<Name>PubFiles)"
          DestinationFolder="$(PublishDir)@Email\<key>.module\%(RecursiveDir)" />
  </Target>
</Project>
```

### csproj boilerplate (host)

```xml
<Target Name="PreBuild" BeforeTargets="Build">
  <ItemGroup>
    <PluginModuleFiles Include="$(MSBuildThisFileDirectory)..\@Email\$(Configuration)\**\*" />
  </ItemGroup>
  <Copy SourceFiles="@(PluginModuleFiles)"
        DestinationFolder="$(TargetDir)@Email\%(RecursiveDir)" />
</Target>

<Target Name="PostPublish" AfterTargets="Publish">
  <ItemGroup>
    <PluginModulePublishFiles Include="$(MSBuildThisFileDirectory)..\@Email\$(Configuration)\**\*" />
  </ItemGroup>
  <Copy SourceFiles="@(PluginModulePublishFiles)"
        DestinationFolder="$(PublishDir)@Email\%(RecursiveDir)" />
</Target>
```

---

## 11. Reference implementations

Real, working solutions you can study end-to-end:

### Norav.Records

A medical-records platform with two parallel plugin categories: **record providers** (one per file format: NRR, NRE, EDF, Braemar, WFDB) and **encryption providers** (general, monitor). Every plugin follows the exact pattern documented here.

Key files to read:

| File | What it teaches |
|---|---|
| `Norav.Records.Interfaces/IRecordProviderModule.cs` | Defining a plugin contract by marker-inheriting `IWitPlugin`. |
| `Norav.Records/RecordProviders.cs` | Wrapping `WitPluginLoader` in a domain-specific façade with two-phase init. |
| `Norav.Records/RecordServiceCollectionExtensions.cs` | DI extension method (`AddRecordProviders`). |
| `Braemar/Norav.Records.BraemarStreamProvider/*` | A complete plugin: csproj + nuspec + build/.targets + plugin class. |
| `Encryption/Norav.Encryption.General/*` | A second plugin category (encryptors) using the same pattern with a different staging folder (`@Encryptors`). |
| `Norav.Records.Viewer/Norav.Records.Viewer.csproj` | A host that copies from `@Providers/$(Configuration)` and `@Encryptors/$(Configuration)` in PreBuild + PostPublish. |

### OutWit ecosystem

Once `OutWit/Shared` lands, the canonical examples will be:

| Host | Plugin category | Plugin examples |
|---|---|---|
| `OutWit.Identity` | Email — `IEmailProviderPlugin` | `.Resend`, `.Smtp`, `.Null` |
| `OutWit.Identity` | Log query — `ILogProviderPlugin` | `.NewRelic`, `.Loki`, `.File` |
| `OutWit.Identity` | Database — `IDatabaseProviderPlugin` | `.PostgreSql`, `.WitDatabase` |
| `OutWit.Engine` | Same categories, reuses the same plugin packages | (no per-product plugins for Email/Log) |

The database category is per-product (schema-coupled — each product has its own `DbContext` / migrations / EF model). Email and Log are cross-product (one plugin DLL serves any OutWit host).

---

## 12. FAQ

**Q: Can I write a plugin in F# / Visual Basic / any other .NET language?**
A: Yes. The loader inspects metadata; it doesn't care about source language. The `[WitPluginManifest]` attribute exists in any CLI language.

**Q: Can my plugin target a different `TargetFramework` than the host?**
A: It needs to be compatible with the host's framework. A `net8.0` plugin loads into a `net10.0` host fine. The reverse (newer plugin, older host) won't work — the runtime can't load it.

**Q: Can a plugin depend on another plugin?**
A: Yes — declare `[WitPluginDependency("OtherPluginName", MinimumVersion = "x.y.z")]`. The loader orders `Initialize` calls so dependencies run first. You may also resolve services registered by other plugins inside `OnInitialized`.

**Q: How do I unload a plugin at runtime?**
A: Create the loader with `useIsolatedContexts: true`, then call `loader.UnloadPlugin(pluginName)`. The plugin's `AssemblyLoadContext` is collected on the next GC. Note: any references to plugin types held by the host or other plugins will keep the assembly alive.

**Q: Where does my plugin's `appsettings.json` get read from?**
A: From inside the plugin's module folder at runtime (`@Email/<key>.module/appsettings.json`). Use `Assembly.Location` + `Path.GetDirectoryName` to find your own directory. See §6 for the recommended `ConfigurationBuilder` setup.

**Q: My plugin needs a binary native dependency. Where does it go?**
A: Same module folder as the managed DLLs. The plugin's `AssemblyLoadContext` resolves native dependencies relative to the managed assembly's location, the same way a regular .NET process does.

**Q: Can a single NuGet package ship more than one plugin?**
A: Technically yes — pack multiple plugin DLLs into the same `plugins/<key>.module/` folder, with a separate `[WitPluginManifest]` on each class. Each will be discovered independently. In practice, **one plugin per package** is cleaner — operators can mix and match.

**Q: Where do logs from plugins go?**
A: Plugins receive `ILoggerFactory` from the host's DI (resolve it in `OnInitialized` or take it as a ctor parameter on your registered services). Plugin logs flow through the host's logging pipeline — Serilog, NLog, console, whatever the host configured. The host's own logging stack is therefore the single source of truth.

**Q: Can plugins talk to each other directly?**
A: Through DI. Plugin A registers a service in `Initialize`; plugin B resolves it in `OnInitialized`. There's no separate plugin-to-plugin channel — everything goes through `IServiceProvider`. If you need ordering guarantees, declare an explicit `[WitPluginDependency]`.

**Q: Does the loader call `Dispose` on plugins?**
A: Yes if the plugin implements `IDisposable` or `IAsyncDisposable`, and the loader is disposed (or the plugin is unloaded). The loader's own `Dispose` cascades. `WitPluginBase` doesn't implement `IDisposable` by default — opt in if you have resources to clean up.

---

## License

Apache 2.0 — same as `OutWit.Common.Plugins`. This guide may be redistributed and adapted for any OutWit-compatible host's documentation.

## Feedback

Found a gotcha not covered here? Open an issue against the host project that surfaced it (WitIdentity, WitEngine, Norav.Records, …) — host-specific quirks accumulate fastest in the host's own README.
