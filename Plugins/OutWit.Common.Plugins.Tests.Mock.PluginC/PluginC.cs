using System;
using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.Plugins.Abstractions.Attributes;
using OutWit.Common.Plugins.Tests.Mock.Interfaces;

namespace OutWit.Common.Plugins.Tests.Mock.PluginC
{
    [WitPluginManifest("PluginC", Version = "1.0.0", Priority = 150)]
    [WitPluginDependency("PluginA", MinimumVersion = "1.0.0")]
    public class PluginC : ITestPlugin
    {
        public string GetName() => "PluginC";
        public void Dispose() { }
        public void Initialize(IServiceCollection services) { }
        public void OnInitialized(IServiceProvider serviceProvider) { }
        public void OnUnloading() { }
    }
}
