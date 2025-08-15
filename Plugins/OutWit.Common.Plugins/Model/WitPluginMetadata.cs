using System;
using System.Collections.Generic;
using System.Linq;
using OutWit.Common.Abstract;
using OutWit.Common.Attributes;
using OutWit.Common.Collections;
using OutWit.Common.Values;

namespace OutWit.Common.Plugins.Model
{
    internal class WitPluginMetadata : ModelBase
    {
        #region Constructors

        public WitPluginMetadata(string name, string typeName, string filePath)
        {
            Name = name;
            PluginTypeName = typeName;
            FilePath = filePath;
        }

        #endregion

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
        {
            if(modelBase is not WitPluginMetadata metadata)
                return false;
            
            return Name.Is(metadata.Name) &&
                   PluginTypeName.Is(metadata.PluginTypeName) &&
                   FilePath.Is(metadata.FilePath) &&
                   Version.Is(metadata.Version) &&
                   LoadOrder.Is(metadata.LoadOrder) &&
                   Dependencies.Is(metadata.Dependencies);
        }

        public override WitPluginMetadata Clone()
        {
            return new WitPluginMetadata(Name, PluginTypeName, FilePath)
            {
                Version = Version,
                Priority = Priority,
                LoadOrder = LoadOrder,
                Dependencies = Dependencies.Select(dependency => dependency.Clone()).ToList()
            };
        }

        #endregion

        #region Properties

        [ToString]
        public string Name { get;}

        public string PluginTypeName { get; }

        public string FilePath { get; }

        [ToString]
        public Version? Version { get; init; }
        
        public int Priority { get; init; }
        
        public int LoadOrder { get; set; }

        public IReadOnlyList<WitPluginDependency> Dependencies { get; init; }

        #endregion
    }
}
