using System.Threading;
using OutWit.Common.MVVM.Navigation.Model;

namespace OutWit.Common.MVVM.Navigation.Services
{
    /// <summary>
    /// Everything one pipeline run needs. HistoryIndex is the journal index to move to
    /// for Back/Forward/Refresh and is ignored for New.
    /// </summary>
    internal sealed record NavigationRequest(NavigationOutlet Outlet,
                                             NavigationRoute Route,
                                             NavigationParameters Parameters,
                                             NavigationKind Kind,
                                             int HistoryIndex,
                                             CancellationToken Cancellation);
}
