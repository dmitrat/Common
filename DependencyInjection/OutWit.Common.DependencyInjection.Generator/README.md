# OutWit.Common.DependencyInjection.Generator

Roslyn source generator that pairs with `OutWit.Common.DependencyInjection`.

For any `partial class` marked `[InjectableHost]`, this generator emits the
`IServiceProvider` constructor and the matching private `Services` property
required by `InjectAspect`'s field auto-discovery — so the host class only
declares its `[Inject]` properties.

The generator is bundled into the `OutWit.Common.DependencyInjection` package
as an analyzer. You normally do **not** install this package directly; it is
pulled in transitively. Use `OutWit.Common.DependencyInjection` instead.

For full documentation see the
[OutWit.Common.DependencyInjection README](https://github.com/dmitrat/Common/tree/main/DependencyInjection/OutWit.Common.DependencyInjection).

## License

Licensed under the Apache License, Version 2.0. See `LICENSE`.
