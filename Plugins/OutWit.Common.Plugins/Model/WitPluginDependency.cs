using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OutWit.Common.Abstract;
using OutWit.Common.Attributes;
using OutWit.Common.Values;

namespace OutWit.Common.Plugins.Model
{
    internal class WitPluginDependency : ModelBase
    {
        #region Constructors

        public WitPluginDependency(string pluginName, Version minimumVersion)
        {
            PluginName = pluginName;
            MinimumVersion = minimumVersion;
        }

        #endregion

        #region ModelBase

        public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
        {
            if(modelBase is not WitPluginDependency dependency)
                return false;
            return PluginName.Is(dependency.PluginName) && 
                   MinimumVersion.Is(dependency.MinimumVersion);
        }

        public override WitPluginDependency Clone()
        {
            return new WitPluginDependency(PluginName, MinimumVersion);
        }

        #endregion

        #region Properties

        [ToString]
        public string PluginName { get; }

        [ToString]
        public Version MinimumVersion { get; }

        #endregion
    }
}
