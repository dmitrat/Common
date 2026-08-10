using System;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.MVVM.Interfaces;

namespace OutWit.Common.Licensing.MVVM.Tests.Mock
{
    /// <summary>
    /// A dispatcher that owns one thread, the way a UI dispatcher does, and
    /// records whether it was actually used.
    /// </summary>
    internal sealed class LicenseDispatcherMock : IDispatcher
    {
        #region Fields

        private readonly int m_threadId = Thread.CurrentThread.ManagedThreadId;

        #endregion

        #region IDispatcher

        public bool CheckAccess()
        {
            return Thread.CurrentThread.ManagedThreadId == m_threadId;
        }

        public void Invoke(Action action)
        {
            Marshalled++;

            action();
        }

        public Task InvokeAsync(Action action)
        {
            Invoke(action);

            return Task.CompletedTask;
        }

        public TResult Invoke<TResult>(Func<TResult> func)
        {
            Marshalled++;

            return func();
        }

        public Task<TResult> InvokeAsync<TResult>(Func<TResult> func)
        {
            return Task.FromResult(Invoke(func));
        }

        #endregion

        #region Properties

        /// <summary>How many times work had to be marshalled onto the owning thread.</summary>
        public int Marshalled { get; private set; }

        #endregion
    }
}
