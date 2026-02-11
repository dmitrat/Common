using System.Collections.Generic;
using System.Windows;
using OutWit.Common.Settings.Interfaces;
using OutWit.Common.Settings.Samples.Wpf.Module.Csv;
using OutWit.Common.Settings.Samples.Wpf.Module.Database;
using OutWit.Common.Settings.Samples.Wpf.Module.Json;
using OutWit.Common.Settings.Samples.Wpf.Module.SharedDatabase;
using OutWit.Common.Settings.Samples.Wpf.ViewModels;
using OutWit.Common.Settings.Samples.Wpf.Views;

namespace OutWit.Common.Settings.Samples.Wpf
{
    public partial class App : Application
    {
        #region Functions

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var appModule = new ApplicationModule();
            appModule.Initialize();

            var netModule = new NetworkModule();
            netModule.Initialize();

            var advModule = new AdvancedModule();
            advModule.Initialize();

            var sharedModule = new SharedDatabaseModule();
            sharedModule.Initialize();

            var managers = new List<ISettingsManager>
            {
                appModule.Manager,
                netModule.Manager,
                advModule.Manager,
                sharedModule.Manager
            };

            var appVm = new ApplicationViewModel(managers);
            MainWindow = new MainWindow { DataContext = appVm };
            MainWindow.Show();
        }

        #endregion
    }
}
