using System;
using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.Plugins.Abstractions.Attributes;
using OutWit.Common.Plugins.Tests.Mock.Interfaces;

namespace OutWit.Common.Plugins.Tests.Mock.DuplicateNamePlugin
{
    [WitPluginManifest("PluginA", Version = "1.1.0")]
    public class DuplicateNamePlugin : ITestPlugin
    {
        public string GetName() => "DuplicateNamePlugin";
        public void Dispose() { }
        public void Initialize(IServiceCollection services) { }
        public void OnInitialized(IServiceProvider serviceProvider) { }
        public void OnUnloading() { }
    }

}
