# OutWit.Common.NUnit

`OutWit.Common.NUnit` is a companion library to `OutWit.Common` that enhances the NUnit testing experience. It introduces a fluent, expressive assertion syntax for verifying the semantic equality of objects inheriting from `ModelBase`.

## 💡 The Problem

When using the `OutWit.Common` library, your data models often inherit from `ModelBase`, which provides a powerful `.Is()` method for deep, property-by-property equality checks.

A standard NUnit test to verify this might look like this:

```csharp
// Standard NUnit assertion
Assert.That(actualModel.Is(expectedModel), Is.True);
````

While this works, it's not as readable and doesn't fully leverage NUnit's constraint-based model.

## ✨ The Solution

This library introduces the

`Was` class, a static helper that provides custom constraints for a more natural, fluent assertion syntax.

With `OutWit.Common.NUnit`, the same test becomes:

C#

```csharp
// Fluent assertion with OutWit.Common.NUnit
Assert.That(actualModel, Was.EqualTo(expectedModel));
```

This approach is more idiomatic to NUnit and clearly expresses the intent of the test.

## Features

- **Fluent Assertions**: Provides a static `Was` class for expressive and readable tests.
    
- **Deep Equality Testing**: The `Was.EqualTo()` constraint is specifically designed to use the `.Is()` method from `ModelBase` for semantic object comparison.
    
- **Negation Support**: Easily assert inequality with `Was.Not.EqualTo()`.
    
- **Seamless NUnit Integration**: Plugs directly into NUnit's `Assert.That()` syntax as a custom constraint.
    

## Getting Started

### 1. Installation

Install the package from NuGet into your test project.

Bash

```
dotnet add package OutWit.Common.NUnit
```

Your test project must also have references to

`NUnit` and `OutWit.Common`.

### 2. Usage

Simply import the namespace and use the `Was` class in your NUnit `Assert.That` calls.

#### Example

Assume you have a model inheriting from `ModelBase`:

C#

```csharp
// Your model in the main project
public class MyModel : ModelBase
{
    public int Id { get; set; }
    public string Name { get; set; }

    public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
    {
        if (modelBase is not MyModel other)
            return false;
        
        return this.Id == other.Id && this.Name == other.Name;
    }
    
    // Other methods (Clone, etc.)
}
```

Your NUnit tests can now use `Was` for cleaner assertions:

C#

```csharp
using NUnit.Framework;
using OutWit.Common.NUnit; // Import the library

[TestFixture]
public class MyModelTests
{
    [Test]
    public void TwoModelsWithSameValues_ShouldBeEqual()
    {
        // Arrange
        var model1 = new MyModel { Id = 1, Name = "Test" };
        var model2 = new MyModel { Id = 1, Name = "Test" };

        // Act & Assert
        Assert.That(model1, Was.EqualTo(model2));
    }

    [Test]
    public void TwoModelsWithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var model1 = new MyModel { Id = 1, Name = "Test" };
        var model2 = new MyModel { Id = 2, Name = "Test" };

        // Act & Assert
        Assert.That(model1, Was.Not.EqualTo(model2));
    }
}
```

The `Was` class also provides convenient shortcuts to standard NUnit constraints like `Null`, `True`, `False`, and `Empty`.

## License

Licensed under the Apache License, Version 2.0. See `LICENSE`.

## Attribution (optional)

If you use OutWit.Common.NUnit in a product, a mention is appreciated (but not required), for example:
"Powered by OutWit.Common.NUnit (https://ratner.io/)".

## Trademark / Project name

"OutWit" and the OutWit logo are used to identify the official project by Dmitry Ratner.

You may:
- refer to the project name in a factual way (e.g., "built with OutWit.Common.NUnit");
- use the name to indicate compatibility (e.g., "OutWit.Common.NUnit-compatible").

You may not:
- use "OutWit.Common.NUnit" as the name of a fork or a derived product in a way that implies it is the official project;
- use the OutWit.Common.NUnit logo to promote forks or derived products without permission.
