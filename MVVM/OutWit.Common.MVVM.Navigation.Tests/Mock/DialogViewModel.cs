using System;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Model;

namespace OutWit.Common.MVVM.Navigation.Tests.Mock
{
    /// <summary>
    /// A dialog view model with a string result. Created either by the test or by the
    /// dialog service through ActivatorUtilities (then it takes the scoped dependency).
    /// </summary>
    public sealed class DialogViewModel : IDialogAware<string>, IDisposable
    {
        #region Events

        public event DialogCloseRequestedEventHandler<string>? CloseRequested;

        #endregion

        #region Constructors

        public DialogViewModel(ScopedDependency? dependency = null)
        {
            Dependency = dependency;
        }

        #endregion

        #region IDialogAware

        public Task OnOpenedAsync(NavigationParameters parameters, CancellationToken cancellation)
        {
            OpenedWith = parameters;

            if (ThrowOnOpened != null)
                throw ThrowOnOpened;

            return Task.CompletedTask;
        }

        public Task<bool> CanCloseAsync(DialogResult<string> result, CancellationToken cancellation)
        {
            CanCloseCalls++;
            return Task.FromResult(CanClose);
        }

        #endregion

        #region Functions

        public void RequestClose(DialogResult<string> result)
        {
            CloseRequested?.Invoke(result);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            IsDisposed = true;
        }

        #endregion

        #region Properties

        public ScopedDependency? Dependency { get; }

        public NavigationParameters? OpenedWith { get; private set; }

        public bool CanClose { get; set; } = true;

        public int CanCloseCalls { get; private set; }

        public Exception? ThrowOnOpened { get; set; }

        public bool IsDisposed { get; private set; }

        #endregion
    }
}
