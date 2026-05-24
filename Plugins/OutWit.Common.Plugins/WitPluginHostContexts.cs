using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using OutWit.Common.Interfaces;
using OutWit.Common.Plugins.Model;

namespace OutWit.Common.Plugins
{
    /// <summary>
    /// Process-wide registry of <see cref="WitPluginHostContext"/>
    /// entries keyed by <see cref="Assembly"/>. Populated automatically
    /// by <see cref="WitPluginLoader{TPlugin}"/> after each successful
    /// load; queried by code that wants to know "where on disk did the
    /// loader put this plugin assembly?" — independently of
    /// <see cref="Assembly.Location"/>, which can disagree when the
    /// runtime resolved the assembly from a project-reference graph
    /// copy or a shared
    /// <see cref="System.Runtime.Loader.AssemblyLoadContext"/> lookup
    /// rather than from the path the loader scanned.
    /// </summary>
    /// <remarks>
    /// The <see cref="For(Assembly)"/> accessor always returns a
    /// non-null <see cref="IAssemblyContext"/>: if the assembly was
    /// loaded via <see cref="WitPluginLoader{TPlugin}"/>, the
    /// registered context is returned verbatim; otherwise a synthetic
    /// fallback context is constructed from
    /// <see cref="Assembly.Location"/> so callers don't need to
    /// special-case the "not a plugin" path.
    /// </remarks>
    public static class WitPluginHostContexts
    {
        #region Fields

        private static readonly ConcurrentDictionary<Assembly, WitPluginHostContext> s_byAssembly = new();

        #endregion

        #region Functions

        /// <summary>
        /// Returns the registered host context for <paramref name="assembly"/>,
        /// or a synthetic fallback derived from
        /// <see cref="Assembly.Location"/> when no entry exists.
        /// Never returns <c>null</c>.
        /// </summary>
        public static IAssemblyContext For(Assembly assembly)
        {
            if (s_byAssembly.TryGetValue(assembly, out var registered))
                return registered;

            var location = assembly.Location ?? string.Empty;
            return new WitPluginHostContext(
                name: assembly.GetName().Name ?? "<unknown>",
                assemblyPath: location,
                homeDirectory: Path.GetDirectoryName(location) ?? string.Empty);
        }

        /// <summary>
        /// Looks up the registered host context by plugin name.
        /// Returns <c>null</c> when no entry with the matching
        /// <see cref="WitPluginHostContext.Name"/> exists. Name match
        /// is ordinal case-insensitive (matches the loader's
        /// duplicate-name check semantics).
        /// </summary>
        public static WitPluginHostContext? TryGetByName(string name)
        {
            foreach (var entry in s_byAssembly.Values)
            {
                if (string.Equals(entry.Name, name, System.StringComparison.OrdinalIgnoreCase))
                    return entry;
            }

            return null;
        }

        /// <summary>
        /// Enumerates every currently-registered host context.
        /// </summary>
        public static IReadOnlyList<WitPluginHostContext> All
            => s_byAssembly.Values.ToArray();

        #endregion

        #region Tools

        internal static void Register(Assembly assembly, WitPluginHostContext context)
        {
            s_byAssembly[assembly] = context;
        }

        internal static void Unregister(Assembly assembly)
        {
            s_byAssembly.TryRemove(assembly, out _);
        }

        #endregion
    }
}
