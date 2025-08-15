using System;
using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.Plugins.Abstractions.Attributes;
using OutWit.Common.Plugins.Tests.Mock.Interfaces;

namespace OutWit.Common.Plugins.Tests.Mock.CircularA
{
    [WitPluginManifest("CircularA")]
    [WitPluginDependency("CircularB")]
    public class CircularA : ITestPlugin
    {
        public string GetName() => "CircularA";
        public void Dispose() { }
        public void Initialize(IServiceCollection services) { }
        public void OnInitialized(IServiceProvider serviceProvider) { }
        public void OnUnloading() { }
    }
}
