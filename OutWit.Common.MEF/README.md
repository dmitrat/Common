
# OutWit.Common.MEF

`OutWit.Common.MEF` is a helper library that provides a seamless bridge between the **`OutWit.Common`** ecosystem and Microsoft's **Managed Extensibility Framework (MEF)**.

This project allows you to leverage MEF's powerful composition and extensibility features, such as attribute-based dependency injection (`[Import]`, `[Export]`), with components and models built using the `OutWit.Common` library.

## ✨ Key Features

* **Seamless Integration**: Connects `OutWit.Common` components to the MEF composition container.
* **Promote Extensibility**: Simplifies the creation of modular, plugin-based applications where different modules can discover and interact with each other.
* **Simplified DI**: Use MEF to automatically satisfy dependencies for your `OutWit.Common` services and models.
* **Modern .NET Support**: Multi-targeted for .NET 6, 7, 8, and 9. 

## 🚀 Getting Started

### 1. Installation

Install the package from NuGet into your host application or relevant modules.

```bash
dotnet add package OutWit.Common.MEF
````

### 2. Dependencies

This library relies on two main packages:

- **`OutWit.Common`**: The core library providing base models and services.
    
- **`System.ComponentModel.Composition`**: The official package for the Managed Extensibility Framework (MEF).
    

### 3. Conceptual Usage

The primary goal is to allow parts of your application to be composed at runtime. For example, you can `[Export]` a service in one module and `[Import]` it in another without a direct compile-time reference.

#### Example: Exporting a Service

In one of your modules, you might define a service and export it using MEF's `[Export]` attribute.

C#

```csharp
// In MyServiceModule.csproj
// <PackageReference Include="OutWit.Common.MEF" />

using System.ComponentModel.Composition;
using OutWit.Common.Services; // Assuming a base interface from OutWit.Common

[Export(typeof(IMyService))]
public class MyServiceImpl : IMyService
{
    public void PerformAction(string data)
    {
        // ... implementation ...
    }
}
```

#### Example: Importing and Using a Service

In your main application or another module, you can compose the parts and import the service where needed.

```csharp
// In MainApp.csproj
// <PackageReference Include="OutWit.Common.MEF" />

using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using OutWit.Common.Services;

public class MainViewModel
{
    [Import]
    private IMyService _myService; // MEF will inject this

    public MainViewModel()
    {
        Compose();
    }

    private void Compose()
    {
        // Create a catalog of parts from your application's assemblies
        var catalog = new AggregateCatalog();
        catalog.Catalogs.Add(new AssemblyCatalog(typeof(MainViewModel).Assembly));
        // Add other assemblies or directories containing parts
        // catalog.Catalogs.Add(new DirectoryCatalog("./modules"));
        
        var container = new CompositionContainer(catalog);
        container.ComposeParts(this);
    }
    
    public void DoSomething()
    {
        // _myService is now a valid instance of MyServiceImpl
        _myService.PerformAction("Hello MEF!");
    }
}
```

This library provides the necessary glue and potentially custom MEF catalogs or helpers to make this integration with `OutWit.Common` components straightforward.