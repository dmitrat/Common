using System;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Model;

namespace OutWit.Common.MVVM.Navigation.Tests.Mock
{
    /// <summary>
    /// A global guard registered through nav.AddGuard. Records "Global.From" / "Global.To".
    /// </summary>
    public sealed class GlobalGuard : INavigationGuard
    {
        #region Constructors

        public GlobalGuard(CallLog log)
        {
            Log = log;
        }

        #endregion

        #region INavigationGuard

        public Task<bool> CanNavigateToAsync(NavigationContext context, CancellationToken cancellation)
        {
            Log.Add("Global.To");
            return Task.FromResult(AllowTo(context));
        }

        public Task<bool> CanNavigateFromAsync(NavigationContext context, CancellationToken cancellation)
        {
            Log.Add("Global.From");
            return Task.FromResult(AllowFrom(context));
        }

        #endregion

        #region Properties

        public CallLog Log { get; }

        public static Func<NavigationContext, bool> AllowTo { get; set; } = _ => true;

        public static Func<NavigationContext, bool> AllowFrom { get; set; } = _ => true;

        #endregion
    }
}
