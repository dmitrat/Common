using System.Collections.Generic;
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
    /// The list. A Cached route: one instance, kept for the life of the outlet, so coming
    /// back to it is instant and — with the platform's NavigationOutlet control — the scroll
    /// position is still where the user left it.
    /// </summary>
    public class StudiesViewModel : ViewModelBase<ApplicationViewModel>, INavigationAware
    {
        #region Constructors

        public StudiesViewModel(ApplicationViewModel applicationVm)
            : base(applicationVm)
        {
            InitCommands();
        }

        #endregion

        #region Initialization

        private void InitCommands()
        {
            OpenCommand = new RelayCommandAsync<Study>(OpenAsync, study => study != null);
            ImportCommand = new RelayCommandAsync(ImportAsync);
        }

        #endregion

        #region Functions

        private Task OpenAsync(Study? study)
        {
            if (study == null)
                return Task.CompletedTask;

            return ApplicationVm.Navigation.NavigateAsync(Routes.STUDY, new NavigationParameters(("id", study.Id)));
        }

        /// <summary>
        /// A long operation behind a progress dialog. The dialog only appears because the work
        /// outlasts the delay; it reports as it goes, and Cancel stops it. Nothing here knows
        /// whether the dialog is a window or an overlay.
        /// </summary>
        private async Task ImportAsync()
        {
            var result = await ApplicationVm.Progress.RunAsync(async (reporter, cancellation) =>
            {
                var imported = 0;

                for (var step = 1; step <= 20; step++)
                {
                    cancellation.ThrowIfCancellationRequested();

                    await Task.Delay(250, cancellation);

                    imported++;
                    reporter.Report($"Importing study {step} of 20…", step / 20d);
                }

                return imported;
            }, new ProgressOptions { Title = "Import", Status = "Preparing…" });

            ImportSummary = result.IsCompleted
                ? $"imported {result.Value} studies"
                : result.IsCancelled
                    ? "import cancelled"
                    : $"import failed: {result.Error?.Message}";
        }

        #endregion

        #region INavigationAware

        public async Task OnNavigatedToAsync(NavigationContext context, CancellationToken cancellation)
        {
            // A Cached route keeps the instance, but OnNavigatedTo still runs on every arrival —
            // what to do about that is the screen's decision. This one keeps what it has and
            // reloads only when asked, which is what Refresh is for.
            if (Studies.Count > 0 && context.Kind != NavigationKind.Refresh)
                return;

            // The screen is already on screen when this runs, and the navigation bar is live:
            // the outlet was released at the commit. Leaving mid-load cancels the token.
            IsLoading = true;

            try
            {
                Studies = await ApplicationVm.Studies.LoadAllAsync(cancellation);
                LoadCount++;
            }
            finally
            {
                IsLoading = false;
            }
        }

        public Task OnNavigatedFromAsync(NavigationContext context, CancellationToken cancellation)
        {
            return Task.CompletedTask;
        }

        #endregion

        #region Properties

        [Notify]
        public IReadOnlyList<Study> Studies { get; set; } = new List<Study>();

        [Notify]
        public bool IsLoading { get; set; }

        /// <summary>
        /// How often the list actually reloaded — the sample shows it, so that "Cached keeps
        /// the instance" and "Refresh reloads it" are visible rather than claimed.
        /// </summary>
        [Notify]
        public int LoadCount { get; set; }

        /// <summary>
        /// What the last import came to — the progress dialog returns a result rather than
        /// throwing, so this is the whole error handling the screen needs.
        /// </summary>
        [Notify]
        public string? ImportSummary { get; set; }

        #endregion

        #region Commands

        public RelayCommandAsync<Study> OpenCommand { get; private set; } = null!;

        public RelayCommandAsync ImportCommand { get; private set; } = null!;

        #endregion
    }
}
