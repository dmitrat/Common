# OutWit.Common.DependencyInjection

Property-based dependency injection for .NET using the `[Inject]` attribute.
Replaces large constructor parameter lists with declarative, lazily-resolved
properties powered by AspectInjector IL weaving — works on top of the standard
`Microsoft.Extensions.DependencyInjection` container with no special host.

A bundled Roslyn source generator (`[InjectableHost]`) can also emit the
`IServiceProvider` constructor boilerplate, so a host class only needs to
declare its `[Inject]` properties.

## Why

Standard .NET DI requires every dependency in the constructor:

```csharp
public class ClientPool
{
    public ClientPool(
        ILogger<ClientPool> logger,
        ConfigurationService configuration,
        ClientStore store,
        IServiceProvider serviceProvider)
    {
        // ...and growing.
    }
}
```

With `[Inject]`, dependencies become properties. The constructor only takes
`IServiceProvider`, which DI supplies automatically:

```csharp
public class ClientPool
{
    private readonly IServiceProvider m_serviceProvider;

    public ClientPool(IServiceProvider serviceProvider)
    {
        m_serviceProvider = serviceProvider;
    }

    [Inject] public ILogger<ClientPool> Logger { get; set; } = null!;
    [Inject] public ConfigurationService Configuration { get; set; } = null!;
    [Inject] public ClientStore Store { get; set; } = null!;
}
```

Or, with `[InjectableHost]` and the bundled generator, you can drop the
constructor entirely:

```csharp
[InjectableHost]
public partial class ClientPool
{
    [Inject] public ILogger<ClientPool> Logger { get; set; } = null!;
    [Inject] public ConfigurationService Configuration { get; set; } = null!;
    [Inject] public ClientStore Store { get; set; } = null!;
}
```

Registration stays standard:

```csharp
services.AddSingleton<ClientPool>();
```

## Install

```bash
dotnet add package OutWit.Common.DependencyInjection
```

The Roslyn source generator is shipped inside this package as an analyzer —
no separate install needed.

## How it works

The `[Inject]` attribute triggers an AspectInjector aspect (`InjectAspect`)
that, at compile time:

1. Mixes the `IInjectable` interface into the target class.
2. Intercepts the getter of every `[Inject]` property.
3. On the first read, resolves the service from `IServiceProvider` and caches
   it in the backing field. Subsequent reads return the cached value.

The aspect locates `IServiceProvider` in this order:

1. **`IInjectable.ServiceProvider`** — set explicitly via `InitInject()` or by
   the `Add*WithInject()` helpers.
2. **Instance field** — any instance field of type `IServiceProvider`
   (including private, protected, or inherited) is discovered automatically.
   The `[InjectableHost]` generator emits exactly such a field for you.

If no service provider is found, the getter is passive: it returns the current
backing field value as-is. This keeps `[Inject]`-annotated classes usable
without DI (for example, in tests).

## Quick start

```csharp
public class MyService
{
    private readonly IServiceProvider m_serviceProvider;

    public MyService(IServiceProvider serviceProvider)
    {
        m_serviceProvider = serviceProvider;
    }

    [Inject] public ILogger<MyService> Logger { get; set; } = null!;
    [Inject] public IRepository? Repository { get; set; }
}
```

```csharp
services.AddSingleton<MyService>();
```

That is the whole setup. Properties resolve lazily on first access; no special
registration helper is needed for the common case.

## `[InjectableHost]` — boilerplate-free hosts

For classes whose only constructor parameter is `IServiceProvider`, the
bundled Roslyn source generator can emit the constructor and the `Services`
field for you. Mark the class `[InjectableHost]` and declare it `partial`:

```csharp
[InjectableHost]
public partial class MyService
{
    [Inject] public ILogger<MyService> Logger { get; set; } = null!;
    [Inject] public IRepository? Repository { get; set; }
}
```

The generator emits:

```csharp
// MyService.InjectableHost.g.cs (auto-generated)
partial class MyService
{
    public MyService(global::System.IServiceProvider services)
    {
        Services = services;
    }

    private global::System.IServiceProvider Services { get; }
}
```

Registration is unchanged: `services.AddSingleton<MyService>();`.

**Requirements**:

- Class must be `partial`.
- Class must not declare its own constructor (the generator emits one). The
  generator reports `OWDI001` if the class is not partial, and `OWDI002` if
  the class has an explicit constructor.

**When to skip `[InjectableHost]`**: any class that needs additional ctor
parameters (e.g., `ctor(Guid clientId, IServiceProvider services)`) should
keep the explicit constructor and not use this attribute.

## Required vs optional

By default the property's nullability decides whether the service is required
or optional:

| Declaration | Behavior |
|---|---|
| `IFoo Foo { get; set; } = null!;` | Required — throws if not registered |
| `IFoo? Foo { get; set; }` | Optional — returns `null` if not registered |

Override with the `Requirement` property:

```csharp
// Force optional even though the property is non-nullable.
[Inject(Requirement = InjectRequirement.Optional)]
public IFoo Foo { get; set; } = null!;

// Force required even though the property is nullable.
[Inject(Requirement = InjectRequirement.Required)]
public IFoo? Foo { get; set; }
```

Shorthand aliases (see below) cover the same cases without the keyword soup:

```csharp
[InjectOptional] public IFoo Foo { get; set; } = null!;
[InjectRequired] public IFoo? Foo { get; set; }
```

## Resolution modes

`Mode` controls how the service is resolved on each property access:

| Mode | Behavior |
|---|---|
| `InjectMode.Cached` (default) | Resolve once from the owning service provider and cache in the backing field. |
| `InjectMode.Transient` | Resolve on every property access; never cache. Useful for a singleton owner that needs fresh transient/scoped services. |
| `InjectMode.Scoped` | Open a dedicated `IServiceScope` on first access, resolve from it, cache the result. The scope is disposed when the owner's `Dispose` / `DisposeAsync` is called. |

```csharp
public class MyChannel
{
    private readonly IServiceProvider m_serviceProvider;

    public MyChannel(IServiceProvider serviceProvider)
    {
        m_serviceProvider = serviceProvider;
    }

    [Inject(Mode = InjectMode.Transient)]
    public IDbContext DbContext { get; set; } = null!;
}
```

## Shorthand aliases

For the common combinations there are dedicated attribute names — all of them
inherit from `[Inject]` and behave identically to the long form. Pick whatever
reads best:

| Alias | Equivalent | Typical use |
|---|---|---|
| `[InjectScoped]` | `[Inject(Mode = InjectMode.Scoped)]` | A scoped DbContext injected into a singleton service. |
| `[InjectTransient]` | `[Inject(Mode = InjectMode.Transient)]` | A property resolved fresh per access. |
| `[InjectOptional]` | `[Inject(Requirement = InjectRequirement.Optional)]` | Force optional on a non-nullable property. |
| `[InjectRequired]` | `[Inject(Requirement = InjectRequirement.Required)]` | Force required on a nullable property. |

```csharp
[InjectableHost]
public partial class UserExecutionScopeService
{
    [InjectScoped]
    public ModelContext Db { get; set; } = null!;

    [InjectOptional]
    public IFeatureFlags Flags { get; set; } = null!;
}
```

## ServiceProvider discovery

The aspect finds `IServiceProvider` in this order:

1. **Field auto-discovery** — store `IServiceProvider` in any instance field.
   The aspect scans every field (including private and inherited). This is
   what `[InjectableHost]` emits for you.

2. **`InitInject` fallback** — call `this.InitInject(sp)` when the class does
   not store the provider in a field.

   ```csharp
   public class MyService
   {
       public MyService(IServiceProvider serviceProvider)
       {
           this.InitInject(serviceProvider);
       }

       [Inject] public IFoo Foo { get; set; } = null!;
   }
   ```

3. **Direct mixin assignment** — `((IInjectable)instance).ServiceProvider = sp;`
   This is what the `Add*WithInject` helpers do under the hood.

## Eager injection — `Add*WithInject`

For cases where you need every `[Inject]` property resolved at creation time
(not on first access), use the registration helpers:

```csharp
services.AddSingletonWithInject<MyService>();
services.AddSingletonWithInject<IMyService, MyServiceImpl>();

services.AddScopedWithInject<MyService>();
services.AddScopedWithInject<IMyService, MyServiceImpl>();

services.AddTransientWithInject<MyService>();
services.AddTransientWithInject<IMyService, MyServiceImpl>();
```

These methods:

- Create the instance via `ActivatorUtilities.CreateInstance`.
- Set `IServiceProvider` on the mixin (for any later lazy access).
- Resolve all `[Inject]` properties eagerly via `PropertyInjector.Inject`.

## Manual injection — `PropertyInjector`

For tests, factories, or anywhere you want a one-shot eager pass without going
through the aspect, call `PropertyInjector.Inject` directly:

```csharp
var instance = new MyService();
PropertyInjector.Inject(instance, serviceProvider);
```

`PropertyInjector` walks the type's `[Inject]` properties via reflection and
calls `GetRequiredService` / `GetService` per property. The reflection result
is cached per type, so repeated calls are cheap.

## Testing pattern — substitute by direct assignment

A `[Inject]` property has a setter, so tests can substitute any single
dependency without spinning up a real container. The aspect's getter honors
externally-assigned values (the `Cached` mode short-circuits when the backing
field is already non-null):

```csharp
[Test]
public void HappyPathTest()
{
    var channel = new ProjectAdministrationChannel(serviceProvider)
    {
        PrincipalStore = m_principalStoreFake   // overrides what [Inject] would have resolved
    };

    var result = await channel.GetProjectsAsync(0, 20);
    Assert.That(result.IsSuccess, Is.True);
}
```

`InjectMode.Transient` and `InjectMode.Scoped` properties intentionally
re-resolve on every access, so they cannot be substituted this way; for those,
use a real (or mocked) `IServiceProvider`.

## Inheritance

`[Inject]` works across the type hierarchy. Properties declared in base
classes are resolved alongside properties declared in derived classes. The
service-provider field can live on either the base or the derived class —
the locator walks the hierarchy.

```csharp
public class BaseService
{
    [Inject] public ILogger Logger { get; set; } = null!;
}

[InjectableHost]
public partial class DerivedService : BaseService
{
    [Inject] public IRepository? Repository { get; set; }
}
```

## Blazor: namespace collision

Blazor components have their own `[Inject]` attribute,
`Microsoft.AspNetCore.Components.InjectAttribute`, which the Blazor renderer
handles. That attribute is **different** from
`OutWit.Common.DependencyInjection.InjectAttribute` and runs on a different
mechanism — they do not interact.

If a single file needs both attributes (rare), use fully qualified names or
aliases to disambiguate. The recommended split is simple:

- **Blazor components / `*.razor`** — use Blazor's `[Inject]`. Don't add
  `using OutWit.Common.DependencyInjection;` to `_Imports.razor`.
- **Plain service classes / view models** — use this package's `[Inject]`
  (and `[InjectableHost]`).

## Diagnostics

The generator emits two diagnostics:

- **OWDI001** — `[InjectableHost]` requires a `partial` class.
- **OWDI002** — `[InjectableHost]` class declares its own constructor; either
  remove the constructor or drop the attribute.

`[Inject]` on a property with no setter (read-only / expression-bodied) is a
programmer error — `PropertyInjector` and `InjectAspect` both throw a clear
`InvalidOperationException` on the first metadata scan.

## API reference

| Type | Purpose |
|---|---|
| `InjectAttribute` | Marks a property for DI resolution. |
| `InjectScopedAttribute` | Shorthand for `[Inject(Mode = InjectMode.Scoped)]`. |
| `InjectTransientAttribute` | Shorthand for `[Inject(Mode = InjectMode.Transient)]`. |
| `InjectOptionalAttribute` | Shorthand for `[Inject(Requirement = InjectRequirement.Optional)]`. |
| `InjectRequiredAttribute` | Shorthand for `[Inject(Requirement = InjectRequirement.Required)]`. |
| `InjectMode` | `Cached` / `Transient` / `Scoped` — resolution lifetime. |
| `InjectRequirement` | `Auto` / `Required` / `Optional` — override nullability. |
| `InjectableHostAttribute` | Marks a partial class for source-generator scaffolding. |
| `IInjectable` | Mixin interface added automatically by the aspect. |
| `InjectAspect` | AspectInjector aspect (getter advice + mixin). |
| `InjectableExtensions.InitInject` | Explicit service-provider hook-up. |
| `PropertyInjector.Inject` | Eager reflection-based injection. |
| `ServiceCollectionExtensions.Add*WithInject` | Registration + eager injection. |

## License

Licensed under the Apache License, Version 2.0. See `LICENSE`.

## Attribution (optional)

If you use OutWit.Common.DependencyInjection in a product, a mention is
appreciated (but not required), for example:
"Powered by OutWit.Common.DependencyInjection (https://ratner.io/)".

## Trademark / Project name

"OutWit" and the OutWit logo are used to identify the official project by
Dmitry Ratner.

You may:
- refer to the project name in a factual way (e.g., "built with
  OutWit.Common.DependencyInjection");
- use the name to indicate compatibility (e.g., "OutWit-DI compatible").

You may not:
- use "OutWit.Common.DependencyInjection" as the name of a fork or a derived
  product in a way that implies it is the official project;
- use the OutWit logo to promote forks or derived products without permission.
