# OutWit.Common.MVVM.Abstractions

**?? This project is currently empty and reserved for future cross-platform abstractions.**

## Current Status

Attribute definitions for MVVM property generation have been moved to platform-specific projects:

- **WPF**: `OutWit.Common.MVVM.WPF` contains `[StyledProperty]` and `[AttachedProperty]`
- **Avalonia**: (Coming soon) Will contain its own `[StyledProperty]` and `[DirectProperty]`

## Why Platform-Specific?

Each platform has its own property system:
- **WPF**: `DependencyProperty` with `GetValue`/`SetValue`
- **Avalonia**: `StyledProperty`/`DirectProperty` with different APIs

By keeping attributes in platform-specific projects, we can:
1. Include AspectInjector integration directly in the attribute
2. Avoid complex conditional compilation
3. Keep each project self-contained

## Future Use

This project may be used for truly cross-platform abstractions that don't require platform-specific integration, such as:
- Interface definitions
- Common enums
- Shared constants

## Related Packages

- **OutWit.Common.MVVM.WPF** - WPF implementation with attributes and aspects
- **OutWit.Common.MVVM.WPF.Generator** - Source generator for WPF
- **OutWit.Common.MVVM** - Cross-platform base classes (commands, collections)
