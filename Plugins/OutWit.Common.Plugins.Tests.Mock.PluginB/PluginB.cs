using System;
using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.Plugins.Abstractions.Attributes;
using OutWit.Common.Plugins.Tests.Mock.Interfaces;

namespace OutWit.Common.Plugins.Tests.Mock.PluginB
{
    [WitPluginManifest("PluginB", Version = "1.0.0", Priority = 100)]
    [WitPluginDependency("PluginA", MinimumVersion = "1.0.0")]
    public class PluginB : ITestPlugin
    {
        public string GetName() => "PluginB";
        public void Dispose() { }
        public void Initialize(IServiceCollection services) { }
        public void OnInitialized(IServiceProvider serviceProvider) { }
        public void OnUnloading() { }
    }
}
