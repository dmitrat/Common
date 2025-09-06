using Microsoft.Extensions.Logging;
using OutWit.Common.Plugins.Abstractions.Attributes;
using OutWit.Common.Plugins.Abstractions.Interfaces;
using OutWit.Common.Plugins.Model;
using OutWit.Common.Plugins.Utils;
using OutWit.Common.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using OutWit.Common.Values;

namespace OutWit.Common.Plugins
{
    public class WitPluginLoader<TPlugin> : IDisposable, IEnumerable<TPlugin>
        where TPlugin : class, IWitPlugin
    {
        #region Constants

        private const string ASSEMBLY_SEARCH_PATTERN = "*.dll";

        #endregion

        #region Fields

        private readonly Dictionary<string, WitPluginContext<TPlugin>> m_loadedPlugins = new();

        private readonly List<string> m_pluginSearchPaths = new ();

        #endregion

        #region Constructors

        public WitPluginLoader(string searchPath, bool useIsolatedContexts = true, ILogger? logger = null)
        {
            UseIsolatedContext = useIsolatedContexts;
            Logger = logger;

            if (string.IsNullOrEmpty(searchPath))
                throw new ArgumentNullException(nameof(searchPath), "Search path cannot be null or empty.");

            if (!Directory.Exists(searchPath))
                throw new DirectoryNotFoundException($"Search path '{searchPath}' does not exist.");

            m_pluginSearchPaths.Add(searchPath);
            
            Logger?.LogInformation($"Looking for plugins in {m_pluginSearchPaths.Single()}");
            
            if(!UseIsolatedContext)
                AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        }

        public WitPluginLoader(IEnumerable<string> searchPaths, ILogger? logger = null)
        {
            Logger = logger;

            if (searchPaths == null)
                throw new ArgumentNullException(nameof(searchPaths), "Search paths cannot be null.");

            foreach (var path in searchPaths.Where(Directory.Exists))
                m_pluginSearchPaths.Add(path);
            
            Logger?.LogInformation($"Looking for plugins in {string.Join(", ", m_pluginSearchPaths)}");
        }

        #endregion

        #region Functions

        /// <summary>
        /// Scans directories, resolves dependencies, and loads all valid plugins.
        /// </summary>
        public void Load()
        {
            IReadOnlyList<string> pluginCandidates = GetPluginCandidates();
            IReadOnlyDictionary<string, WitPluginMetadata> plugins = DiscoverMetadata(pluginCandidates, out List<Exception> errors);
            
            if (errors.Any())
                throw new AggregateException("Errors discovered during plugin scanning.", errors);

            IReadOnlyList<WitPluginMetadata> loadOrder = GetLoadOrder(plugins, out errors);
            if (errors.Any())
                throw new AggregateException("Plugin dependency validation failed.", errors);

            for (int i = 0; i < loadOrder.Count; i++)
            {
                loadOrder[i].LoadOrder = i;
                LoadSinglePlugin(loadOrder[i]);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void UnloadPlugin(string pluginName)
        {
            if(!UseIsolatedContext)
                return;

            var dependents = new List<string>();

            foreach (var context in m_loadedPlugins.Values)
            {
                if (context.Metadata.Dependencies.Any(dependency => dependency.PluginName.Is(pluginName)))
                    dependents.Add(context.Metadata.Name);
            }

            if (dependents.Any())
            {
                Logger?.LogError($"Cannot unload plugin '{pluginName}' because it is a dependency for: {string.Join(", ", dependents)}.");
                throw new InvalidOperationException($"Cannot unload plugin '{pluginName}' because it is a dependency for: {string.Join(", ", dependents)}.");
            }

            if (!m_loadedPlugins.Remove(pluginName, out var loadedPlugin))
            {
                Logger?.LogError($"Plugin '{pluginName}' is not loaded or does not exist.");
                return;
            }
            
            loadedPlugin.Plugin.Dispose();
            loadedPlugin.LoadContext.Unload();
        }

        private void LoadSinglePlugin(WitPluginMetadata metadata)
        {
            if (m_loadedPlugins.ContainsKey(metadata.Name))
                return;

            Assembly assembly;
            AssemblyLoadContext loadContext = AssemblyLoadContext.Default;
            WeakReference? reference = null;

            if (UseIsolatedContext)
            {
                loadContext = new WitPluginLoadContext(metadata.FilePath);
                assembly = loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(metadata.FilePath)));
                reference = new WeakReference(loadContext, trackResurrection: true);
            }
            else
            {
                assembly = loadContext.LoadFromAssemblyPath(metadata.FilePath);
            }

            TPlugin? instance = null;
            try
            {
                instance = (TPlugin)Activator.CreateInstance(assembly.GetType(metadata.PluginTypeName)!)!;
            }
            catch (Exception e)
            {
                Logger?.LogError(e, $"Failed to create instance of plugin '{metadata.Name}' from '{metadata.FilePath}'.");
                throw;
            }

            m_loadedPlugins[metadata.Name] = new WitPluginContext<TPlugin>(instance, metadata, loadContext, reference);
        }


        private IReadOnlyDictionary<string, WitPluginMetadata> DiscoverMetadata(IReadOnlyList<string> assemblyPaths, out List<Exception> errors)
        {
            var metadata = new Dictionary<string, WitPluginMetadata>();
            errors = new List<Exception>();

            using var metadataContext = new MetadataLoadContext(GetAssemblyResolver(assemblyPaths));

            foreach (var path in assemblyPaths)
            {
                try
                {
                    var assemblyBytes = File.ReadAllBytes(path);
                    var assembly = metadataContext.LoadFromByteArray(assemblyBytes);
                    foreach (var type in assembly.GetTypes())
                    {
                        bool isPluginType = type.GetInterfaces().Any(x => x.FullName == typeof(TPlugin).FullName);
                        if (!isPluginType)
                            continue;
                        
                        var manifestData = type.GetAttributeData<WitPluginManifestAttribute>();
                        if (manifestData == null)
                            continue;

                        string? pluginName = manifestData.GetName();
                        if (string.IsNullOrEmpty(pluginName))
                        {
                            Logger?.LogError("Plugin name metadata is empty");
                            errors.Add(new InvalidOperationException($"Plugin name metadata is empty'."));
                            continue;
                        }
                        if (metadata.TryGetValue(pluginName, out var existingMetadata))
                        {
                            Logger?.LogError($"Duplicate plugin name '{pluginName}' found in '{path}' and '{existingMetadata.FilePath}'.");
                            errors.Add(new InvalidOperationException($"Duplicate plugin name '{pluginName}' found in '{path}' and '{existingMetadata.FilePath}'."));
                            continue;
                        }
                        
                        metadata[pluginName] = new WitPluginMetadata(pluginName, type.FullName!, path)
                        {
                            Version = manifestData.GetVersion(),
                            Priority = manifestData.GetPriority(),
                            Dependencies = type.GetDependencies()
                        };
                    }
                }
                catch (BadImageFormatException)
                {
                    Logger?.LogTrace("Skipping non-.NET (native) assembly: {path}", path);
                }
                catch (Exception ex)
                {
                    Logger?.LogError(ex, $"Failed to inspect metadata of '{path}'.");
                    errors.Add(new FileLoadException($"Failed to inspect metadata of '{path}'.", ex));
                }
            }
            return metadata;
        }

        private List<WitPluginMetadata> GetLoadOrder(IReadOnlyDictionary<string, WitPluginMetadata> availablePlugins, out List<Exception> errors)
        {
            errors = new List<Exception>();
            var sorted = new List<WitPluginMetadata>();
            var visited = new Dictionary<string, WitPluginVisitState>();

            IReadOnlyList<WitPluginMetadata> initialOrder = availablePlugins.Values.OrderBy(metadata => metadata.Priority).ToList();

            foreach (var plugin in initialOrder)
            {
                if (!visited.ContainsKey(plugin.Name))
                    Visit(plugin, availablePlugins, visited, sorted, errors);
            }
            return sorted;
        }

        private void Visit(WitPluginMetadata plugin, IReadOnlyDictionary<string, WitPluginMetadata> available, Dictionary<string, WitPluginVisitState> visited, List<WitPluginMetadata> sorted, List<Exception> errors)
        {
            visited[plugin.Name] = WitPluginVisitState.Visiting;

            foreach (var dependency in plugin.Dependencies)
            {
                if (!available.TryGetValue(dependency.PluginName, out var metadata))
                {
                    Logger?.LogError($"Plugin '{plugin.Name}' has an unresolved dependency on '{dependency.PluginName}'.");
                    errors.Add(new InvalidOperationException($"Plugin '{plugin.Name}' has an unresolved dependency on '{dependency.PluginName}'."));
                }

                else if (metadata.Version < dependency.MinimumVersion)
                {
                    Logger?.LogError($"Plugin '{plugin.Name}' requires version '{dependency.MinimumVersion}' of '{dependency.PluginName}', but found version '{metadata.Version}'.");
                    errors.Add(new InvalidOperationException($"Plugin '{plugin.Name}' requires version '{dependency.MinimumVersion}' of '{dependency.PluginName}', but found version '{metadata.Version}'."));
                }

                else if (visited.TryGetValue(dependency.PluginName, out var state) && state == WitPluginVisitState.Visiting)
                {
                    Logger?.LogError($"Circular dependency detected: '{plugin.Name}' -> '{dependency.PluginName}'.");
                    errors.Add(new InvalidOperationException($"Circular dependency detected: '{plugin.Name}' -> '{dependency.PluginName}'."));
                }
                else
                {
                    Visit(metadata, available, visited, sorted, errors);
                }
            }

            visited[plugin.Name] = WitPluginVisitState.Visited;
            sorted.Add(plugin);
        }


        private PathAssemblyResolver GetAssemblyResolver(IReadOnlyList<string> assemblyPaths)
        {
            return new PathAssemblyResolver(assemblyPaths.Concat(GetParentAssemblies()).Concat(GetRuntimeAssemblies()));
        }

        private IReadOnlyList<string> GetPluginCandidates()
        {
            return m_pluginSearchPaths.SelectMany(p =>
                Directory.GetFiles(p, ASSEMBLY_SEARCH_PATTERN, SearchOption.AllDirectories)).ToImmutableList();
        }
        
        private IReadOnlyList<string> GetParentAssemblies()
        {
            var assembly = Assembly.GetCallingAssembly();
            string? assemblyDirectory = Path.GetDirectoryName(assembly.Location);
            
            if (!string.IsNullOrEmpty(assemblyDirectory) && Directory.Exists(assemblyDirectory))
                return Directory.GetFiles(assemblyDirectory, ASSEMBLY_SEARCH_PATTERN);
            
            Logger?.LogError($"Cannot find base assembly directory '{assemblyDirectory}'");
            throw new DirectoryNotFoundException($"Cannot find base assembly directory '{assemblyDirectory}'");

        }

        private IReadOnlyList<string> GetRuntimeAssemblies()
        {
            return Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), ASSEMBLY_SEARCH_PATTERN);
        }

        #endregion

        #region IEnumerable

        public IEnumerator<TPlugin> GetEnumerator()
        {
            return Plugins.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (!UseIsolatedContext)
                AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;

            foreach (var plugin in m_loadedPlugins.Values.OrderByDescending(context => context.Metadata.LoadOrder))
            {
                try
                {
                    UnloadPlugin(plugin.Metadata.Name);
                }
                catch
                {
                    Logger?.LogWarning($"Failed to unload plugin '{plugin}' on shutdown.");
                }
            }
        
        }

        #endregion

        #region Event Handlers

        private Assembly? OnAssemblyResolve(object? sender, ResolveEventArgs args)
        {
            var neededAssemblyName = new AssemblyName(args.Name).Name;
            if (string.IsNullOrEmpty(neededAssemblyName))
                return null;

            var assemblyFile = GetPluginCandidates().FirstOrDefault(x => Path.GetFileNameWithoutExtension(x) == neededAssemblyName);
            return string.IsNullOrEmpty(assemblyFile) 
                ? null
                : Assembly.LoadFrom(assemblyFile);
        }

        #endregion

        #region Properies

        public TPlugin this[string pluginName]
        {
            get
            {
                if (m_loadedPlugins.TryGetValue(pluginName, out var context))
                    return context.Plugin;
                
                throw new KeyNotFoundException($"Plugin '{pluginName}' is not loaded.");
            }
        }

        public IReadOnlyList<string> Keys => m_loadedPlugins
            .Values
            .OrderByDescending(context=>context.Metadata.Priority)
            .Select(context => context.Metadata.Name)
            .ToImmutableList();
        
        public IReadOnlyList<TPlugin> Plugins => m_loadedPlugins
            .Values
            .OrderByDescending(context => context.Metadata.Priority)
            .Select(context => context.Plugin)
            .ToImmutableList();

        private ILogger? Logger { get; }
        
        public bool UseIsolatedContext { get; }

        #endregion
    }
}
