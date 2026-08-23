using System;
using System.Collections.Generic;

namespace OutWit.Common.MVVM.Navigation.Model
{
    /// <summary>
    /// What <c>services.AddNavigation(nav => ...)</c> collects. Registered as a singleton
    /// and read by the registries and the navigation service when they are built.
    /// </summary>
    public sealed class NavigationOptions
    {
        #region Constants

        /// <summary>
        /// Journal depth when the application does not set one.
        /// </summary>
        public const int DEFAULT_HISTORY_DEPTH = 50;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates options with the defaults: the Main outlet, no zones, no routes,
        /// <see cref="DEFAULT_HISTORY_DEPTH"/>.
        /// </summary>
        public NavigationOptions()
        {
            Outlets = new List<string> { NavigationOutlets.MAIN };
            Zones = new List<string>();
            Routes = new List<NavigationRoute>();
            Views = new List<KeyValuePair<Type, Type>>();
            HistoryDepth = DEFAULT_HISTORY_DEPTH;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Outlets created up front. More can be added at run time through
        /// <see cref="Interfaces.INavigationService.AddOutlet"/>.
        /// </summary>
        public List<string> Outlets { get; }

        /// <summary>
        /// Zones created up front. Zones are also created on first use.
        /// </summary>
        public List<string> Zones { get; }

        /// <summary>
        /// Routes registered up front. Modules register more in their OnInitialized.
        /// </summary>
        public List<NavigationRoute> Routes { get; }

        /// <summary>
        /// View model type to view type pairs registered up front.
        /// </summary>
        public List<KeyValuePair<Type, Type>> Views { get; }

        /// <summary>
        /// Maximum journal entries per outlet. Zero disables the journal.
        /// </summary>
        public int HistoryDepth { get; set; }

        #endregion
    }
}
