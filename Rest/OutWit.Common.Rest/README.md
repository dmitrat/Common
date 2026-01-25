# OutWit.Common.Rest

## Overview

OutWit.Common.Rest is a robust library designed to simplify and streamline HTTP client interactions in .NET applications. By leveraging a modular and extensible architecture, it offers powerful abstractions for building and executing RESTful requests, handling responses, and managing query parameters with ease.

## Features

### 1. Query Builder
The `QueryBuilder` class provides a fluent API for constructing URL query strings dynamically. It supports multiple parameter types, including primitives, enumerations, and collections.

#### Example Usage
```csharp
var queryBuilder = new QueryBuilder()
    .AddParameter("name", "John Doe")
    .AddParameter("age", 30)
    .AddParameter("tags", new[] { "developer", "blogger" });

string query = await queryBuilder.AsStringAsync();
Console.WriteLine(query); // Output: name=John%20Doe&age=30&tags=developer,blogger
```
### 2. REST Client
The `RestClient` class provides an abstraction for HTTP operations, including `GET`, `POST`, and `SEND` requests. It simplifies common tasks like deserialization and content creation while allowing advanced configuration with fluent methods.

#### Example Usage
```csharp
var client = RestClientBuilder.Create()
    .WithBearer("your-token")
    .WithHeader("Custom-Header", "HeaderValue");

var result = await client.GetAsync<MyResponseType>("https://api.example.com/resource");
```

### 3. Exception Handling
The `RestClientException` class encapsulates HTTP status codes and response content, making error handling intuitive and detailed.

#### Example
```csharp
try
{
    var result = await client.GetAsync<MyResponseType>("https://api.example.com/invalid-resource");
}
catch (RestClientException ex)
{
    Console.WriteLine($"Error: {ex.StatusCode}, Content: {ex.Content}");
}
```

### 4. Serialization and Deserialization
Integrated with Newtonsoft.Json, the library supports efficient serialization and deserialization of request and response content.

#### Deserialize Example
```csharp
var response = await httpResponseMessage.DeserializeAsync<MyResponseType>();
```

### 5. Builder Utilities
The library includes utilities for building requests with advanced features like:
- Custom authorization headers
- Content serialization
- Dynamic query parameter handling

## Installation

Install the package via NuGet:
```bash
Install-Package OutWit.Common.Rest
```

## License

Licensed under the Apache License, Version 2.0. See `LICENSE`.

## Attribution (optional)

If you use OutWit.Common.Rest in a product, a mention is appreciated (but not required), for example:
"Powered by OutWit.Common.Rest (https://ratner.io/)".

## Trademark / Project name

"OutWit" and the OutWit logo are used to identify the official project by Dmitry Ratner.

You may:
- refer to the project name in a factual way (e.g., "built with OutWit.Common.Rest");
- use the name to indicate compatibility (e.g., "OutWit.Common.Rest-compatible").

You may not:
- use "OutWit.Common.Rest" as the name of a fork or a derived product in a way that implies it is the official project;
- use the OutWit.Common.Rest logo to promote forks or derived products without permission.
