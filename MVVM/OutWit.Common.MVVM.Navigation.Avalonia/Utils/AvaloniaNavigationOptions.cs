using System;
using OutWit.Common.MVVM.Navigation.Avalonia.Dialogs;
using OutWit.Common.MVVM.Navigation.Model;
using OutWit.Common.MVVM.Navigation.Interfaces;

namespace OutWit.Common.MVVM.Navigation.Avalonia.Utils
{
    /// <summary>
    /// What <c>services.AddAvaloniaNavigation(o => ...)</c> hands the application.
    /// </summary>
    public sealed class AvaloniaNavigationOptions
    {
        #region Functions

        /// <summary>
        /// Show dialogs as modal windows (the default). Dialogs nest.
        /// </summary>
        /// <returns>These options.</returns>
        public AvaloniaNavigationOptions UseWindowDialogs()
        {
            DialogHostType = typeof(DialogHostWindow);
            return this;
        }

        /// <summary>
        /// Show dialogs on the overlay layer of the active window. Dialogs do not nest.
        /// </summary>
        /// <returns>These options.</returns>
        public AvaloniaNavigationOptions UseOverlayDialogs()
        {
            DialogHostType = typeof(DialogHostOverlay);
            return this;
        }

        /// <summary>
        /// Show dialogs through a custom host — an adapter over DialogHost.Avalonia, for instance.
        /// </summary>
        /// <typeparam name="THost">The host type; resolved from DI as a singleton.</typeparam>
        /// <returns>These options.</returns>
        public AvaloniaNavigationOptions UseDialogHost<THost>()
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
