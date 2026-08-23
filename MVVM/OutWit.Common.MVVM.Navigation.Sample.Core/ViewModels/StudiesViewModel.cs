using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Aspects;
using OutWit.Common.MVVM.Commands;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Model;
using OutWit.Common.MVVM.Navigation.Sample.Core.Models;
using OutWit.Common.MVVM.ViewModels;

namespace OutWit.Common.MVVM.Navigation.Sample.Core.ViewModels
{
    /// <summary>
    /// The list. A Cached route: one instance, kept for the life of the outlet, so coming
    /// back to it is instant and — with the platform's NavigationOutlet control — the scroll
    /// position is still where the user left it.
    /// </summary>
    public class StudiesViewModel : ViewModelBase<ApplicationViewModel>, INavigationAware
    {
        #region Constructors

        public StudiesViewModel(ApplicationViewModel applicationVm)
            : base(applicationVm)
        {
            InitCommands();
        }

        #endregion

        #region Initialization

        private void InitCommands()
        {
            OpenCommand = new RelayCommandAsync<Study>(OpenAsync, study => study != null);
        }

        #endregion

        #region Functions

        private Task OpenAsync(Study? study)
        {
            if (study == null)
                return Task.CompletedTask;

            return ApplicationVm.Navigation.NavigateAsync(Routes.STUDY, new NavigationParameters(("id", study.Id)));
        }

        #endregion

        #region INavigationAware

        public async Task OnNavigatedToAsync(NavigationContext context, CancellationToken cancellation)
        {
            // A Cached route keeps the instance, but OnNavigatedTo still runs on every arrival —
            // what to do about that is the screen's decision. This one keeps what it has and
            // reloads only when asked, which is what Refresh is for.
            if (Studies.Count > 0 && context.Kind != NavigationKind.Refresh)
                return;

            // The screen is already on screen when this runs, and the navigation bar is live:
            // the outlet was released at the commit. Leaving mid-load cancels the token.
            IsLoading = true;

            try
            {
                Studies = await ApplicationVm.Studies.LoadAllAsync(cancellation);
                LoadCount++;
            }
            finally
            {
                IsLoading = false;
            }
        }

        public Task OnNavigatedFromAsync(NavigationContext context, CancellationToken cancellation)
        {
            return Task.CompletedTask;
        }

        #endregion

        #region Properties

        [Notify]
        public IReadOnlyList<Study> Studies { get; set; } = new List<Study>();

        [Notify]
        public bool IsLoading { get; set; }

        /// <summary>
        /// How often the list actually reloaded — the sample shows it, so that "Cached keeps
        /// the instance" and "Refresh reloads it" are visible rather than claimed.
        /// </summary>
        [Notify]
        public int LoadCount { get; set; }

        #endregion

        #region Commands

        public RelayCommandAsync<Study> OpenCommand { get; private set; } = null!;

        #endregion
    }
}
