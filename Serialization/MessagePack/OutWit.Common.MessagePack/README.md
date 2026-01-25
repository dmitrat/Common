# OutWit.Common.MessagePack

A utility library for the [MessagePack-CSharp](https://github.com/neuecc/MessagePack-CSharp) serializer. It simplifies common serialization tasks, provides helpful extension methods, and includes a set of pre-configured, serializable data structures for common use cases.

## Features
* **Simple Serialization/Deserialization**: Extension methods (`ToMessagePackBytes`, `FromMessagePackBytes`) to serialize and deserialize any object with a single line of code.
* **Built-in Compression**: Easily enable or disable LZ4 compression via a boolean flag.
* **Deep Cloning**: A convenient `.MessagePackClone()` extension method to perform a deep copy of any serializable object.
* **Centralized Configuration**: A simple static class `MessagePackUtils` for registering custom formatters and resolvers globally.
* **Custom Formatters Included**:
  * `TypeFormatter`: Serializes `System.Type` objects.
  * `PropertyChangedEventArgsFormatter`: Serializes `System.ComponentModel.PropertyChangedEventArgs`.
* **File I/O Helpers**: Methods to easily export and load collections to/from a MessagePack file.

## Installation

Install the package via the .NET CLI:

```bash

dotnet add package OutWit.Common.MessagePack

```

## Quick Start
The library is configured with sensible defaults and is ready to use immediately after installation.

### Basic Serialization and Deserialization

The `MessagePackUtils` class provides extension methods for any object.

```csharp
using OutWit.Common.MessagePack;

public class MyData
{
    public int Id { get; set; }
    public string Name { get; set; }
}

var myObject = new MyData { Id = 1, Name = "Test" };

// Serialize the object to a byte array (with LZ4 compression by default)
byte[] msgPackBytes = myObject.ToMessagePackBytes(); 

// Deserialize it back
MyData deserializedObject = msgPackBytes.FromMessagePackBytes<MyData>();
```

### Disabling Compression

To serialize without compression, pass `false` to the `withCompression` parameter.

```csharp
// Serialize without compression
byte[] plainBytes = myObject.ToMessagePackBytes(withCompression: false); 

// Deserialize without compression
MyData deserializedObject = plainBytes.FromMessagePackBytes<MyData>(withCompression: false);
```

### Cloning an Object

You can create a deep clone of any serializable object using the `MessagePackClone` extension method.

```csharp
var originalObject = new MyData { Id = 10, Name = "Original" };
var clonedObject = originalObject.MessagePackClone(); 

// clonedObject is a new instance with the same values
// but is not the same reference as originalObject.
```

### Registering a Custom Formatter

The library uses a central resolver. You can easily register your own formatters at application startup.

```csharp
using OutWit.Common.MessagePack;
using MessagePack;
using MessagePack.Formatters;

// 1. Define your custom formatter
public class MyCustomObjectFormatter : IMessagePackFormatter<MyCustomObject>
{
    public void Serialize(ref MessagePackWriter writer, MyCustomObject value, MessagePackSerializerOptions options)
    {
        // ... serialization logic
    }

    public MyCustomObject Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        // ... deserialization logic
    }
}

// 2. Register it at startup
public static class Program
{
    public static void Main(string[] args)
    {
        // Register the formatter for MyCustomObject
        MessagePackUtils.Register<MyCustomObjectFormatter>();

        // ... rest of your application
    }
}
```

## License

Licensed under the Apache License, Version 2.0. See `LICENSE`.

## Attribution (optional)

If you use OutWit.Common.MessagePack in a product, a mention is appreciated (but not required), for example:
"Powered by OutWit.Common.MessagePack (https://ratner.io/)".

## Trademark / Project name

"OutWit" and the OutWit logo are used to identify the official project by Dmitry Ratner.

You may:
- refer to the project name in a factual way (e.g., "built with OutWit.Common.MessagePack");
- use the name to indicate compatibility (e.g., "OutWit.Common.MessagePack-compatible").

You may not:
- use "OutWit.Common.MessagePack" as the name of a fork or a derived product in a way that implies it is the official project;
- use the OutWit.Common.MessagePack logo to promote forks or derived products without permission.
