using System;
using System.Collections.Generic;
using System.Linq;
#if !NETSTANDARD2_0
using System.Runtime.Loader;
#endif
using System.Text;
using System.Threading.Tasks;
using OutWit.Common.Plugins.Abstractions.Interfaces;

namespace OutWit.Common.Plugins.Model
{
    internal class WitPluginContext<TPlugin>
        where TPlugin : class, IWitPlugin
    {
#if !NETSTANDARD2_0
        public WitPluginContext(TPlugin plugin, WitPluginMetadata metadata, AssemblyLoadContext loadContext, WeakReference? contextReference)
        {
            Plugin = plugin;
            Metadata = metadata;
            LoadContext = loadContext;
            ContextReference = contextReference;
        }
#else
        public WitPluginContext(TPlugin plugin, WitPluginMetadata metadata)
        {
            Plugin = plugin;
            Metadata = metadata;
        }
#endif

        #region Properties

        public TPlugin Plugin { get; }
        
        public WitPluginMetadata Metadata { get; }

#if !NETSTANDARD2_0
        public AssemblyLoadContext LoadContext { get; }
        
        public WeakReference? ContextReference { get; }
#endif

        #endregion
    }
}
