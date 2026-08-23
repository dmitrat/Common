# [OutWit.Common](https://github.com/dmitrat/Common/tree/main/OutWit.Common)

A zero-dependency foundational library for.NET that accelerates robust application development. Key features include an intelligent `ModelBase` for non-intrusive value comparison (`Is()`), immutable updates (`With()`), and a declarative `ToString()` via attributes.

# [OutWit.Common.Aspects](https://github.com/dmitrat/Common/tree/main/OutWit.Common.Aspects)

A lightweight, runtime AOP library for automating `INotifyPropertyChanged` notifications. Keeps your build process simple and your code clean, using a high-performance, reflection-caching mechanism for fast runtime notifications.

# [OutWit.Common.Logging](https://github.com/dmitrat/Common/tree/main/OutWit.Common.Logging)

A logging library for MVVM using Serilog and AOP. Features include declarative `[Log]` and `[Measure]` attributes, a UI-bindable logger for real-time log display, and intelligent filtering for `INotifyPropertyChanged` events.

# [OutWit.Common.CommandLine](https://github.com/dmitrat/Common/tree/main/OutWit.Common.CommandLine)

A lightweight extension for the popular CommandLineParser library. It adds the missing piece: serializing a C# options object back into a command-line argument string.

# [OutWit.Common.Rest](https://github.com/dmitrat/Common/tree/main/OutWit.Common.Rest)

A set of utilities to streamline REST API interactions. Features a fluent client for easy configuration of requests with bearer tokens and custom headers, and a flexible query builder for converting C# types into URL parameters.

# [OutWit.Common.Reflection](https://github.com/dmitrat/Common/tree/main/OutWit.Common.Reflection)

A lightweight .NET reflection library designed to simplify complex tasks. Includes extension methods to recursively discover all events and methods from a type's complete hierarchy, including base classes and interfaces.

The key feature is a powerful utility to create a single, universal event handler that can subscribe to any event, regardless of its delegate signature, using dynamic method generation. Ideal for logging, tracing, or dynamic proxy scenarios.

# [OutWit.Common.Proxy](https://github.com/dmitrat/Common/tree/main/OutWit.Common.Proxy)

Provides the core interfaces (`IProxyInterceptor`, `IProxyInvocation`), models, and attributes (`ProxyTargetAttribute`) for creating and using compile-time proxies with the `OutWit.Common.Proxy.Generator`. This package is required at runtime to implement the interception logic.

# [OutWit.Common.Proxy.Generator](https://github.com/dmitrat/Common/tree/main/OutWit.Common.Proxy.Generator)

A C# Source Generator that automatically creates proxy classes for interfaces decorated with `[ProxyTargetAttribute]`. This generator enables compile-time interception of methods, properties, and events, eliminating runtime reflection for proxy creation.

# [OutWit.Common.Json](https://github.com/dmitrat/Common/tree/main/OutWit.Common.Json)

Json (System.Text.Json) serialization tools/snippets. Provides intuitive extension methods for serialization, deserialization, deep cloning, and file I/O operations. Includes built-in support for source generation to maximize performance , along with custom converters for `System.Type` and `System.Security.Cryptography.RSAParameters`.

# [OutWit.Common.MemoryPack](https://github.com/dmitrat/Common/tree/main/OutWit.Common.MemoryPack)

A helper library that simplifies and accelerates development with the MemoryPack serializer. Provides convenient extension methods for serialization, deserialization, cloning, and file I/O. Includes a built-in formatter for `PropertyChangedEventArgs`.

# [OutWit.Common.MessagePack](https://github.com/dmitrat/Common/tree/main/OutWit.Common.MessagePack)

A collection of helpers and extension methods for MessagePack-CSharp to simplify serialization, deserialization, and object cloning. Provides built-in LZ4 compression support.

# [OutWit.Common.ProtoBuf](https://github.com/dmitrat/Common/tree/main/OutWit.Common.ProtoBuf)

A collection of tools and helper classes to simplify serialization with protobuf-net. Provides extension methods for serialization, deserialization, and deep cloning, plus built-in support for common types like `DateTimeOffset`.

# [OutWit.Common.MVVM](https://github.com/dmitrat/Common/tree/main/OutWit.Common.MVVM)

A collection of essential helpers and components for WPF and the MVVM pattern. Includes a `ViewModelBase`, `DelegateCommand`, a thread-safe `SafeObservableCollection`, `SortedCollection`, powerful binding utilities, and an AOP `[Bindable]` attribute to easily create DependencyProperties.

# [OutWit.Common.MVVM.Navigation](https://github.com/dmitrat/Common/tree/main/MVVM/OutWit.Common.MVVM.Navigation)

ViewModel-first navigation for desktop applications — the regions, navigation and dialog part of Prism, without the container, the statics or the region adapters. Named **outlets** with a journal and guards; **zones** where modules contribute navigation bar entries and menu items; typed **dialogs**; start-up validation that reports a route with no view before a user finds it by clicking. View models are created through `ActivatorUtilities`, `Transient` routes get their own DI scope, and a screen that loads slowly never blocks the next navigation.

# [OutWit.Common.MVVM.Navigation.Avalonia](https://github.com/dmitrat/Common/tree/main/MVVM/OutWit.Common.MVVM.Navigation.Avalonia)

The Avalonia half: a `ViewLocator` that pairs view models with views through an explicit registry or a naming convention, a `NavigationOutlet` control that keeps a cached screen's view — and its scroll position — alive across navigations, and dialog hosts over modal windows or the overlay layer, with no external dependency.

# [OutWit.Common.MVVM.Navigation.WPF](https://github.com/dmitrat/Common/tree/main/MVVM/OutWit.Common.MVVM.Navigation.WPF)

The same for WPF, so a WPF application can drop Prism and Unity without leaving WPF: the locator doubles as a `DataTemplateSelector`, `ViewPresenter` shows the view of any view model, and dialogs are modal windows whose close button goes through the dialog's own `CanCloseAsync`.

# [OutWit.Common.MVVM.Navigation.Modules](https://github.com/dmitrat/Common/tree/main/MVVM/OutWit.Common.MVVM.Navigation.Modules)

UI modules over `OutWit.Common.Plugins`: a plugin that registers services, routes, views and contributions, loaded from a folder next to the application or compiled in. Same two-phase shape as every other OutWit plugin axis; a module that throws is recorded and logged while the rest carry on.

# [OutWit.Common.NUnit](https://github.com/dmitrat/Common/tree/main/OutWit.Common.NUnit)

An extension library for NUnit that provides fluent assertion helpers `(Assert.That(actual, Was.EqualTo(expected)))` for testing custom objects inheriting from `ModelBase` in the `OutWit.Common` framework. It simplifies semantic equality checks by integrating the `ModelBase.Is()` method directly into the NUnit constraint model for more readable and expressive tests.

# [OutWit.Common.Plugins.Abstractions](https://github.com/dmitrat/Common/tree/main/OutWit.Common.Plugins.Abstractions)

A lightweight set of abstractions for creating plugins for the `OutWit.Common.Plugins` framework. This package includes the core `IWitPlugin` interface, the `WitPluginBase` convenience class, and attributes (`WitPluginManifest`, `WitPluginDependency`) for defining plugin metadata and dependencies. Your plugin projects should reference this package.

# [OutWit.Common.Plugins](https://github.com/dmitrat/Common/tree/main/OutWit.Common.Plugins)

A robust and flexible plugin system for .NET. Features dynamic discovery from directories, sophisticated dependency resolution (validates versions and detects circular dependencies), and isolated loading via `AssemblyLoadContext` to enable hot-reloading and unloading of plugins. It integrates seamlessly with `Microsoft.Extensions.DependencyInjection` for a modern, decoupled architecture.

## License

Licensed under the Apache License, Version 2.0. See `LICENSE`.

## Attribution (optional)

If you use OutWit.Common in a product, a mention is appreciated (but not required), for example:
"Powered by OutWit.Common (https://ratner.io/)".

## Trademark / Project name

"OutWit" and the OutWit logo are used to identify the official project by Dmitry Ratner.

You may:

- refer to the project name in a factual way (e.g., "built with OutWit.Common");
- use the name to indicate compatibility (e.g., "OutWit.Common-compatible").

You may not:

- use "OutWit.Common" as the name of a fork or a derived product in a way that implies it is the official project;
- use the OutWit.Common logo to promote forks or derived products without permission.