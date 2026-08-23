using System;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Aspects;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Model;
using OutWit.Common.MVVM.ViewModels;

namespace OutWit.Common.MVVM.Navigation.Sample.Core.ViewModels
{
    /// <summary>
    /// A screen that belongs to the Reports module, not to the shell. The shell's markup
    /// never mentions it: the module registered its route, its view and its place in the
    /// navigation bar.
    /// </summary>
    public class ReportsViewModel : ViewModelBase<ApplicationViewModel>, INavigationAware
    {
        #region Constructors

        public ReportsViewModel(ApplicationViewModel applicationVm)
            : base(applicationVm)
        {
        }

        #endregion

        #region INavigationAware

        public Task OnNavigatedToAsync(NavigationContext context, CancellationToken cancellation)
        {
            GeneratedUtc = DateTime.UtcNow;

            return Task.CompletedTask;
        }

        public Task OnNavigatedFromAsync(NavigationContext context, CancellationToken cancellation)
        {
            return Task.CompletedTask;
        }

        #endregion

        #region Properties

        [Notify]
        public DateTime GeneratedUtc { get; set; }

        #endregion
    }
}
