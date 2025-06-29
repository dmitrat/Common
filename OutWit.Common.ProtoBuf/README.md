# OutWit.Common.ProtoBuf

A .NET library that provides a set of utilities and wrapper classes to simplify serialization tasks using [protobuf-net](https://github.com/protobuf-net/protobuf-net). This package offers convenient extension methods, pre-configured surrogates for common types, and serializable collection classes.

## Features

-   **Simplified Serialization**: Extension methods (`ToProtoBytes`, `FromProtoBytes`) to easily serialize objects to byte arrays and deserialize them back.
-   **Deep Cloning**: A convenient `ProtoClone` extension method that uses serialization to perform a deep copy of an object.
-   **File I/O**: Helpers to export and load collections directly to/from files (`ExportAsProtoBuf`, `LoadAsProtoBuf`).
-   **Built-in Surrogates**: Out-of-the-box serialization support for `DateTimeOffset` and `PropertyChangedEventArgs`.
-   **Easy Configuration**: A simple registration pattern to add your own custom surrogates or subtypes.

## Installation

This library is intended to be used as a NuGet package. You can add it to your project using the .NET CLI:

```bash
dotnet add package OutWit.Common.ProtoBuf
```

## Usage

### Basic Serialization and Deserialization

The library extends any object with `ToProtoBytes()` and `FromProtoBytes<T>()` methods.

```csharp
// Assuming you have a class marked with ProtoContract
[ProtoContract]
public class Person
{
    [ProtoMember(1)]
    public string Name { get; set; }

    [ProtoMember(2)]
    public int Age { get; set; }
}

// --- Usage ---
var originalPerson = new Person { Name = "John Doe", Age = 30 };

// Serialize the object to a byte array
byte[] data = originalPerson.ToProtoBytes();

// Deserialize the byte array back to an object
var deserializedPerson = data.FromProtoBytes<Person>();

Console.WriteLine($"Deserialized Person: {deserializedPerson.Name}, Age: {deserializedPerson.Age}");
```

### Cloning Objects

Perform a deep clone on any serializable object.

```csharp
var originalPerson = new Person { Name = "Jane Doe", Age = 25 };

// Create a deep clone
var clonedPerson = originalPerson.ProtoClone();

// 'clonedPerson' is a new instance with the same data
// 'originalPerson != clonedPerson' will be true
```

### Registering Custom Surrogates

If you need to serialize a type that you cannot directly annotate with `[ProtoContract]` (e.g., a type from a third-party library), you can register a surrogate.

```csharp
// The type you want to serialize
public class ThirdPartyPoint
{
    public int X { get; set; }
    public int Y { get; set; }
}

// A surrogate with a compatible data structure
[ProtoContract]
public class PointSurrogate
{
    [ProtoMember(1)]
    public int X { get; set; }
    [ProtoMember(2)]
    public int Y { get; set; }

    // Conversion operators
    public static implicit operator ThirdPartyPoint(PointSurrogate surrogate)
    {
        return new ThirdPartyPoint { X = surrogate.X, Y = surrogate.Y };
    }

    public static implicit operator PointSurrogate(ThirdPartyPoint original)
    {
        return new PointSurrogate { X = original.X, Y = original.Y };
    }
}

// --- Registration (at application startup) ---
ProtoBufUtils.Register(options =>
{
    options.RegisterSurrogate<ThirdPartyPoint, PointSurrogate>();
});

// Now you can serialize/deserialize ThirdPartyPoint objects
var point = new ThirdPartyPoint { X = 10, Y = 20 };
byte[] data = point.ToProtoBytes();
var newPoint = data.FromProtoBytes<ThirdPartyPoint>();
```
