using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using OutWit.Common.MVVM.ViewModels;
using OutWit.Common.MVVM.WPF.Commands;
using OutWit.Common.Settings.Interfaces;

namespace OutWit.Common.Settings.Samples.Wpf.ViewModels
{
    /// <summary>
    /// Aggregates settings collections from all module managers
    /// into a sorted list of group ViewModels.
    /// </summary>
    public sealed class SettingsViewModel : ViewModelBase<ApplicationViewModel>
    {
        #region Fields

        private readonly List<ISettingsManager> m_managers;
        private List<SettingsGroupViewModel> m_groups = new();
        private bool m_hasChanges;

        #endregion

        #region Constructors

        public SettingsViewModel(ApplicationViewModel appVm, List<ISettingsManager> managers)
            : base(appVm)
        {
            m_managers = managers;

            InitCommands();
            Rebuild();
        }

        #endregion

        #region Initialization

        private void InitCommands()
        {
            SaveCommand = new DelegateCommand(_ => Save(), _ => HasChanges);
            CancelCommand = new DelegateCommand(_ => Cancel());
        }

        #endregion

        #region Functions

        /// <summary>
        /// Rebuilds group ViewModels from all managers' collections.
        /// </summary>
        public void Rebuild()
        {
            foreach (var group in m_groups)
                group.Dispose();

            Groups = m_managers
                .SelectMany(m => m.Collections)
                .OrderBy(c => c.Priority)
                .Select(c => new SettingsGroupViewModel(ApplicationVm, c))
                .Where(g => g.VisibleValues.Count > 0)
                .ToList();

            foreach (var group in Groups)
            {
                foreach (var val in group.VisibleValues)
                    val.PropertyChanged += OnValuePropertyChanged;
            }

            HasChanges = false;
        }

        private void Save()
        {
            foreach (var manager in m_managers)
                manager.Save();

            HasChanges = false;
        }

        private void Cancel()
        {
            foreach (var group in m_groups)
            {
                foreach (var val in group.VisibleValues)
                    val.PropertyChanged -= OnValuePropertyChanged;
            }

            foreach (var manager in m_managers)
                manager.Load();

            Rebuild();
        }

        #endregion

        #region Event Handlers

        private void OnValuePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SettingsValueViewModel.Value))
                HasChanges = true;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the list of visible setting groups sorted by priority.
        /// </summary>
        public List<SettingsGroupViewModel> Groups
        {
            get => m_groups;
            private set
            {
                m_groups = value;
                OnPropertyChanged(nameof(Groups));
            }
        }

        /// <summary>
        /// Gets whether any setting values have been modified.
        /// </summary>
        public bool HasChanges
        {
            get => m_hasChanges;
            private set
            {
                m_hasChanges = value;
                OnPropertyChanged(nameof(HasChanges));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        #endregion

        #region Commands

        /// <summary>
        /// Saves all settings to their respective providers.
        /// </summary>
        public DelegateCommand SaveCommand { get; private set; } = null!;

        /// <summary>
        /// Cancels all changes by reloading from providers.
        /// </summary>
        public DelegateCommand CancelCommand { get; private set; } = null!;

        #endregion
    }
}
