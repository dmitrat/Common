using System;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Sample.Core.Services;
using OutWit.Common.MVVM.ViewModels;

namespace OutWit.Common.MVVM.Navigation.Sample.Core.ViewModels
{
    /// <summary>
    /// The root view model every other one hangs off, as in the house style. With navigation
    /// in play it no longer constructs the screens — the navigation service does that, from
    /// DI — so what stays here is the shared state and the services the screens reach for.
    /// </summary>
    public class ApplicationViewModel : ViewModelBase<ApplicationViewModel>
    {
        #region Constructors

        public ApplicationViewModel(INavigationService navigation,
                                    IContributionRegistry contributions,
                                    IDialogService dialogs,
                                    IProgressDialogService progress,
                                    StudyStore studies)
            : base(null!)
        {
            Navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            Contributions = contributions ?? throw new ArgumentNullException(nameof(contributions));
            Dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
            Progress = progress ?? throw new ArgumentNullException(nameof(progress));
            Studies = studies ?? throw new ArgumentNullException(nameof(studies));

            Shell = new ShellViewModel(this);
        }

        #endregion

        #region Properties

        /// <summary>
        /// The window's view model: outlets, zones, Back/Forward.
        /// </summary>
        public ShellViewModel Shell { get; }

        #endregion

        #region Services

        public INavigationService Navigation { get; }

        public IContributionRegistry Contributions { get; }

        public IDialogService Dialogs { get; }

        public IProgressDialogService Progress { get; }

        public StudyStore Studies { get; }

        #endregion
    }
}
