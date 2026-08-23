using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.MVVM.Navigation.Model;

namespace OutWit.Common.MVVM.Navigation.Services
{
    /// <summary>
    /// A view model instance together with the DI scope it was created in (Transient
    /// routes only). Disposing the handle disposes the view model first, then the scope.
    /// </summary>
    internal sealed class NavigationViewModelHandle
    {
        #region Constructors

        public NavigationViewModelHandle(NavigationRoute route, object viewModel, IServiceScope? scope)
        {
            Route = route ?? throw new ArgumentNullException(nameof(route));
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            Scope = scope;
        }

        #endregion

        #region Functions

        public async ValueTask DisposeAsync()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;

            try
            {
                if (ViewModel is IAsyncDisposable asyncDisposable)
                    await asyncDisposable.DisposeAsync();
                else if (ViewModel is IDisposable disposable)
                    disposable.Dispose();
            }
            finally
            {
                Scope?.Dispose();
            }
        }

        #endregion

        #region Properties

        public NavigationRoute Route { get; }

        public object ViewModel { get; }

        public IServiceScope? Scope { get; }

        public bool IsDisposed { get; private set; }

        #endregion
    }
}
