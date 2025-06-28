# OutWit.Common

OutWit.Common is a zero-dependency, foundational utility library engineered to accelerate the development of high-quality.NET applications. 

## The Core Philosophy: Non-Intrusive Comparison and Immutable Updates

At the heart of `OutWit.Common` lies an elegant solution to a classic dilemma in object-oriented programming with.NET: the conflict between value equality and reference identity.

In standard.NET development, implementing value-based equality requires overriding the `Equals()` and `GetHashCode()` methods. While this is effective for comparing objects based on their data, it permanently alters their fundamental behavior. An object with a value-based `Equals()` implementation can no longer be reliably used as a key in collections like `Dictionary<TKey, TValue>` or `HashSet<T>`, as its hash code may change if its properties are mutable, leading to lost items and unpredictable behavior.

`OutWit.Common` "outwits" this problem by decoupling value comparison from object identity, promoting a safer, more predictable, and highly testable programming model.

### The `ModelBase` and `Is()` Solution

The cornerstone of this philosophy is the `OutWit.Common.Abstract.ModelBase` abstract class. By inheriting from `ModelBase`, developers are required to implement two key methods: `Is()` and `Clone()`.

- The `Is(ModelBase other)` method is designated for performing deep, value-based comparisons.

- The standard `Equals()` and `GetHashCode()` methods are left untouched, preserving their default reference-based behavior, which is ideal for collection management.

This separation of concerns is exposed to the developer through a rich set of extension methods in the `OutWit.Common.Values.ValueUtils` class. The primary method, `Is()`, provides a unified API for comparing any two objects. A master dispatcher method, `Check()`, intelligently routes the comparison to the most appropriate implementation: it will use `ModelBase.Is()` for derived models, `CollectionUtils.Is()` for collections, and `IComparable.CompareTo()` for primitives, all handled automatically.

This design provides the best of both worlds: robust value comparison for business logic and assertions, and stable reference identity for performance-critical collections.

### The `With()` Method for Fluent, Immutable Updates

Complementing the comparison model, `OutWit.Common` encourages an immutable-style approach to object modification through the `PropertiesUtils.With()` extension methods. Instead of directly mutating an object's state, which can lead to side effects and complex state management, the `With()` method allows for the creation of a new, modified instance in a single, fluent expression.

This is made possible by the `Clone()` method contract from `ModelBase`. When `With()` is called, it first creates a copy of the object via `Clone()`. It then applies the specified changes to the clone and returns the new instance, leaving the original object untouched.

The combination of the `Is()` and `With()` patterns enables a functional-inspired programming paradigm within C#. Domain models can be treated as immutable data records that are compared by value and "updated" by creating new versions. This approach dramatically enhances predictability, simplifies state tracking, improves thread safety, and makes unit testing significantly more robust, as assertions can be made on object states without fear of mutation.

## Getting Started

### Installation

To add `OutWit.Common` to your project, install it via the NuGet Package Manager.

```Shell

dotnet add package OutWit.Common

```

### Your First Model

Here is a minimal example of creating a domain model that leverages the core features of the library.

``` CSharp

using OutWit.Common.Abstract;
using OutWit.Common.Attributes;
using OutWit.Common.Values;

// 1. Inherit from ModelBase
public class Person : ModelBase
{
    // This property will be included in the ToString() output
    public int Id { get; set; }

    // Use a custom name in the output
    public string Name { get; set; }

    // 2. Implement the 'Is' method for value comparison
    public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
    {
        if (modelBase is not Person other)
            return false;

        // Use the ValueUtils.Is() extension for safe and correct comparison
        return this.Id.Is(other.Id) && this.Name.Is(other.Name);
    }

    // 3. Implement the 'Clone' method for immutable-style updates
    public override ModelBase Clone()
    {
        // Return a new instance with the same values
        return new Person { Id = this.Id, Name = this.Name };
    }
}

```

## Practical Usage Examples

The following examples demonstrate how to use the key patterns of `OutWit.Common`.

### Example 1: Creating and Comparing Models

Using the `Person` class defined above, you can perform robust value comparisons without affecting how the objects behave in collections.

``` CSharp
using System;
using OutWit.Common.Values; // For the 'Is' extension method

var person1 = new Person { Id = 1, Name = "John Doe" };
var person2 = new Person { Id = 1, Name = "John Doe" }; // Same values, different instance
var person3 = new Person { Id = 2, Name = "Jane Smith" };

// Use the 'Is()' method for value-based comparison
bool areEqual = person1.Is(person2);
bool areDifferent = person1.Is(person3);

Console.WriteLine($"person1.Is(person2): {areEqual}"); // Output: person1.Is(person2): True
Console.WriteLine($"person1.Is(person3): {areDifferent}"); // Output: person1.Is(person3): False

// The default Equals() method still performs reference comparison
Console.WriteLine($"person1.Equals(person2): {person1.Equals(person2)}"); // Output: person1.Equals(person2): False
```

### Example 2: Fluent, Immutable-Style Updates

The `With()` extension method provides a clean and safe way to create modified versions of your models.

```CSharp
using System;
using OutWit.Common.Utils; // For the 'With' extension method

var originalPerson = new Person { Id = 1, Name = "John Doe" };

// Create a new person with an updated name using the 'With' method
var updatedPerson = originalPerson.With(p => p.Name, "Johnathan Doe");

// The original object remains unchanged
Console.WriteLine($"Original: {originalPerson}"); // Output: Original: Id: 1, FullName: John Doe

// The new object has the updated value
Console.WriteLine($"Updated:  {updatedPerson}");  // Output: Updated:  Id: 1, FullName: Johnathan Doe
```

### Example 3: Deep Collection Comparison

CollectionUtils extends the `Is()` logic to entire collections, performing a deep, element-by-element comparison.

```CSharp
using System;
using System.Collections.Generic;
using OutWit.Common.Collections; // For the collection 'Is' extension method

var list1 = new List<Person>
{
    new Person { Id = 1, Name = "John Doe" },
    new Person { Id = 2, Name = "Jane Smith" }
};

var list2 = new List<Person>
{
    new Person { Id = 1, Name = "John Doe" },
    new Person { Id = 2, Name = "Jane Smith" }
};

// Even though the lists and their items are different instances,
// 'Is()' correctly identifies them as value-equivalent.
bool areListsEqual = list1.Is(list2);
Console.WriteLine($"Lists are equal: {areListsEqual}"); // Output: Lists are equal: True

// Modify an item in one list
list2 = new Person { Id = 3, Name = "Sam Jones" };

bool areListsStillEqual = list1.Is(list2);
Console.WriteLine($"Lists are still equal: {areListsStillEqual}"); // Output: Lists are still equal: False
```
