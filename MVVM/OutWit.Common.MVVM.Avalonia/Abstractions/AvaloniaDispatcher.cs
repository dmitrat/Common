using System;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace OutWit.Common.MVVM.Avalonia.Abstractions
{
    /// <summary>
    /// Avalonia implementation of IDispatcher for cross-thread UI access.
    /// </summary>
    public class AvaloniaDispatcher : OutWit.Common.MVVM.Interfaces.IDispatcher
    {
        #region Fields

        private readonly Dispatcher m_dispatcher;

        #endregion

        #region Constructors

        public AvaloniaDispatcher(Dispatcher dispatcher)
        {
            m_dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        /// <summary>
        /// Gets the dispatcher for the UI thread.
        /// </summary>
        public static AvaloniaDispatcher UIThread => new AvaloniaDispatcher(Dispatcher.UIThread);

        #endregion

        #region IDispatcher

        public bool CheckAccess()
        {
            return m_dispatcher.CheckAccess();
        }

        public void Invoke(Action action)
        {
            if (m_dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                m_dispatcher.Invoke(action);
            }
        }

        public Task InvokeAsync(Action action)
        {
            if (m_dispatcher.CheckAccess())
            {
                action();
                return Task.CompletedTask;
            }

            return m_dispatcher.InvokeAsync(action).GetTask();
        }

        public TResult Invoke<TResult>(Func<TResult> func)
        {
            if (m_dispatcher.CheckAccess())
            {
                return func();
            }

            return m_dispatcher.Invoke(func);
        }

        public Task<TResult> InvokeAsync<TResult>(Func<TResult> func)
        {
            if (m_dispatcher.CheckAccess())
            {
                return Task.FromResult(func());
            }

            return m_dispatcher.InvokeAsync(func).GetTask();
        }

        #endregion
    }
}
