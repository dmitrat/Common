# OutWit.Common.MemoryPack

**OutWit.Common.MemoryPack** is a .NET library that provides a collection of helper methods and pre-configured, serializable types to simplify and accelerate development with the [MemoryPack](https://github.com/Cysharp/MemoryPack) serialization library. It offers extension methods for common tasks, custom formatters, and a suite of ready-to-use data structures for various domain needs.

## Key Features

* **Fluent Extension Methods**: Simplifies serializing, deserializing, and cloning objects.
* **File I/O Helpers**: Easily export and load collections of objects to and from files, both synchronously and asynchronously.
* **Custom Formatter for `PropertyChangedEventArgs`**: Includes a built-in formatter for `System.ComponentModel.PropertyChangedEventArgs`, which is automatically registered and essential for MVVM applications.
* **Serializable-Ready Types**: A collection of `[MemoryPackable]` types designed to be used directly in your data models:
    * **Collections**: `MemoryPackMap<TKey, TValue>` and `MemoryPackSet<TValue>` for serializable, read-only collections.
    * **Messaging**: `MemoryPackMessage` and `MemoryPackMessageWith<TData>` for creating standardized DTOs.
    * **Domain Primitives**: Includes `MemoryPackRange<TValue>`, `MemoryPackRangeSet<TValue>`, and `MemoryPackValueInSet<TValue>` for representing common data constraints.
* **Easy Configuration**: A simple registration pattern to add your own custom formatters. 
* **Cross-Platform Support**: Targets .NET 6, 7, 8, and 9.

## Installation

Install the package from NuGet using the .NET CLI:

```bash
dotnet add package OutWit.Common.MemoryPack
```

## Usage

### Basic Serialization and Deserialization

The `MemoryPackUtils` class provides convenient extension methods.

```csharp
using OutWit.Common.MemoryPack;
using MemoryPack;

[MemoryPackable]
public partial class User
{
    public string Name { get; set; }
    public int Age { get; set; }
}

var user = new User { Name = "John Doe", Age = 30 };

// Serialize the object to bytes
byte[] bytes = user.ToMemoryPackBytes();

// Deserialize the bytes back to an object
User deserializedUser = bytes.FromMemoryPackBytes<User>();
```

### Cloning an Object

You can easily create a deep clone of any `[MemoryPackable]` object.

```csharp
// Clone the user object
[cite_start]User clonedUser = user.MemoryPackClone();
```

### Exporting and Loading Collections

Save a collection to a file and load it back.

```csharp
using OutWit.Common.MemoryPack.Ranges;

var ranges = new List<MemoryPackRange<int>>
{
    new(1, 10),
    new(20, 30)
};

// Export the list to a file
await ranges.ExportAsMemoryPackAsync("ranges.bin"); 

// Load the list from the file
IReadOnlyList<MemoryPackRange<int>> loadedRanges = await MemoryPackUtils.LoadAsMemoryPackAsync<MemoryPackRange<int>>("ranges.bin");
```

### Registering a Custom Formatter

You can register your own custom `MemoryPackFormatter` during application startup.

```csharp
// Your custom formatter
public class MyCustomTypeFormatter : MemoryPackFormatter<MyCustomType>
{
    // ... implementation
}

// Register it
MemoryPackUtils.Register(options => 
{
    options.Register(new MyCustomTypeFormatter());
});
```
