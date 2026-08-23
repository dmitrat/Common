using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Aspects;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Model;
using OutWit.Common.MVVM.Navigation.Sample.Core.ViewModels;
using OutWit.Common.MVVM.ViewModels;

namespace OutWit.Common.MVVM.Navigation.Sample.Module.Avalonia
{
    /// <summary>
    /// A screen that arrives with the module DLL. It takes the application's root view model
    /// through DI like any other screen — the module shares the host's types because UI
    /// modules are loaded into the default assembly context, not an isolated one.
    /// </summary>
    public class AuditViewModel : ViewModelBase<ApplicationViewModel>, INavigationAware
    {
        #region Constructors

        public AuditViewModel(ApplicationViewModel applicationVm)
            : base(applicationVm)
        {
            Assembly = GetType().Assembly.GetName().Name ?? "?";
            Location = GetType().Assembly.Location;
        }

        #endregion

        #region INavigationAware

        public Task OnNavigatedToAsync(NavigationContext context, CancellationToken cancellation)
        {
            Entries = new[]
            {
                $"loaded from {Location}",
                $"host sees {ApplicationVm.Navigation.Outlets.Count} outlet(s)",
                $"opened {DateTime.UtcNow:HH:mm:ss} UTC"
            };

            return Task.CompletedTask;
        }

        public Task OnNavigatedFromAsync(NavigationContext context, CancellationToken cancellation)
        {
            return Task.CompletedTask;
        }

        #endregion

        #region Properties

        /// <summary>
        /// The assembly this screen came from — the proof that it is not the application's.
        /// </summary>
        public string Assembly { get; }

        /// <summary>
        /// Where on disk the loader found it: the @Modules folder, not the application's own.
        /// </summary>
        public string Location { get; }

        [Notify]
        public IReadOnlyList<string> Entries { get; set; } = Array.Empty<string>();

        #endregion
    }
}
