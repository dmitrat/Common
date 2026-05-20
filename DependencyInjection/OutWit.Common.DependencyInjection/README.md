# OutWit.Common.DependencyInjection

Property-based dependency injection for .NET using the `[Inject]` attribute.
Replaces large constructor parameter lists with declarative, lazily-resolved
properties powered by AspectInjector IL weaving — works on top of the standard
`Microsoft.Extensions.DependencyInjection` container with no special host.

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

    [Inject] public ILogger<ClientPool> Logger { get; private set; } = null!;
    [Inject] public ConfigurationService Configuration { get; private set; } = null!;
    [Inject] public ClientStore Store { get; private set; } = null!;
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

    [Inject] public ILogger<MyService> Logger { get; private set; } = null!;
    [Inject] public IRepository? Repository { get; private set; }
}
```

```csharp
services.AddSingleton<MyService>();
```

That is the whole setup. Properties resolve lazily on first access; no special
registration helper is needed for the common case.

## Required vs optional

By default the property's nullability decides whether the service is required
or optional:

| Declaration | Behavior |
|---|---|
| `IFoo Foo { get; private set; } = null!;` | Required — throws if not registered |
| `IFoo? Foo { get; private set; }` | Optional — returns `null` if not registered |

Override with the `Requirement` property:

```csharp
// Force optional even though the property is non-nullable.
[Inject(Requirement = InjectRequirement.Optional)]
public IFoo Foo { get; private set; } = null!;

// Force required even though the property is nullable.
[Inject(Requirement = InjectRequirement.Required)]
public IFoo? Foo { get; private set; }
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
    public IDbContext DbContext { get; private set; } = null!;
}
```

```csharp
public class MyWorker : IDisposable
{
    private readonly IServiceProvider m_serviceProvider;

    public MyWorker(IServiceProvider serviceProvider)
    {
        m_serviceProvider = serviceProvider;
    }

    [Inject(Mode = InjectMode.Scoped)]
    public IWorkContext Work { get; private set; } = null!;

    public void Dispose() { /* aspect disposes the scope before this body runs */ }
}
```

## ServiceProvider discovery

The aspect finds `IServiceProvider` in this order:

1. **Field auto-discovery** — store `IServiceProvider` in any instance field.
   The aspect scans every field (including private and inherited).

   ```csharp
   public class MyService
   {
       private readonly IServiceProvider m_serviceProvider;

       public MyService(IServiceProvider serviceProvider)
       {
           m_serviceProvider = serviceProvider;
       }

       [Inject] public IFoo Foo { get; private set; } = null!;
   }
   ```

2. **`InitInject` fallback** — call `this.InitInject(sp)` when the class does
   not store the provider in a field.

   ```csharp
   public class MyService
   {
       public MyService(IServiceProvider serviceProvider)
       {
           this.InitInject(serviceProvider);
       }

       [Inject] public IFoo Foo { get; private set; } = null!;
   }
   ```

3. **Direct mixin assignment** — `((IInjectable)instance).ServiceProvider = sp;`
   This is what the `Add*WithInject` helpers do under the hood.

## Eager injection — `Add*WithInject`

For the rare case where you need every `[Inject]` property resolved at
creation time (not on first access), use the registration helpers:

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

## Inheritance

`[Inject]` works across the type hierarchy. Properties declared in base classes
are resolved alongside properties declared in derived classes:

```csharp
public class BaseService
{
    [Inject] public ILogger Logger { get; private set; } = null!;
}

public class DerivedService : BaseService
{
    private readonly IServiceProvider m_serviceProvider;

    public DerivedService(IServiceProvider serviceProvider)
    {
        m_serviceProvider = serviceProvider;
    }

    [Inject] public IRepository? Repository { get; private set; }
}
```

## Diagnostics

`[Inject]` on a property without a setter (read-only / expression-bodied) is a
programmer error — `PropertyInjector` and `InjectAspect` both throw a clear
`InvalidOperationException` on the first metadata scan.

## API reference

| Type | Purpose |
|---|---|
| `InjectAttribute` | Marks a property for DI resolution. |
| `InjectMode` | `Cached` / `Transient` / `Scoped` — resolution lifetime. |
| `InjectRequirement` | `Auto` / `Required` / `Optional` — override nullability. |
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
