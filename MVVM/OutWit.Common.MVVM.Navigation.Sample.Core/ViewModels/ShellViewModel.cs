using System.ComponentModel;
using OutWit.Common.Aspects;
using OutWit.Common.MVVM.Commands;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.ViewModels;

namespace OutWit.Common.MVVM.Navigation.Sample.Core.ViewModels
{
    /// <summary>
    /// What the window binds to. It owns no screens: it hands the markup the outlet object
    /// and the zones, and the navigation service fills them.
    /// </summary>
    public class ShellViewModel : ViewModelBase<ApplicationViewModel>
    {
        #region Constructors

        public ShellViewModel(ApplicationViewModel applicationVm)
            : base(applicationVm)
        {
            Main = applicationVm.Navigation.Outlet();
            NavigationBar = applicationVm.Contributions.Zone(Zones.NAVIGATION_BAR);
            MenuFile = applicationVm.Contributions.Zone(Zones.MENU_FILE);

            InitCommands();
            InitEvents();
            UpdateStatus();
        }

        #endregion

        #region Initialization

        private void InitCommands()
        {
            BackCommand = new RelayCommandAsync(async () => await ApplicationVm.Navigation.GoBackAsync(), () => CanGoBack);
            ForwardCommand = new RelayCommandAsync(async () => await ApplicationVm.Navigation.GoForwardAsync(), () => CanGoForward);
            RefreshCommand = new RelayCommandAsync(async () => await ApplicationVm.Navigation.RefreshAsync(), () => Main.Content != null);
        }

        private void InitEvents()
        {
            Main.PropertyChanged += OnOutletPropertyChanged;
        }

        #endregion

        #region Tools

        private void UpdateStatus()
        {
            CanGoBack = Main.CanGoBack;
            CanGoForward = Main.CanGoForward;
            IsBusy = Main.IsNavigating;
            Title = Main.RouteKey == null ? "OutWit Navigation Sample" : $"OutWit Navigation Sample — {Main.RouteKey}";

            BackCommand.RaiseCanExecuteChanged();
            ForwardCommand.RaiseCanExecuteChanged();
            RefreshCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region Event Handlers

        private void OnOutletPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            UpdateStatus();
        }

        #endregion

        #region Properties

        /// <summary>
        /// The one outlet this window shows. Bound straight from XAML.
        /// </summary>
        public INavigationOutlet Main { get; }

        /// <summary>
        /// The navigation rail: whatever the shell and the modules put there.
        /// </summary>
        public IContributionZone NavigationBar { get; }

        /// <summary>
        /// The File menu, nested items included.
        /// </summary>
        public IContributionZone MenuFile { get; }

        [Notify]
        public string Title { get; set; } = "OutWit Navigation Sample";

        [Notify]
        public bool IsBusy { get; set; }

        [Notify]
        public bool CanGoBack { get; set; }

        [Notify]
        public bool CanGoForward { get; set; }

        #endregion

        #region Commands

        public RelayCommandAsync BackCommand { get; private set; } = null!;

        public RelayCommandAsync ForwardCommand { get; private set; } = null!;

        public RelayCommandAsync RefreshCommand { get; private set; } = null!;

        #endregion
    }
}
