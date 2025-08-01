using System;
using Microsoft.Extensions.DependencyInjection;

namespace OutWit.Common.Plugins.Abstractions.Interfaces
{
    public interface IWitPlugin : IDisposable
    {
        /// <summary>
        /// Called once when the plugin is first loaded and initialized.
        /// Use for registering services in DI.
        /// </summary>
        void Initialize(IServiceCollection services);

        /// <summary>
        /// Called after all plugins have been initialized.
        /// Use for logic that depends on other plugins being available.
        /// </summary>
        void OnInitialized(IServiceProvider serviceProvider);

        /// <summary>
        /// Called just before the plugin is about to be unloaded.
        /// Use for cleanup, saving state, or releasing resources.
        /// </summary>
        void OnUnloading();
    }
}
