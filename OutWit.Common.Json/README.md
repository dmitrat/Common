# OutWit.Common.Json

This library enhances `System.Text.Json` by providing a set of intuitive extension methods and a powerful, performance-oriented architecture.

## Key Features

* **Fluent Extension Methods**: Simplifies serialization and deserialization to and from strings and byte arrays.
* **Source Generation Ready**: Boost performance and reduce memory allocation by using `System.Text.Json` source generation. The library provides a mechanism to register your own `JsonSerializerContext`.
* **Merged Context Resolver**: Combines multiple `JsonSerializerContext` instances, falling back to reflection if a type is not found in any registered context. This gives you the performance of source generation with the flexibility of reflection.
* **Built-in Default Context**: Comes with a pre-configured context for a wide range of primitive types, arrays, and lists, so it works great out of the box.
* **Custom Converters Included**:
    * `Type`: Serializes `System.Type` objects to their assembly-qualified names.
    * `RSAParameters`: Serializes `System.Security.Cryptography.RSAParameters` structures.
* **File I/O Helpers**: Easily export collections to JSON files or load them back, with both synchronous and asynchronous methods available.
* **Graceful Error Handling**: Methods safely handle exceptions during serialization/deserialization, returning `null` or `default` and optionally logging errors via `Microsoft.Extensions.Logging`.
* **Deep Cloning**: A simple `JsonClone()` extension method to create a deep copy of an object.

## Installation

Install the package from NuGet:

```powershell
Install-Package OutWit.Common.Json
```

Or via the .NET CLI:

```bash
dotnet add package OutWit.Common.Json
```

## Basic Usage

The library provides easy-to-use extension methods for any object.

```csharp
using OutWit.Common.Json;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
}

var user = new User { Id = 1, Name = "John Doe" };

// Serialize to a JSON string
string json = user.ToJsonString(); // {"Id":1,"Name":"John Doe"}

// Serialize to an indented JSON string
string indentedJson = user.ToJsonString(indented: true);
/*
{
  "Id": 1,
  "Name": "John Doe"
}
*/

// Deserialize from a JSON string
var deserializedUser = json.FromJsonString<User>();

// Create a deep clone of the object
var clonedUser = user.JsonClone();
```

## Advanced Usage: Using Source Generation

For optimal performance, especially in AOT (Ahead-of-Time) compiled scenarios, you can use your own source-generated `JsonSerializerContext`.

### 1. Define your data models and a `JsonSerializerContext`:

```csharp
using System.Text.Json.Serialization;

namespace MyApp.Models
{
    public class Product
    {
        public string Sku { get; set; }
        public decimal Price { get; set; }
    }

    // Define a context for your types
    [JsonSerializable(typeof(Product))]
    [JsonSerializable(typeof(Product[]))]
    internal partial class AppJsonContext : JsonSerializerContext
    {
    }
}
```

### 2. Register your context at application startup:

```csharp
using OutWit.Common.Json;
using MyApp.Models;

public static class Program
{
    public static void Main(string[] args)
    {
        // Register your context with OutWit.Common.Json
        JsonUtils.Register(new AppJsonContext());

        // Now, all serialization calls will use your generated context
        // for the Product type, falling back for other types.
        var product = new Product { Sku = "ABC-123", Price = 99.99m };
        string json = product.ToJsonString();

        System.Console.WriteLine(json);
    }
}
```

You can also use the `optionsBuilder` to register multiple contexts. 

```csharp
JsonUtils.Register(options => 
{
    options.Contexts.Add(new AppJsonContext());
    options.Contexts.Add(new AnotherContext());
});
```
