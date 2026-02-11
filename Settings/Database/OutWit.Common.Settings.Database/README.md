# OutWit.Common.Settings.Database

## Overview

OutWit.Common.Settings.Database is an Entity Framework Core database storage provider for the OutWit.Common.Settings framework. It enables storing settings in relational databases with support for standalone databases, shared databases, and multi-user isolation.

## Features

### 1. Standalone Database
Use separate database files for each scope with automatic table creation:

```csharp
var manager = new SettingsBuilder()
    .UseDatabase(path => options => options.UseSqlite($"Data Source={path}"))
    .RegisterContainer<AdvancedSettings>()
    .Build();

manager.Merge();
manager.Load();
```

### 2. Shared Database
Store all scopes in a single database with separate tables for Default, Global, and User settings:

```csharp
var manager = new SettingsBuilder()
    .UseSharedDatabase(
        options => options.UseNpgsql(connectionString),
        userId: Environment.UserName)  // For per-user isolation
    .RegisterContainer<SharedSettings>()
    .Build();
```

Tables created:
- `Settings` - Default scope (read-only)
- `Settings_Global` - Global scope
- `Settings_User` - User scope with `UserId` column

### 3. Custom DbContext Integration
Integrate settings storage into your existing database context:

```csharp
public class AppDbContext : DbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplySettingsConfiguration();  // Adds Settings tables
        // ... your other configurations
    }
}

// Usage
var manager = new SettingsBuilder()
    .UseDatabase(() => new AppDbContext(), SettingsScope.Default)
    .Build();
```

### 4. Database Providers
Works with any EF Core database provider:
- SQLite
- PostgreSQL
- SQL Server
- MySQL
- And more...

```csharp
// SQLite
.UseDatabase(path => o => o.UseSqlite($"Data Source={path}"))

// PostgreSQL
.UseSharedDatabase(o => o.UseNpgsql(connectionString), userId)

// SQL Server
.UseDatabase(o => o.UseSqlServer(connectionString), SettingsScope.Global)
```

### 5. Multi-User Support
User scope settings are isolated by `UserId` in shared database mode:

```csharp
// Desktop app - use Windows username
.UseSharedDatabase(configure, userId: Environment.UserName)

// Web app - use authenticated user ID
.UseSharedDatabase(configure, userId: currentUser.Id)
```

### 6. Automatic Schema Management
Tables are created automatically on first use. The schema includes:
- `SettingsGroups` - Group metadata (display name, priority)
- `SettingsEntries` - Individual settings (group, key, value, valueKind, tag, hidden)

## Installation

Install the package via NuGet:
```bash
Install-Package OutWit.Common.Settings.Database
```

**Dependencies:**
- `OutWit.Common.Settings`
- `Microsoft.EntityFrameworkCore.Relational` (8.0+ / 9.0+ / 10.0+)

You also need a database provider package:
```bash
Install-Package Microsoft.EntityFrameworkCore.Sqlite
# or
Install-Package Npgsql.EntityFrameworkCore.PostgreSQL
# or
Install-Package Microsoft.EntityFrameworkCore.SqlServer
```

## License

Licensed under the Apache License, Version 2.0. See `LICENSE`.

## Attribution (optional)

If you use OutWit.Common.Settings.Database in a product, a mention is appreciated (but not required), for example:
"Powered by OutWit.Common.Settings.Database (https://ratner.io/)".

## Trademark / Project name

"OutWit" and the OutWit logo are used to identify the official project by Dmitry Ratner.

You may:
- refer to the project name in a factual way (e.g., "built with OutWit.Common.Settings.Database");
- use the name to indicate compatibility (e.g., "OutWit.Common.Settings.Database-compatible").

You may not:
- use "OutWit.Common.Settings.Database" as the name of a fork or a derived product in a way that implies it is the official project;
- use the OutWit.Common.Settings.Database logo to promote forks or derived products without permission.
