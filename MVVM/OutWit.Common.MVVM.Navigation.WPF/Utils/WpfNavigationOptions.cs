using System;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Model;
using OutWit.Common.MVVM.Navigation.WPF.Dialogs;

namespace OutWit.Common.MVVM.Navigation.WPF.Utils
{
    /// <summary>
    /// What <c>services.AddWpfNavigation(o => ...)</c> hands the application.
    /// </summary>
    public sealed class WpfNavigationOptions
    {
        #region Functions

        /// <summary>
        /// Show dialogs as modal windows (the default). Dialogs nest.
        /// </summary>
        /// <returns>These options.</returns>
        public WpfNavigationOptions UseWindowDialogs()
        {
            DialogHostType = typeof(DialogHostWindow);
            return this;
        }

        /// <summary>
        /// Show dialogs through a custom host — an adapter over MaterialDesignThemes' DialogHost, for instance.
        /// </summary>
        /// <typeparam name="THost">The host type; resolved from DI as a singleton.</typeparam>
        /// <returns>These options.</returns>
        public WpfNavigationOptions UseDialogHost<THost>()
            where THost : class, IDialogHost
        {
            DialogHostType = typeof(THost);
            return this;
        }

        #endregion

        #region Properties

        /// <summary>
        /// How the view locator finds views that are not in the registry.
        /// </summary>
        public ViewNamingConvention ViewConvention { get; set; } = ViewNamingConvention.ViewModelsToViews;

        /// <summary>
        /// The <see cref="IDialogHost"/> implementation to register.
        /// </summary>
        public Type DialogHostType { get; private set; } = typeof(DialogHostWindow);

        #endregion
    }
}
