using System;
using System.Reflection;
using System.Runtime.Loader;

namespace OutWit.Common.Plugins.Model
{
    internal class WitPluginLoadContext : AssemblyLoadContext
    {
        #region Fields

        private readonly AssemblyDependencyResolver m_resolver;

        #endregion

        #region Constructors

        public WitPluginLoadContext(string pluginPath) 
            : base(isCollectible: true)
        {
            m_resolver = new AssemblyDependencyResolver(pluginPath);
        }

        #endregion

        #region Functions

        protected override Assembly? Load(AssemblyName assemblyName)
        {
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
