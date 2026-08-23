using System;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Model;

namespace OutWit.Common.MVVM.Navigation.Tests.Mock
{
    /// <summary>
    /// A view model for Transient routes: takes a scoped dependency so tests can see the
    /// scope being created per navigation and disposed with the instance.
    /// </summary>
    public sealed class TransientViewModel : INavigationGuard, IDisposable
    {
        #region Fields

        private static int s_instances;

        #endregion

        #region Constructors

        public TransientViewModel(CallLog log, ScopedDependency dependency)
        {
            Log = log;
            Dependency = dependency;
            Id = Interlocked.Increment(ref s_instances);
            log.Add($"Transient#{Id}.Created");
        }

        #endregion

        #region INavigationGuard

        public Task<bool> CanNavigateToAsync(NavigationContext context, CancellationToken cancellation)
        {
            return Task.FromResult(CanNavigateTo);
        }

        public Task<bool> CanNavigateFromAsync(NavigationContext context, CancellationToken cancellation)
        {
            return Task.FromResult(true);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Log.Add($"Transient#{Id}.Disposed");
            IsDisposed = true;
        }

        #endregion

        #region Properties

        public static bool CanNavigateTo { get; set; } = true;

        public CallLog Log { get; }

        public ScopedDependency Dependency { get; }

        public int Id { get; }

        public bool IsDisposed { get; private set; }

        #endregion
    }
}
