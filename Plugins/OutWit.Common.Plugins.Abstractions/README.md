# OutWit.Common.Plugins.Abstractions

This package contains the minimal set of shared interfaces, base classes, and attributes required to build plugins compatible with the `OutWit.Common.Plugins` system. It is intentionally lightweight to ensure your plugins do not depend on the main loader logic.

## Key Components

* **`IWitPlugin`**: The core interface that every plugin must implement. It defines the plugin lifecycle with methods for initialization, post-initialization, and unloading. 
* **`WitPluginBase`**: An abstract base class that provides a default empty implementation of `IWitPlugin` for convenience. You can override only the methods you need.
* **`[WitPluginManifestAttribute]`**: A mandatory attribute for any plugin class. It defines essential metadata like the plugin's unique **name**, **version**, and load **priority**, making it discoverable by the plugin loader. 
* **`[WitPluginDependencyAttribute]`**: An optional attribute that can be used multiple times to declare dependencies on other plugins. It allows the loader to validate that required plugins are present before loading. 

## Installation

Add the package to your plugin's class library project using NuGet.

```bash
dotnet add package OutWit.Common.Plugins.Abstractions
````

## How to Create a Plugin

1. Create a new class library project.    
2. Add a reference to `OutWit.Common.Plugins.Abstractions`.    
3. Create a class that inherits from `WitPluginBase` (or directly implements `IWitPlugin`).    
4. Add the `[WitPluginManifestAttribute]` to your class, specifying a unique name.    
5. If needed, add `[WitPluginDependencyAttribute]` attributes to declare dependencies.    
6. Implement the `Initialize` and `OnInitialized` methods to register services and execute logic.    

### Example

C#

```csharp
using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.Plugins.Abstractions;
using OutWit.Common.Plugins.Abstractions.Attributes;
using OutWit.Common.Plugins.Abstractions.Interfaces;
using System;

[WitPluginManifest("MyFirstPlugin", Version = "1.0.0", Priority = 100)]
[WitPluginDependency("AnotherPlugin", MinimumVersion = "2.1.0")]
public class MyFirstPlugin : WitPluginBase
{
    // Use for registering services in DI.
    public override void Initialize(IServiceCollection services)
    {
        // Register services in the DI container
        services.AddSingleton<MyService>();
    }

    // Called after all plugins have been initialized.
    public override void OnInitialized(IServiceProvider serviceProvider)
    {
        // Logic that runs after all plugins are loaded
        var myService = serviceProvider.GetRequiredService<MyService>();
        myService.Run();
    }

    // Called just before the plugin is about to be unloaded.
    public override void OnUnloading()
    {
        // Perform cleanup here
    }
}
```

## License

Licensed under the Apache License, Version 2.0. See `LICENSE`.

## Attribution (optional)

If you use OutWit.Common.Plugins.Abstractions in a product, a mention is appreciated (but not required), for example:
"Powered by OutWit.Common.Plugins.Abstractions (https://ratner.io/)".

## Trademark / Project name

"OutWit" and the OutWit logo are used to identify the official project by Dmitry Ratner.

You may:
- refer to the project name in a factual way (e.g., "built with OutWit.Common.Plugins.Abstractions");
- use the name to indicate compatibility (e.g., "OutWit.Common.Plugins.Abstractions-compatible").

You may not:
- use "OutWit.Common.Plugins.Abstractions" as the name of a fork or a derived product in a way that implies it is the official project;
- use the OutWit.Common.Plugins.Abstractions logo to promote forks or derived products without permission.