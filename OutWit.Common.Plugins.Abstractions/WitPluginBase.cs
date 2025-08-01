using System;
using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.Plugins.Abstractions.Interfaces;

namespace OutWit.Common.Plugins.Abstractions
{
    public abstract class WitPluginBase : IWitPlugin
    {
        public virtual void Initialize(IServiceCollection services)
        {
        }

        public virtual void OnInitialized(IServiceProvider serviceProvider)
        {
        }

        public virtual void OnUnloading()
        {
        }

        public virtual void Dispose()
        {
        }
    }
}
