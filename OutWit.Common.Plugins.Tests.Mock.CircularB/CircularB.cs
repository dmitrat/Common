using System;
using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.Plugins.Abstractions.Attributes;
using OutWit.Common.Plugins.Tests.Mock.Interfaces;

namespace OutWit.Common.Plugins.Tests.Mock.CircularB
{
    [WitPluginManifest("CircularB")]
    [WitPluginDependency("CircularA")]
    public class CircularB : ITestPlugin
    {
        public string GetName() => "CircularB";
        public void Dispose() { }
        public void Initialize(IServiceCollection services) { }
        public void OnInitialized(IServiceProvider serviceProvider) { }
        public void OnUnloading() { }
    }

}
