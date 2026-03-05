#if !NETSTANDARD2_0
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Loader;

namespace OutWit.Common.Plugins.Model
{
    internal class WitPluginLoadContext : AssemblyLoadContext
    {
        #region Fields

        private readonly AssemblyDependencyResolver m_resolver;

        private readonly HashSet<string> m_sharedAssemblyNames;

        #endregion

        #region Constructors

        public WitPluginLoadContext(string pluginPath, IReadOnlyCollection<string> sharedAssemblyNames) 
            : base(isCollectible: true)
        {
            m_resolver = new AssemblyDependencyResolver(pluginPath);
            m_sharedAssemblyNames = new HashSet<string>(sharedAssemblyNames, StringComparer.OrdinalIgnoreCase);
        }

        #endregion

        #region Functions

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            if (assemblyName.Name != null && m_sharedAssemblyNames.Contains(assemblyName.Name))
                return null;

            string? assemblyPath = m_resolver.ResolveAssemblyToPath(assemblyName);
            return assemblyPath != null
                ? LoadFromAssemblyPath(assemblyPath) 
                : null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            string? libraryPath = m_resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            return libraryPath != null 
                ? LoadUnmanagedDllFromPath(libraryPath) 
                : IntPtr.Zero;
        }

        #endregion
    }
}
#endif
