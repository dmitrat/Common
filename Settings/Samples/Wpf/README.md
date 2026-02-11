# OutWit.Common.Settings.Samples.Wpf

A complete WPF desktop application demonstrating the OutWit.Common.Settings library.

## What This Sample Demonstrates

### ?? Settings Window UI

A fully functional settings dialog with:

- **Tabbed Interface** - Settings organized by groups (Application, Network, Advanced, Shared)
- **Multiple Value Editors** - Automatic editor selection based on value type
- **Change Detection** - Modified values are highlighted
- **Reset Functionality** - Reset individual values or entire groups to defaults
- **Save/Cancel** - Standard dialog workflow

### ?? Value Editors

| Type | Editor | Description |
|------|--------|-------------|
| `String` | TextBox | Standard text input |
| `Integer`, `Long`, `Double`, `Decimal` | TextBox | Numeric input |
| `Boolean` | CheckBox | Toggle switch |
| `Enum` | ComboBox | Dropdown selection |
| `TimeSpan` | TextBox | Duration input |
| `Password` | PasswordBox | Masked input |
| `BoundedInt` | Slider + Label | Range-constrained integer |
| `ColorRgb` | RGB Sliders + Preview | Color picker with live preview |

### ??? Storage Provider Modules

Each module demonstrates a different storage backend:

#### Module.Json
- **Provider:** `JsonSettingsProvider`
- **Format:** Human-readable JSON files
- **Settings:** `ApplicationSettings` (Theme, Language, AutoSave, Notifications)

#### Module.Csv
- **Provider:** `CsvSettingsProvider`  
- **Format:** CSV files (editable in Excel/spreadsheets)
- **Settings:** `NetworkSettings` (Proxy, Timeout, MaxRetries)

#### Module.Database
- **Provider:** `DatabaseSettingsProvider`
- **Format:** SQLite database (`.witdb`)
- **Settings:** `AdvancedSettings` (CacheSize, LogLevel, DebugMode, AccentColor)

#### Module.SharedDatabase
- **Provider:** `DatabaseScopedSettingsProvider`
- **Format:** Single shared SQLite database with per-user isolation
- **Settings:** `SharedSettings` (GlobalSetting, UserSpecificSetting)

## Architecture

```
App.xaml.cs
??? ApplicationModule (JSON)      ??? ISettingsManager
??? NetworkModule (CSV)           ??? ISettingsManager
??? AdvancedModule (Database)     ??? ISettingsManager
??? SharedDatabaseModule          ??? ISettingsManager
         ?
         ?
    ApplicationViewModel
         ?
         ?
    SettingsViewModel
         ?
         ??? SettingsGroupViewModel (per group/tab)
         ?    ??? SettingsValueViewModel (per setting)
         ?
         ?
    SettingsWindow.xaml
         ??? EditorTemplateSelector (picks editor by ValueKind)
```

## Key Files

| File | Purpose |
|------|---------|
| `App.xaml.cs` | Initializes all settings modules, aggregates managers |
| `ViewModels/SettingsViewModel.cs` | Aggregates groups from all managers |
| `ViewModels/SettingsValueViewModel.cs` | Wraps `ISettingsValue` for WPF binding |
| `Views/SettingsWindow.xaml` | Settings dialog with TabControl |
| `Views/Editors/EditorTemplateSelector.cs` | Selects DataTemplate by ValueKind |
| `Views/Editors/BoundedIntEditor.xaml` | Custom slider editor |
| `Views/Editors/ColorRgbEditor.xaml` | RGB color picker |

## Running

```bash
dotnet run --project Settings/Samples/Wpf/OutWit.Common.Settings.Samples.Wpf
```

Click **"Settings..."** button to open the settings dialog.

## Settings Files Location

After running, settings files are created in:
- **JSON:** `{AppDir}/settings.json` (defaults), `{UserProfile}/settings.json` (user)
- **CSV:** `{AppDir}/settings.csv` (defaults), `{UserProfile}/settings.csv` (user)
- **Database:** `{AppDir}/settings.witdb` (defaults), `{UserProfile}/settings.witdb` (user)
- **Shared:** `{AppDir}/shared-settings.witdb` (all scopes in one file)
