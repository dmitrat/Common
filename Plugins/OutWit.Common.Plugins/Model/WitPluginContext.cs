using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;
using OutWit.Common.Plugins.Abstractions.Interfaces;

namespace OutWit.Common.Plugins.Model
{
    internal class WitPluginContext<TPlugin>
        where TPlugin : class, IWitPlugin
    {
        public WitPluginContext(TPlugin plugin, WitPluginMetadata metadata, AssemblyLoadContext loadContext, WeakReference? contextReference)
        {
            Plugin = plugin;
            Metadata = metadata;
            LoadContext = loadContext;
            ContextReference = contextReference;
        }

        #region Properties

        public TPlugin Plugin { get; }
        
        public WitPluginMetadata Metadata { get; }

        public AssemblyLoadContext LoadContext { get; }
        
        public WeakReference? ContextReference { get; }

        #endregion
    }
}
