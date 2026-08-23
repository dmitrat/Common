using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Model;

namespace OutWit.Common.MVVM.Navigation.Sample.Core.Guards
{
    /// <summary>
    /// A global guard: asked about every navigation in every outlet, before the target view
    /// model is even created. One flag here freezes navigation application-wide — a running
    /// computation, a modal workflow, an expired licence. In the Prism-era code this was
    /// LockNavigation/UnlockNavigation on a service locator.
    /// </summary>
    public sealed class BusyGuard : INavigationGuard
    {
        #region INavigationGuard

        public Task<bool> CanNavigateToAsync(NavigationContext context, CancellationToken cancellation)
        {
            return Task.FromResult(true);
        }

        public Task<bool> CanNavigateFromAsync(NavigationContext context, CancellationToken cancellation)
        {
            return Task.FromResult(!IsLocked);
        }

        #endregion

        #region Properties

        /// <summary>
        /// While true, no navigation leaves any screen.
        /// </summary>
        public bool IsLocked { get; set; }

        #endregion
    }
}
