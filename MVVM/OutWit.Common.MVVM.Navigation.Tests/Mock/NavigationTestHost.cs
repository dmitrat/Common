using System;
using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.MVVM.Interfaces;
using OutWit.Common.MVVM.Navigation.Utils;

namespace OutWit.Common.MVVM.Navigation.Tests.Mock
{
    /// <summary>
    /// Builds a container the way an application would, plus the test doubles.
    /// </summary>
    public static class NavigationTestHost
    {
        #region Functions

        public static ServiceProvider Build(Action<NavigationBuilder>? configure = null,
                                            Action<IServiceCollection>? services = null,
                                            IDispatcher? dispatcher = null)
        {
            var collection = new ServiceCollection();

            collection.AddSingleton<CallLog>();
            collection.AddSingleton<StalledTargetGateHolder>();
            collection.AddScoped<ScopedDependency>();
            collection.AddNavigation(configure);

            // a platform package registers its dispatcher after AddNavigation; the last
            // registration wins over the TryAdd'ed DispatcherImmediate
            if (dispatcher != null)
                collection.AddSingleton(dispatcher);

            services?.Invoke(collection);

            return collection.BuildServiceProvider();
        }

        #endregion
    }
}
