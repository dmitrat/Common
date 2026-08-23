using System;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Model;

namespace OutWit.Common.MVVM.Navigation.Tests.Mock
{
    /// <summary>
    /// A configurable INavigationAware + INavigationGuard + IDisposable view model. Every
    /// call is recorded as "{Name}.{What}" in the shared <see cref="CallLog"/>; gates let a
    /// test hold a step open to exercise preemption and cancellation.
    /// </summary>
    public class AwareViewModel : INavigationAware, INavigationGuard, IDisposable
    {
        #region Fields

        private static int s_instances;

        #endregion

        #region Constructors

        public AwareViewModel(CallLog log, StalledTargetGateHolder? gates = null)
        {
            Log = log;
            Id = Interlocked.Increment(ref s_instances);
            Name = GetType().Name.Replace("ViewModel", string.Empty);
            log.Add($"{Name}.Created");

            var stallOnNavigatedTo = false;
            var gate = gates?.Take(out stallOnNavigatedTo);
            if (gate == null)
                return;

            if (stallOnNavigatedTo)
                OnNavigatedToGate = gate;
            else
                CanNavigateToGate = gate;
        }

        #endregion

        #region INavigationGuard

        public async Task<bool> CanNavigateToAsync(NavigationContext context, CancellationToken cancellation)
        {
            Log.Add($"{Name}.CanTo");

            if (CanNavigateToGate != null)
                await CanNavigateToGate.Task;

            return CanNavigateTo(context);
        }

        public async Task<bool> CanNavigateFromAsync(NavigationContext context, CancellationToken cancellation)
        {
            Log.Add($"{Name}.CanFrom");

            if (CanNavigateFromGate != null)
                await CanNavigateFromGate.Task;

            return CanNavigateFrom(context);
        }

        #endregion

        #region INavigationAware

        public async Task OnNavigatedToAsync(NavigationContext context, CancellationToken cancellation)
        {
            Log.Add($"{Name}.To");
            LastContext = context;
            NavigatedToCount++;

            if (OnNavigatedToGate != null)
                await OnNavigatedToGate.Task;

            if (ThrowOnNavigatedTo != null)
                throw ThrowOnNavigatedTo;

            cancellation.ThrowIfCancellationRequested();
        }

        public Task OnNavigatedFromAsync(NavigationContext context, CancellationToken cancellation)
        {
            Log.Add($"{Name}.From");
            NavigatedFromCount++;

            if (ThrowOnNavigatedFrom != null)
                throw ThrowOnNavigatedFrom;

            return Task.CompletedTask;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Log.Add($"{Name}.Disposed");
            IsDisposed = true;
        }

        #endregion

        #region Properties

        public CallLog Log { get; }

        public int Id { get; }

        public string Name { get; set; }

        public Func<NavigationContext, bool> CanNavigateTo { get; set; } = _ => true;

        public Func<NavigationContext, bool> CanNavigateFrom { get; set; } = _ => true;

        public TaskCompletionSource<bool>? CanNavigateToGate { get; set; }

        public TaskCompletionSource<bool>? CanNavigateFromGate { get; set; }

        public TaskCompletionSource<bool>? OnNavigatedToGate { get; set; }

        public Exception? ThrowOnNavigatedTo { get; set; }

        public Exception? ThrowOnNavigatedFrom { get; set; }

        public NavigationContext? LastContext { get; private set; }

        public int NavigatedToCount { get; private set; }

        public int NavigatedFromCount { get; private set; }

        public bool IsDisposed { get; private set; }

        #endregion
    }
}
