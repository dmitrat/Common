using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Aspects;
using OutWit.Common.MVVM.Commands;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Model;
using OutWit.Common.MVVM.ViewModels;

namespace OutWit.Common.MVVM.Navigation.Sample.Core.ViewModels
{
    /// <summary>
    /// Settings, and the switch that locks navigation: it flips a flag the global
    /// <see cref="Guards.BusyGuard"/> reads, so every route in the application refuses to move
    /// while it is on. That is what replaces Prism-era LockNavigation/UnlockNavigation.
    /// </summary>
    public class SettingsViewModel : ViewModelBase<ApplicationViewModel>, INavigationAware
    {
        #region Fields

        private readonly Guards.BusyGuard m_busy;

        #endregion

        #region Constructors

        public SettingsViewModel(ApplicationViewModel applicationVm, Guards.BusyGuard busy)
            : base(applicationVm)
        {
            m_busy = busy;

            InitCommands();
        }

        #endregion

        #region Initialization

        private void InitCommands()
        {
            ToggleLockCommand = new RelayCommand(() =>
            {
                m_busy.IsLocked = !m_busy.IsLocked;
                IsNavigationLocked = m_busy.IsLocked;
            });
        }

        #endregion

        #region INavigationAware

        public Task OnNavigatedToAsync(NavigationContext context, CancellationToken cancellation)
        {
            IsNavigationLocked = m_busy.IsLocked;
            VisitCount++;

            return Task.CompletedTask;
        }

        public Task OnNavigatedFromAsync(NavigationContext context, CancellationToken cancellation)
        {
            return Task.CompletedTask;
        }

        #endregion

        #region Properties

        [Notify]
        public bool IsNavigationLocked { get; set; }

        /// <summary>
        /// Rises on every arrival, while the instance stays the same: a Cached route.
        /// </summary>
        [Notify]
        public int VisitCount { get; set; }

        #endregion

        #region Commands

        public RelayCommand ToggleLockCommand { get; private set; } = null!;

        #endregion
    }
}
