using System.Collections.Generic;
using System.Linq;
using OutWit.Common.MVVM.ViewModels;
using OutWit.Common.MVVM.WPF.Commands;
using OutWit.Common.Settings.Collections;

namespace OutWit.Common.Settings.Samples.Wpf.ViewModels
{
    /// <summary>
    /// ViewModel for a single settings group (displayed as a tab).
    /// </summary>
    public sealed class SettingsGroupViewModel : ViewModelBase<ApplicationViewModel>
    {
        #region Fields

        private readonly SettingsCollection m_collection;

        #endregion

        #region Constructors

        public SettingsGroupViewModel(ApplicationViewModel appVm, SettingsCollection collection)
            : base(appVm)
        {
            m_collection = collection;

            VisibleValues = collection
                .Where(v => !v.Hidden)
                .Select(v => new SettingsValueViewModel(appVm, v))
                .ToList();

            InitCommands();
        }

        #endregion

        #region Initialization

        private void InitCommands()
        {
            ResetAllCommand = new DelegateCommand(
                _ => ResetAll(),
                _ => VisibleValues.Any(v => !v.IsDefault));
        }

        #endregion

        #region Functions

        private void ResetAll()
        {
            foreach (var value in VisibleValues)
                value.Reset();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the display name of this group.
        /// </summary>
        public string DisplayName => m_collection.DisplayName;

        /// <summary>
        /// Gets the group key.
        /// </summary>
        public string Group => m_collection.Group;

        /// <summary>
        /// Gets the display priority.
        /// </summary>
        public int Priority => m_collection.Priority;

        /// <summary>
        /// Gets the list of visible (non-hidden) settings value ViewModels.
        /// </summary>
        public List<SettingsValueViewModel> VisibleValues { get; }

        #endregion

        #region Commands

        /// <summary>
        /// Resets all values in this group to their defaults.
        /// </summary>
        public DelegateCommand ResetAllCommand { get; private set; } = null!;

        #endregion
    }
}
