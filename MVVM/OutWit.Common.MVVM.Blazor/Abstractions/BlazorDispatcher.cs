using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using OutWit.Common.MVVM.Interfaces;

namespace OutWit.Common.MVVM.Blazor.Abstractions
{
    /// <summary>
    /// Blazor implementation of IDispatcher for UI thread invocation.
    /// Wraps ComponentBase.InvokeAsync for cross-thread UI access.
    /// </summary>
    public class BlazorDispatcher : IDispatcher
    {
        #region Fields

        private readonly ComponentBase m_component;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a BlazorDispatcher from a ComponentBase instance.
        /// </summary>
        public BlazorDispatcher(ComponentBase component)
        {
            m_component = component ?? throw new ArgumentNullException(nameof(component));
        }

        #endregion

        #region IDispatcher

        /// <summary>
        /// In Blazor, components are always accessed on the correct synchronization context.
        /// Returns true as Blazor handles this automatically.
        /// </summary>
        public bool CheckAccess()
        {
            // Blazor ensures component code runs on the correct synchronization context
            return true;
        }

        public void Invoke(Action action)
        {
            // In Blazor, synchronous invoke is generally discouraged
            // We invoke async and wait
            InvokeAsync(action).GetAwaiter().GetResult();
        }

        public Task InvokeAsync(Action action)
        {
            return GetInvokeAsync()(action);
        }

        public TResult Invoke<TResult>(Func<TResult> func)
        {
            return InvokeAsync(func).GetAwaiter().GetResult();
        }

        public Task<TResult> InvokeAsync<TResult>(Func<TResult> func)
        {
            return GetInvokeAsyncFunc<TResult>()(func);
        }

        #endregion

        #region Functions

        private Func<Action, Task> GetInvokeAsync()
        {
            // Use reflection to access protected InvokeAsync method
            var method = typeof(ComponentBase).GetMethod("InvokeAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null,
                new[] { typeof(Action) },
                null);

            if (method == null)
                throw new InvalidOperationException("Could not find InvokeAsync method on ComponentBase");

            return action => (Task)method.Invoke(m_component, new object[] { action })!;
        }

        private Func<Func<TResult>, Task<TResult>> GetInvokeAsyncFunc<TResult>()
        {
            var method = typeof(ComponentBase).GetMethod("InvokeAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null,
                new[] { typeof(Func<TResult>) },
                null);

            if (method == null)
                throw new InvalidOperationException("Could not find InvokeAsync method on ComponentBase");

            return func => (Task<TResult>)method.Invoke(m_component, new object[] { func })!;
        }

        #endregion
    }
}
