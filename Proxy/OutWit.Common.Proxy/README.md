# OutWit.Common.Proxy

`OutWit.Common.Proxy` provides foundational classes and interfaces for creating proxy objects using static code generation. The library is designed for environments where dynamic generation (e.g., `Castle DynamicProxy`) is not feasible, such as AoT compilation or Blazor.

## Features

- **Interface for creating interceptors (`IProxyInterceptor`)**:
  Allows developers to configure interception of method, property, and event calls.

- **`ProxyTargetAttribute`**:
  Used to mark interfaces that should be processed by the proxy generator.

- **`IProxyInvocation` Interface**:
  Provides details about the invocation, including method name, parameters, return values, and their types.

## Getting Started

### Installation
Add `OutWit.Common.Proxy` to your project via NuGet:
```bash
dotnet add package OutWit.Common.Proxy
```

### Usage

1. Define an interface and mark it with the `ProxyTargetAttribute`:

   ```csharp
   using OutWit.Common.Proxy;

   [ProxyTarget]
   public interface IExampleService
   {
       string GetData(int id);
       event EventHandler DataChanged;
   }
   ```

2. Implement a class that implements `IProxyInterceptor` to handle calls:

   ```csharp
   public class ExampleInterceptor : IProxyInterceptor
   {
       public void Intercept(IProxyInvocation invocation)
       {
           Console.WriteLine($"Intercepted method: {invocation.MethodName}");
           if (invocation.MethodName == "GetData")
           {
               invocation.ReturnValue = $"Data for ID {invocation.Parameters[0]}";
           }
       }
   }
   ```

3. Integrate the `OutWit.Common.Proxy.Generator` library to generate the proxy for your interface (refer to `README.md` for `OutWit.Common.Proxy.Generator`).

---

## Interfaces and Classes

- **`ProxyTargetAttribute`**:
  An attribute to mark interfaces that are processed by the proxy generator.

- **`IProxyInterceptor`**:
  Interface for creating a handler for method, property, and event calls.

- **`IProxyInvocation`**:
  Interface that describes a method/property/event invocation.

- **`ProxyInvocation`**:
  Implementation of `IProxyInvocation`.

---

## License

Licensed under the Apache License, Version 2.0. See `LICENSE`.

## Attribution (optional)

If you use OutWit.Common.Proxy in a product, a mention is appreciated (but not required), for example:
"Powered by OutWit.Common.Proxy (https://ratner.io/)".

## Trademark / Project name

"OutWit" and the OutWit logo are used to identify the official project by Dmitry Ratner.

You may:
- refer to the project name in a factual way (e.g., "built with OutWit.Common.Proxy");
- use the name to indicate compatibility (e.g., "OutWit.Common.Proxy-compatible").

You may not:
- use "OutWit.Common.Proxy" as the name of a fork or a derived product in a way that implies it is the official project;
- use the OutWit.Common.Proxy logo to promote forks or derived products without permission.

