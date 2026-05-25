using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using OutWit.Common.Plugins.Abstractions.Attributes;
using OutWit.Common.Plugins.Model;
using OutWit.Common.Values;

namespace OutWit.Common.Plugins.Utils
{
    internal static class AttributesUtils
    {
        public static CustomAttributeData? GetAttributeData<TAttribute>(this Type me)
            where TAttribute : Attribute
        {
            // FullName comparison (vs AssemblyQualifiedName via .Is()) — the
            // attribute type a plugin module references comes from its bundled
            // Plugins.Abstractions copy, which can be at a different version
            // than the host's loader. Matching by FullName lets plugins built
            // against an older Plugins.Abstractions stay discoverable after
            // the host bumps. Mirrors the interface check in
            // WitPluginLoader.DiscoverMetadata.
            var targetName = typeof(TAttribute).FullName;
            return me.CustomAttributes.FirstOrDefault(data => data.AttributeType.FullName == targetName);
        }
        
        public static string? GetName(this CustomAttributeData? me)
        {
            if(me?.ConstructorArguments.Count != 1)
                return null;

            return me.ConstructorArguments[0].Value is not string name 
                ? null
                : name;
        }

        public static Version GetVersion(this CustomAttributeData? me)
        {
            if (me?.NamedArguments == null || me.NamedArguments.Count == 0)
                return new Version(1, 0, 0);
            
            var value = me.NamedArguments
                .FirstOrDefault(arg => arg.MemberName == nameof(WitPluginManifestAttribute.Version))
                .TypedValue.Value;
            
            if(value is not string versionString || !Version.TryParse(versionString, out Version? version))
                return new Version(1, 0, 0);

            return version;
        }

        public static Version GetMinimumVersion(this CustomAttributeData? me)
        {
            if (me?.NamedArguments == null || me.NamedArguments.Count == 0)
                return new Version(1, 0, 0);

            var value = me.NamedArguments
                .FirstOrDefault(arg => arg.MemberName == nameof(WitPluginDependencyAttribute.MinimumVersion))
                .TypedValue.Value;

            if (value is not string versionString || !Version.TryParse(versionString, out Version? version))
                return new Version(1, 0, 0);

            return version;
        }

        public static int GetPriority(this CustomAttributeData? me)
        {
            if (me?.NamedArguments == null || me.NamedArguments.Count == 0)
                return int.MaxValue;

            var value = me.NamedArguments
                .FirstOrDefault(arg => arg.MemberName == nameof(WitPluginManifestAttribute.Priority))
                .TypedValue.Value;

            if (value is not int priority)
                return int.MaxValue;

            return priority;
        }

        public static IReadOnlyList<WitPluginDependency> GetDependencies(this Type me)
        {
            var dependencies = new List<WitPluginDependency>();

            var dependencyAttributeName = typeof(WitPluginDependencyAttribute).FullName;
            IReadOnlyList<CustomAttributeData> attributes = me.GetCustomAttributesData()
                .Where(data => data.AttributeType.FullName == dependencyAttributeName).ToList();

            if (attributes.Count == 0)
                return dependencies;

            foreach (var attribute in attributes)
            {
                var name = attribute.GetName();
                var version = attribute.GetMinimumVersion();
                
                if(string.IsNullOrEmpty(name))
                    continue;
                
                dependencies.Add(new WitPluginDependency(name, version));
            }

            return dependencies.ToImmutableList();


        }
    }
}
