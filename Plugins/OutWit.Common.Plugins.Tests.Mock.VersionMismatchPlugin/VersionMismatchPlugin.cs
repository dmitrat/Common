using System;
using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.Plugins.Abstractions.Attributes;
using OutWit.Common.Plugins.Tests.Mock.Interfaces;

namespace OutWit.Common.Plugins.Tests.Mock.VersionMismatchPlugin
{
    [WitPluginManifest("VersionMismatchPlugin")]
    [WitPluginDependency("PluginA", MinimumVersion = "2.0.0")]
    public class VersionMismatchPlugin : ITestPlugin
    {
        public string GetName() => "VersionMismatchPlugin";
        public void Dispose() { }
        public void Initialize(IServiceCollection services) { }
        public void OnInitialized(IServiceProvider serviceProvider) { }
        public void OnUnloading() { }
    }
}
