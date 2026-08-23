using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.MVVM.Abstractions;
using OutWit.Common.MVVM.Interfaces;
using OutWit.Common.MVVM.Navigation.Utils;
using OutWit.Common.MVVM.Navigation.WPF.Interfaces;
using OutWit.Common.MVVM.Navigation.WPF.Utils;

namespace OutWit.Common.MVVM.Navigation.WPF.Tests.Mock
{
    /// <summary>
    /// Builds a container the way a WPF application would, with DispatcherImmediate so the
    /// STA test thread stays synchronous, and pumps the WPF dispatcher when a test needs it.
    /// </summary>
    public static class WpfTestHost
    {
        #region Functions

        public static ServiceProvider Build(Action<NavigationBuilder>? configure = null,
                                            Action<WpfNavigationOptions>? configureWpf = null,
                                            Window? window = null,
                                            Action<IServiceCollection>? services = null)
        {
            var collection = new ServiceCollection();

            collection.AddSingleton<ViewDependency>();
            collection.AddNavigation(configure);
            collection.AddWpfNavigation(configureWpf);
            collection.AddSingleton<IDispatcher>(DispatcherImmediate.Instance);
            collection.AddSingleton<ITopLevelProvider>(new FixedTopLevelProvider(window));

            services?.Invoke(collection);

            return collection.BuildServiceProvider();
        }

        /// <summary>
        /// Runs queued dispatcher work until the condition holds or the timeout passes.
        /// </summary>
        public static void PumpUntil(Func<bool> condition, int timeoutMilliseconds = 5000)
        {
            var watch = Stopwatch.StartNew();

            while (!condition() && watch.ElapsedMilliseconds < timeoutMilliseconds)
                DoEvents();
        }

        /// <summary>
        /// Runs everything currently queued on the dispatcher.
        /// </summary>
        public static void DoEvents()
        {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
            Dispatcher.PushFrame(frame);
        }

        /// <summary>
        /// Makes async continuations of the calling STA thread come back to its dispatcher.
        /// </summary>
        public static void InstallSynchronizationContext()
        {
            if (SynchronizationContext.Current is not DispatcherSynchronizationContext)
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
        }

        #endregion
    }
}
