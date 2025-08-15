using System;
using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.Plugins.Abstractions.Attributes;
using OutWit.Common.Plugins.Tests.Mock.Interfaces;

namespace OutWit.Common.Plugins.Tests.Mock.PluginA
{
    [WitPluginManifest("PluginA", Version = "1.0.0", Priority = 200)]
    public class PluginA : ITestPlugin
    {
        public string GetName() => "PluginA";
        public void Dispose() { }
        public void Initialize(IServiceCollection services) { }
        public void OnInitialized(IServiceProvider serviceProvider) { }
        public void OnUnloading() { }
    }
}
