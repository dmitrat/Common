using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Aspects;
using OutWit.Common.MVVM.Commands;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Model;
using OutWit.Common.MVVM.Navigation.Sample.Core.Models;
using OutWit.Common.MVVM.ViewModels;

namespace OutWit.Common.MVVM.Navigation.Sample.Core.ViewModels
{
    /// <summary>
    /// One study, opened with a parameter. A Transient route: a fresh instance per
    /// navigation, in its own DI scope, disposed when the next screen arrives. It guards its
    /// own exit — unsaved notes ask before they are thrown away, and the question is a
    /// dialog, which is why <see cref="CanNavigateFromAsync"/> may await one.
    /// </summary>
    public class StudyViewModel : ViewModelBase<ApplicationViewModel>, INavigationAware, INavigationGuard
    {
        #region Constructors

        public StudyViewModel(ApplicationViewModel applicationVm)
            : base(applicationVm)
        {
            InitCommands();
        }

        #endregion

        #region Initialization

        private void InitCommands()
        {
            SaveCommand = new RelayCommand(Save, () => IsDirty);
            BackCommand = new RelayCommandAsync(async () => await ApplicationVm.Navigation.GoBackAsync());
        }

        #endregion

        #region Functions

        private void Save()
        {
            if (Study == null)
                return;

            ApplicationVm.Studies.Save(new Study(Study.Id, Study.Patient, Study.RecordedUtc, Notes));
            IsDirty = false;
            SaveCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region INavigationAware

        public async Task OnNavigatedToAsync(NavigationContext context, CancellationToken cancellation)
        {
            var id = context.Parameters.Get("id", 0);

            Study = await ApplicationVm.Studies.LoadAsync(id, cancellation);
            Notes = Study?.Notes ?? string.Empty;
            IsDirty = false;
        }

        public Task OnNavigatedFromAsync(NavigationContext context, CancellationToken cancellation)
        {
            return Task.CompletedTask;
        }

        #endregion

        #region INavigationGuard

        public Task<bool> CanNavigateToAsync(NavigationContext context, CancellationToken cancellation)
        {
            return Task.FromResult(true);
        }

        public async Task<bool> CanNavigateFromAsync(NavigationContext context, CancellationToken cancellation)
        {
            if (!IsDirty)
                return true;

            var answer = await ApplicationVm.Dialogs.ShowAsync<ConfirmDialogViewModel, bool>(
                new NavigationParameters(
                    ("title", "Unsaved changes"),
                    ("message", $"Study {Study?.Id} has notes you have not saved. Leave anyway?")),
                cancellation: cancellation);

            return answer.IsConfirmed && answer.Value;
        }

        #endregion

        #region Properties

        [Notify]
        public Study? Study { get; set; }

        [Notify(NotifyAlso = nameof(IsDirty))]
        public string Notes { get; set; } = string.Empty;

        [Notify]
        public bool IsDirty { get; set; }

        #endregion

        #region Commands

        public RelayCommand SaveCommand { get; private set; } = null!;

        public RelayCommandAsync BackCommand { get; private set; } = null!;

        #endregion
    }
}
