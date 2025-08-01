using System;
using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.Plugins.Abstractions.Attributes;
using OutWit.Common.Plugins.Tests.Mock.Interfaces;

namespace OutWit.Common.Plugins.Tests.Mock.MissingDependencyPlugin
{
    [WitPluginManifest("MissingDependencyPlugin")]
    [WitPluginDependency("NonExistentPlugin")]
    public class MissingDependencyPlugin : ITestPlugin
    {
        public string GetName() => "MissingDependencyPlugin";
        public void Dispose() { }
        public void Initialize(IServiceCollection services) { }
        public void OnInitialized(IServiceProvider serviceProvider) { }
        public void OnUnloading() { }
    }

}
