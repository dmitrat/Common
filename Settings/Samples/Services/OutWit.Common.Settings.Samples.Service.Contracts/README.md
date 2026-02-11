# OutWit.Common.Settings.Samples.Service.Contracts

Shared service contract for remote settings management.

## Overview

This project contains the `ISettingsService` interface used by both the server and client in the remote settings sample.

## Service Interface

```csharp
[WitRpcService]
public interface ISettingsService
{
    /// <summary>
    /// Gets metadata for all available setting groups.
    /// </summary>
    Task<IReadOnlyList<SettingsGroupInfo>> GetGroupsAsync();

    /// <summary>
    /// Gets all values within a specific group.
    /// </summary>
    Task<IReadOnlyList<ISettingsValue>> GetValuesAsync(string group);

    /// <summary>
    /// Updates a single setting value.
    /// </summary>
    /// <param name="group">The group containing the setting.</param>
    /// <param name="key">The setting key.</param>
    /// <param name="value">The new value as a string.</param>
    Task UpdateValueAsync(string group, string key, string value);

    /// <summary>
    /// Resets a single value to its default.
    /// </summary>
    Task ResetValueAsync(string group, string key);

    /// <summary>
    /// Resets all values in a group to defaults.
    /// </summary>
    Task ResetGroupAsync(string group);

    /// <summary>
    /// Persists all changes to storage.
    /// </summary>
    Task SaveAsync();

    /// <summary>
    /// Reloads all values from storage.
    /// </summary>
    Task ReloadAsync();
}
```

## Key Types

### SettingsGroupInfo

```csharp
public record SettingsGroupInfo
{
    public string Group { get; init; }       // Internal group identifier
    public string DisplayName { get; init; } // User-friendly name
    public int Priority { get; init; }       // Sort order
}
```

### ISettingsValue

Polymorphic value type that includes:
- `Key` - Setting identifier
- `Name` - Display name
- `Value` - Current value (typed)
- `DefaultValue` - Original default value
- `IsDefault` - Whether the value matches default
- `ValueKind` - Type discriminator (String, Integer, Enum, BoundedInt, etc.)

## Usage

### Server Side

Implement the interface:

```csharp
public class SettingsServiceImpl : ISettingsService
{
    public async Task<IReadOnlyList<SettingsGroupInfo>> GetGroupsAsync()
    {
        // Return groups from settings managers
    }
}
```

### Client Side

Inject and use via WitRPC proxy:

```csharp
var service = await channelFactory.GetServiceAsync<ISettingsService>();
var groups = await service.GetGroupsAsync();
```

## Dependencies

- **OutWit.Communication.Model** - `[WitRpcService]` attribute
- **OutWit.Common.Settings** - `ISettingsValue`, `SettingsGroupInfo`
