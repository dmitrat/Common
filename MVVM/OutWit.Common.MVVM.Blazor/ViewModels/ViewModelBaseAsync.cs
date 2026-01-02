using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using OutWit.Common.MVVM.Interfaces;

namespace OutWit.Common.MVVM.Blazor.ViewModels
{
    /// <summary>
    /// Extended base class for Blazor view models with async lifecycle support.
    /// Adds OnInitializedAsync error handling and async dispose support.
    /// </summary>
    public abstract class ViewModelBaseAsync : ViewModelBase, IAsyncDisposable
    {
        #region Fields

        private string? m_error;
        private bool m_asyncDisposed;

        #endregion

        #region Lifecycle

        protected override async Task OnInitializedAsync()
        {
            try
            {
                await InitializeAsync();
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                OnInitializationError(ex);
            }
        }

        /// <summary>
        /// Override this method to perform async initialization.
        /// Exceptions are caught and stored in the Error property.
        /// </summary>
        protected virtual Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called when an error occurs during initialization.
        /// Override to handle initialization errors.
        /// </summary>
        protected virtual void OnInitializationError(Exception exception)
        {
        }

        #endregion

        #region IAsyncDisposable

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore();
            Dispose(false);
            GC.SuppressFinalize(this);
        }

        protected virtual ValueTask DisposeAsyncCore()
        {
            if (m_asyncDisposed)
                return ValueTask.CompletedTask;

            m_asyncDisposed = true;
            return ValueTask.CompletedTask;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the error message from initialization or other operations.
        /// </summary>
        public string? Error
        {
            get => m_error;
            protected set => SetProperty(ref m_error, value);
        }

        /// <summary>
        /// Gets whether there is an error.
        /// </summary>
        public bool HasError => !string.IsNullOrEmpty(Error);

        #endregion
    }
}
