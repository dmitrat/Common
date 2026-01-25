

# OutWit.Common.Plugins

`OutWit.Common.Plugins` is a powerful and lightweight library for building extensible .NET applications. It provides a complete infrastructure for discovering, loading, and managing plugins with a strong focus on dependency resolution and lifecycle management.

The system allows for loading plugins into isolated `AssemblyLoadContext`s, which enables advanced features like unloading assemblies and hot-reloading plugins without restarting the main application.

## Key Features

* **Dynamic Plugin Discovery**: Automatically scans specified directories for plugin assemblies (`*.dll`).
* **Attribute-Based Metadata**: Uses simple attributes to define plugin manifests (`[WitPluginManifest]`)  and dependencies (`[WitPluginDependency]`).
* **Dependency Resolution**:
    * Calculates the correct plugin load order based on priorities and explicit dependencies.
    * Detects and reports missing dependencies, version mismatches, and circular dependencies.
* **Isolated Loading**: Each plugin can be loaded into its own `AssemblyLoadContext`, preventing assembly conflicts and allowing plugins to be individually unloaded.
* **DI Integration**: Designed to work seamlessly with `Microsoft.Extensions.DependencyInjection`. Plugins can register their own services and resolve dependencies from the host application or other plugins.
* **Clear Lifecycle**: Provides well-defined methods for initialization, post-initialization, and cleanup (`Initialize`, `OnInitialized`, `OnUnloading`).
* **Multi-Targeting**: Supports modern .NET versions, including .NET 6, 7, 8, and 9.

## Project Structure

The solution is divided into two main projects:

1.  **`OutWit.Common.Plugins.Abstractions`**: A lightweight package containing the interfaces (`IWitPlugin`) and attributes (`WitPluginManifestAttribute`, `WitPluginDependencyAttribute`) needed to create a plugin. Your plugin projects should reference this.
2.  **`OutWit.Common.Plugins`**: The main implementation containing the `WitPluginLoader` and all the logic for discovery, dependency resolution, and loading. Your host application should reference this.

## Getting Started

### 1. Installation

Install the necessary packages from NuGet.

For your plugin projects:
```bash
dotnet add package OutWit.Common.Plugins.Abstractions
````

For your host application:

Bash

```bash
dotnet add package OutWit.Common.Plugins
```

### 2. Creating a Plugin

First, define a class that implements the

`IWitPlugin` interface (or inherits from the convenient `WitPluginBase` class ). Then, decorate it with the mandatory `[WitPluginManifest]` attribute.

```csharp
// In MyAwesomePlugin.csproj
// <ProjectReference Include="..\OutWit.Common.Plugins.Abstractions.csproj" />

using OutWit.Common.Plugins.Abstractions;
using OutWit.Common.Plugins.Abstractions.Attributes;
using Microsoft.Extensions.DependencyInjection;
using System;

[WitPluginManifest("MyAwesomePlugin", Version = "1.1.0", Priority = 100)]
[WitPluginDependency("AnotherPlugin", MinimumVersion = "2.0.0")]
public class MyAwesomePlugin : WitPluginBase
{
    // 1. Called first. Use this to register your services.
    public override void Initialize(IServiceCollection services)
    {
        Console.WriteLine("MyAwesomePlugin: Initializing and registering services...");
        services.AddSingleton<MyAwesomeService>();
    }

    // 2. Called after all plugins have been initialized and services are available.
    public override void OnInitialized(IServiceProvider serviceProvider)
    {
        Console.WriteLine("MyAwesomePlugin: All plugins are loaded. Resolving services.");
        var myService = serviceProvider.GetRequiredService<MyAwesomeService>();
        myService.DoWork();
    }

    // 3. Called just before the plugin is unloaded.
    public override void OnUnloading()
    {
        Console.WriteLine("MyAwesomePlugin: Unloading. Time for cleanup!");
    }
}

// A sample service provided by this plugin
public class MyAwesomeService
{
    public void DoWork() => Console.WriteLine("MyAwesomeService is doing work!");
}
```

### 3. Loading Plugins in Your Application

In your host application, create an instance of the `WitPluginLoader`, load the plugins, and integrate them into your `IServiceCollection`.

```csharp
using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.Plugins;
using OutWit.Common.Plugins.Abstractions.Interfaces;
using System;
using System.IO;

public class Program
{
    public static void Main(string[] args)
    {
        // Setup a directory for plugins
        string pluginPath = Path.Combine(AppContext.BaseDirectory, "plugins");
        Directory.CreateDirectory(pluginPath);
        // (Ensure your plugin DLLs are copied to this directory)

        var services = new ServiceCollection();
        
        // 1. Initialize the plugin loader
        // UseIsolatedContext defaults to true for hot-reloading capabilities
        var loader = new WitPluginLoader<IWitPlugin>(pluginPath);

        try
        {
            // 2. Discover metadata and resolve dependency order
            loader.Load();
        }
        catch (AggregateException ex)
        {
            Console.WriteLine("Failed to load plugins:");
            foreach (var inner in ex.InnerExceptions)
            {
                Console.WriteLine($"- {inner.Message}");
            }
            return;
        }

        // 3. Let each plugin register its services
        foreach (var plugin in loader.Plugins)
        {
            plugin.Initialize(services);
        }

        // 4. Build the service provider
        var serviceProvider = services.BuildServiceProvider();

        // 5. Notify all plugins that initialization is complete
        foreach (var plugin in loader.Plugins)
        {
            plugin.OnInitialized(serviceProvider);
        }
        
        Console.WriteLine("\nApplication is running. All plugins loaded.");
        Console.WriteLine($"Loaded plugins by priority: {string.Join(", ", loader.Keys)}");

        // ... your application logic ...

        // 6. Unload a specific plugin (if using isolated contexts)
        Console.WriteLine("\nAttempting to unload 'MyAwesomePlugin'...");
        try
        {
             loader.UnloadPlugin("MyAwesomePlugin");
             Console.WriteLine("Plugin unloaded successfully.");
        }
        catch(InvalidOperationException ex)
        {
            Console.WriteLine(ex.Message);
        }

        // 7. Dispose the loader to unload all remaining plugins
        loader.Dispose();
        Console.WriteLine("\nApplication shutting down. All plugins unloaded.");
    }
}
```

## License

Licensed under the Apache License, Version 2.0. See `LICENSE`.

## Attribution (optional)

If you use OutWit.Common.Plugins in a product, a mention is appreciated (but not required), for example:
"Powered by OutWit.Common.Plugins (https://ratner.io/)".

## Trademark / Project name

"OutWit" and the OutWit logo are used to identify the official project by Dmitry Ratner.

You may:
- refer to the project name in a factual way (e.g., "built with OutWit.Common.Plugins");
- use the name to indicate compatibility (e.g., "OutWit.Common.Plugins-compatible").

You may not:
- use "OutWit.Common.Plugins" as the name of a fork or a derived product in a way that implies it is the official project;
- use the OutWit.Common.Plugins logo to promote forks or derived products without permission.