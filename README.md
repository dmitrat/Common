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
