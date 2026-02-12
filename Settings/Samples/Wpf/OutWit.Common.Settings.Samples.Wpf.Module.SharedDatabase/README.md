# OutWit.Common.Settings.Samples.Wpf.Module.SharedDatabase

Shared database module demonstrating multi-user settings with per-user isolation.

## What This Module Demonstrates

### ?? Shared Database with User Isolation

- **Single Database File** - All scopes in one `.witdb` file
- **Per-User Tables** - User settings isolated by username
- **Global Settings** - Shared across all users
- **Mixed Scopes** - Different settings can have different scopes

### Settings Defined

| Setting | Type | Scope | Default | Description |
|---------|------|-------|---------|-------------|
| `GlobalSetting` | String | Global | "shared" | Shared across all users |
| `UserSpecificSetting` | String | User | "default" | Per-user setting |
| `GlobalCounter` | Integer | Global | 0 | Shared counter |
| `UserPreference` | Boolean | User | true | User-specific preference |

## Storage Structure

```
shared-settings.witdb
??? __defaults__           (Default scope table)
??? __global__             (Global scope table - shared)
??? user_alice             (User scope table for Alice)
??? user_bob               (User scope table for Bob)
??? ...
```

## Architecture

```
???????????????????????????????????????????????????????
?               SharedDatabaseModule                  ?
?                                                     ?
?  ???????????????????????????????????????????????   ?
?  ? DatabaseScopedSettingsProvider              ?   ?
?  ?                                             ?   ?
?  ?  ???????????  ???????????  ???????????    ?   ?
?  ?  ? Default ?  ? Global  ?  ?  User   ?    ?   ?
?  ?  ?  Table  ?  ?  Table  ?  ?  Table  ?    ?   ?
?  ?  ???????????  ???????????  ???????????    ?   ?
?  ?                              (per user)   ?   ?
?  ???????????????????????????????????????????????   ?
???????????????????????????????????????????????????????
```

## Usage

```csharp
var module = new SharedDatabaseModule();
module.Initialize();

// Create typed container bound to the manager
var settings = new SharedSettings(module.Manager);

// Global setting - visible to all users
settings.GlobalSetting = "new value";  // Changes for everyone

// User setting - isolated per user
settings.UserSpecificSetting = "my preference";  // Only affects current user

module.Manager.Save();
```

## Module Structure

```csharp
public sealed class SharedDatabaseModule : ISharedDatabaseModule
{
    private const string DB_PATH = "shared-settings.witdb";

    public void Initialize()
    {
        SharedDatabaseSeeder.EnsureDefaults(DB_PATH);

        Manager = new SettingsBuilder()
            .UseSharedDatabase(options =>
            {
                options.DatabasePath = DB_PATH;
                options.UserId = Environment.UserName;  // Per-user isolation
            })
            .RegisterContainer<SharedSettings>()
            .Build();

        Manager.Merge();
        Manager.Load();
    }
}
```

## Seeding Defaults

```csharp
public static class SharedDatabaseSeeder
{
    public static void EnsureDefaults(string dbPath)
    {
        using var provider = new DatabaseScopedSettingsProvider(
            dbPath, SettingsScope.Default);

        provider.Upsert("SharedSettings", "GlobalSetting",
            new SettingsRecord("GlobalSetting", "String", null, 
                "Global Setting", "shared") { Scope = SettingsScope.Global });

        provider.Upsert("SharedSettings", "UserSpecificSetting",
            new SettingsRecord("UserSpecificSetting", "String", null,
                "User Preference", "default") { Scope = SettingsScope.User });
    }
}
```

## When to Use Shared Database

- Multi-user desktop applications
- Settings that should be shared across all users (e.g., license, server URL)
- Settings that must be isolated per user (e.g., preferences, recent files)
- Single configuration file deployment
