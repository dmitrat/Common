using System.Diagnostics;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Modules;
using OutWit.Common.MVVM.Navigation.Sample.Core;
using OutWit.Common.MVVM.Navigation.Sample.Core.ViewModels;
using OutWit.Common.MVVM.Navigation.Utils;
using OutWit.Common.MVVM.Navigation.WPF.Sample.Views;
using OutWit.Common.MVVM.Navigation.WPF.Utils;

namespace OutWit.Common.MVVM.Navigation.WPF.Sample
{
    /// <summary>
    /// The composition root. Compare it with the Avalonia sample's App.axaml.cs: the two
    /// differ in one call — <c>AddWpfNavigation</c> against <c>AddAvaloniaNavigation</c> — and
    /// in which views are registered. The routes, the guards, the screens and the module are
    /// the same assembly, untouched.
    /// </summary>
    public partial class App : Application
    {
        #region Application

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();

            services.AddSample();
            services.AddWpfNavigation();

            var provider = services.BuildServiceProvider();

            RegisterViews(provider.GetRequiredService<IViewRegistry>());

            // the locator goes into the application resources, where NavigationOutlet and
            // ViewPresenter find it, and where XAML can name it as a template selector
            provider.UseWpfViewLocator(this);

            await provider.GetRequiredService<UiModules>().InitializeAsync(provider);
            provider.AddSampleContributions();

            provider.ValidateNavigation(throwOnProblems: Debugger.IsAttached);

            MainWindow = new ShellWindow
            {
                DataContext = provider.GetRequiredService<ApplicationViewModel>().Shell
            };

            MainWindow.Show();

            await provider.GetRequiredService<INavigationService>().NavigateAsync(Routes.STUDIES);
        }

        #endregion

        #region Functions

        /// <summary>
        /// The view models live in the shared assembly and the views do not, so the naming
        /// convention — which looks inside the view model's own assembly — cannot pair them.
        /// </summary>
        private static void RegisterViews(IViewRegistry views)
        {
            views.Register<StudiesViewModel, StudiesView>();
            views.Register<StudyViewModel, StudyView>();
            views.Register<SettingsViewModel, SettingsView>();
            views.Register<ReportsViewModel, ReportsView>();
            views.Register<ConfirmDialogViewModel, ConfirmDialogView>();
        }

        #endregion
    }
}
