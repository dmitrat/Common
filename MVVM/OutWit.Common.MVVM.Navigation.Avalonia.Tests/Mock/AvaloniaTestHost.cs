using System;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.MVVM.Abstractions;
using OutWit.Common.MVVM.Interfaces;
using OutWit.Common.MVVM.Navigation.Avalonia.Interfaces;
using OutWit.Common.MVVM.Navigation.Avalonia.Utils;
using OutWit.Common.MVVM.Navigation.Utils;

namespace OutWit.Common.MVVM.Navigation.Avalonia.Tests.Mock
{
    /// <summary>
    /// Builds a container the way an Avalonia application would. The dispatcher is swapped
    /// for DispatcherImmediate: headless tests already run on the UI thread, and an inline
    /// dispatcher keeps them synchronous.
    /// </summary>
    public static class AvaloniaTestHost
    {
        #region Functions

        public static ServiceProvider Build(Action<NavigationBuilder>? configure = null,
                                            Action<AvaloniaNavigationOptions>? configureAvalonia = null,
                                            TopLevel? topLevel = null,
                                            Action<IServiceCollection>? services = null)
        {
            var collection = new ServiceCollection();

            collection.AddSingleton<ViewDependency>();
            collection.AddNavigation(configure);
            collection.AddAvaloniaNavigation(configureAvalonia);
            collection.AddSingleton<IDispatcher>(DispatcherImmediate.Instance);
            collection.AddSingleton<ITopLevelProvider>(new FixedTopLevelProvider(topLevel));

            services?.Invoke(collection);

            return collection.BuildServiceProvider();
        }

        #endregion
    }
}
