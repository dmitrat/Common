using System;
using System.Threading.Tasks;
using OutWit.Common.MVVM.Interfaces;

namespace OutWit.Common.MVVM.Abstractions
{
    /// <summary>
    /// An <see cref="IDispatcher"/> that runs everything inline on the calling thread.
    /// For unit tests and console hosts, where there is no UI thread to marshal to.
    /// </summary>
    public sealed class DispatcherImmediate : IDispatcher
    {
        #region IDispatcher

        public bool CheckAccess()
        {
            return true;
        }

        public void Invoke(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            action();
        }

        public Task InvokeAsync(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            action();
            return Task.CompletedTask;
        }

        public TResult Invoke<TResult>(Func<TResult> func)
        {
            if (func == null)
                throw new ArgumentNullException(nameof(func));

            return func();
        }

        public Task<TResult> InvokeAsync<TResult>(Func<TResult> func)
        {
            if (func == null)
                throw new ArgumentNullException(nameof(func));

            return Task.FromResult(func());
        }

        #endregion

        #region Properties

        /// <summary>
        /// A shared instance; the class has no state.
        /// </summary>
        public static DispatcherImmediate Instance { get; } = new();

        #endregion
    }
}
