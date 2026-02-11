using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace OutWit.Common.Settings.Configuration
{
    /// <summary>
    /// Resolves cross-platform paths for settings files based on assembly name.
    /// Assembly name is split by dots to create a hierarchical folder structure.
    /// </summary>
    public static class SettingsPathResolver
    {
        #region Constants

        private const int DEFAULT_DEPTH = 1;

        #endregion

        #region Functions

        /// <summary>
        /// Returns the user-scoped settings directory (per-user, roaming).
        /// Windows: %APPDATA%/Segment1/Segment2.../
        /// Linux:   ~/.config/Segment1/Segment2.../
        /// macOS:   ~/Library/Application Support/Segment1/Segment2.../
        /// </summary>
        /// <param name="assembly">The assembly whose name determines the folder hierarchy.</param>
        /// <param name="depth">Number of leading name segments that become separate folders.</param>
        /// <returns>Full path to the user settings directory.</returns>
        public static string GetUserDataPath(Assembly assembly, int depth = DEFAULT_DEPTH)
        {
            var root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(root, BuildRelativePath(assembly, depth));
        }

        /// <summary>
        /// Returns the global-scoped settings directory (shared across users).
        /// Windows: %PROGRAMDATA%/Segment1/Segment2.../
        /// Linux:   /etc/Segment1/Segment2.../
        /// macOS:   /Library/Application Support/Segment1/Segment2.../
        /// </summary>
        /// <param name="assembly">The assembly whose name determines the folder hierarchy.</param>
        /// <param name="depth">Number of leading name segments that become separate folders.</param>
        /// <returns>Full path to the global settings directory.</returns>
        public static string GetGlobalDataPath(Assembly assembly, int depth = DEFAULT_DEPTH)
        {
            var root = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            return Path.Combine(root, BuildRelativePath(assembly, depth));
        }

        /// <summary>
        /// Splits assembly name by dots and creates a hierarchical path.
        /// Depth controls how many leading segments become separate folders.
        /// </summary>
        /// <example>
        /// "OutWit.Settings.Example.Module.Json", depth=1 → "OutWit/Settings.Example.Module.Json"
        /// "OutWit.Settings.Example.Module.Json", depth=2 → "OutWit/Settings/Example.Module.Json"
        /// </example>
        /// <param name="assembly">The assembly whose name to split.</param>
        /// <param name="depth">Number of leading segments that become separate folders.</param>
        /// <returns>A relative path built from the assembly name.</returns>
        internal static string BuildRelativePath(Assembly assembly, int depth)
        {
            var name = assembly.GetName().Name ?? "App";
            var parts = name.Split('.');

            if (depth <= 0 || depth >= parts.Length)
                return name;

            var folders = new string[depth + 1];
            for (int i = 0; i < depth; i++)
                folders[i] = parts[i];

            folders[depth] = string.Join(".", parts.Skip(depth));

            return Path.Combine(folders);
        }

        /// <summary>
        /// Returns the default path for the defaults file.
        /// Convention: <c>{AppContext.BaseDirectory}/Resources/settings{extension}</c>.
        /// </summary>
        /// <param name="extension">File extension including dot (e.g. ".json", ".csv", ".db").</param>
        /// <returns>Full path to the defaults file.</returns>
        public static string GetDefaultsPath(string extension)
        {
            return Path.Combine(AppContext.BaseDirectory, "Resources", $"settings{extension}");
        }

        /// <summary>
        /// Returns the data path for the specified scope.
        /// User scope returns per-user roaming directory.
        /// Global scope returns machine-wide shared directory.
        /// Default scope is not supported and throws <see cref="ArgumentException"/>.
        /// </summary>
        /// <param name="scope">The settings scope (User or Global).</param>
        /// <param name="assembly">The assembly whose name determines the folder hierarchy.</param>
        /// <param name="depth">Number of leading name segments that become separate folders.</param>
        /// <returns>Full path to the scope-specific settings directory.</returns>
        public static string GetScopeDataPath(SettingsScope scope, Assembly assembly, int depth = DEFAULT_DEPTH)
        {
            return scope switch
            {
                SettingsScope.User => GetUserDataPath(assembly, depth),
                SettingsScope.Global => GetGlobalDataPath(assembly, depth),
                _ => throw new ArgumentException(
                    $"Cannot resolve path for scope {scope}. Use explicit file path for Default scope.",
                    nameof(scope))
            };
        }

        #endregion
    }
}
