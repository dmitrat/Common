using System.Collections.Generic;
using OutWit.Common.MVVM.ViewModels;
using OutWit.Common.Settings.Interfaces;

namespace OutWit.Common.Settings.Samples.Wpf.ViewModels
{
    /// <summary>
    /// Root ViewModel containing all child ViewModels.
    /// </summary>
    public sealed class ApplicationViewModel : ViewModelBase<ApplicationViewModel>
    {
        #region Constructors

        public ApplicationViewModel(List<ISettingsManager> managers)
            : base(null!)
        {
            Settings = new SettingsViewModel(this, managers);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the settings ViewModel that aggregates all module collections.
        /// </summary>
        public SettingsViewModel Settings { get; }

        #endregion
    }
}
