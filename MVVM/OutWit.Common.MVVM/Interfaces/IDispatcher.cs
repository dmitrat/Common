using System;
using System.Threading.Tasks;

namespace OutWit.Common.MVVM.Interfaces
{
    /// <summary>
    /// Provides platform-agnostic thread marshalling capabilities
    /// </summary>
    public interface IDispatcher
    {
        /// <summary>
        /// Checks if the current thread has access to this dispatcher
        /// </summary>
        bool CheckAccess();

        /// <summary>
        /// Invokes an action on the dispatcher thread
        /// </summary>
        void Invoke(Action action);

        /// <summary>
        /// Invokes an action on the dispatcher thread asynchronously
        /// </summary>
        Task InvokeAsync(Action action);

        /// <summary>
        /// Invokes a function on the dispatcher thread
        /// </summary>
        TResult Invoke<TResult>(Func<TResult> func);

        /// <summary>
        /// Invokes a function on the dispatcher thread asynchronously
        /// </summary>
        Task<TResult> InvokeAsync<TResult>(Func<TResult> func);
    }
}
