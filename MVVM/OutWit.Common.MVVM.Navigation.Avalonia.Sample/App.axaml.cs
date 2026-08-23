using System.Diagnostics;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.MVVM.Navigation.Avalonia.Sample.Views;
using OutWit.Common.MVVM.Navigation.Avalonia.Utils;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Modules;
using OutWit.Common.MVVM.Navigation.Sample.Core;
using OutWit.Common.MVVM.Navigation.Sample.Core.ViewModels;
using OutWit.Common.MVVM.Navigation.Utils;

namespace OutWit.Common.MVVM.Navigation.Avalonia.Sample
{
    /// <summary>
    /// The composition root. Everything specific to Avalonia lives in these forty lines: the
    /// platform package, the view registrations and the window. The routes, the guards, the
    /// screens and the module come from the shared sample assembly, unchanged from WPF.
    /// </summary>
    public partial class App : Application
    {
        #region Initialization

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override async void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var services = new ServiceCollection();

                services.AddSample();
                services.AddAvaloniaNavigation(options => options.UseOverlayDialogs());

                var provider = services.BuildServiceProvider();

                RegisterViews(provider.GetRequiredService<IViewRegistry>());

                // the view locator serves every ContentControl — nested content, dialogs, zone
                // widgets; the NavigationOutlet control finds it here too
                provider.UseAvaloniaViewLocator();

                await provider.GetRequiredService<UiModules>().InitializeAsync(provider);
                provider.AddSampleContributions();

                // in Debug this throws on a route with no view or an outlet nobody declared,
                // instead of leaving the user to find it by clicking
                provider.ValidateNavigation(throwOnProblems: Debugger.IsAttached);

                desktop.MainWindow = new ShellWindow
                {
                    DataContext = provider.GetRequiredService<ApplicationViewModel>().Shell
                };

                base.OnFrameworkInitializationCompleted();

                await provider.GetRequiredService<INavigationService>().NavigateAsync(Routes.STUDIES);

                return;
            }

            base.OnFrameworkInitializationCompleted();
        }

        /// <summary>
        /// The view models live in the shared assembly and the views do not, so the naming
        /// convention — which looks inside the view model's own assembly — cannot pair them.
        /// Registering explicitly is the other half of the contract, and the only half that
        /// survives trimming.
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
