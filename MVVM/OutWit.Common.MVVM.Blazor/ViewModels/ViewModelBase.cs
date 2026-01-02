using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using OutWit.Common.MVVM.Interfaces;

namespace OutWit.Common.MVVM.Blazor.ViewModels
{
    /// <summary>
    /// Base class for Blazor view models that combines ComponentBase with INotifyPropertyChanged.
    /// Provides common functionality for busy state management, error handling, and property change notifications.
    /// </summary>
    public abstract class ViewModelBase : ComponentBase, IViewModelBase, IDisposable
    {
        #region Events

        public event PropertyChangedEventHandler? PropertyChanged;

        #endregion

        #region Fields

        private bool m_busy;
        private bool m_disposed;

        #endregion

        #region Constructors

        protected ViewModelBase()
        {
            InitEvents();
        }

        #endregion

        #region Initialization

        private void InitEvents()
        {
            PropertyChanged += OnPropertyChangedInternal;
        }

        #endregion

        #region Functions

        /// <summary>
        /// Raises PropertyChanged event for the specified property.
        /// </summary>
        protected void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Sets a property value and raises PropertyChanged if the value changed.
        /// </summary>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            RaisePropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Executes an action and returns a default value on error.
        /// </summary>
        protected TResult? Check<TResult>(Func<TResult> action, TResult? onError = default)
        {
            try
            {
                return action();
            }
            catch (Exception)
            {
                return onError;
            }
        }

        /// <summary>
        /// Executes a boolean action and returns false on error.
        /// </summary>
        protected bool Check(Func<bool> action)
        {
            try
            {
                return action();
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Executes an action and swallows exceptions.
        /// </summary>
        protected void Check(Action action)
        {
            try
            {
                action();
            }
            catch (Exception)
            {
                // Swallow exception
            }
        }

        /// <summary>
        /// Runs an async operation with busy state management.
        /// Automatically sets Busy=true before execution and Busy=false after completion.
        /// </summary>
        protected async Task RunAsync(Func<Task> body)
        {
            try
            {
                Busy = true;
                await body();
            }
            finally
            {
                Busy = false;
                StateHasChanged();
            }
        }

        /// <summary>
        /// Runs an async operation with busy state management and returns a result.
        /// </summary>
        protected async Task<TResult?> RunAsync<TResult>(Func<Task<TResult>> body, TResult? onError = default)
        {
            try
            {
                Busy = true;
                return await body();
            }
            catch (Exception)
            {
                return onError;
            }
            finally
            {
                Busy = false;
                StateHasChanged();
            }
        }

        /// <summary>
        /// Invokes an action on the UI thread (wrapper for InvokeAsync).
        /// </summary>
        protected Task InvokeOnUIAsync(Action action)
        {
            return InvokeAsync(action);
        }

        /// <summary>
        /// Invokes an async func on the UI thread and returns the result.
        /// </summary>
        protected async Task<TResult> InvokeOnUIAsync<TResult>(Func<Task<TResult>> func)
        {
            TResult result = default!;
            await InvokeAsync(async () => result = await func());
            return result;
        }

        #endregion

        #region Event Handlers

        private void OnPropertyChangedInternal(object? sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(e.PropertyName);
        }

        /// <summary>
        /// Called when a property value changes. Override to react to property changes.
        /// </summary>
        protected virtual void OnPropertyChanged(string? propertyName)
        {
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (m_disposed)
                return;

            if (disposing)
            {
                PropertyChanged -= OnPropertyChangedInternal;
            }

            m_disposed = true;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets whether the view model is currently busy executing an operation.
        /// </summary>
        public bool Busy
        {
            get => m_busy;
            protected set => SetProperty(ref m_busy, value);
        }

        /// <summary>
        /// Gets whether the view model has been disposed.
        /// </summary>
        protected bool IsDisposed => m_disposed;

        #endregion
    }
}
