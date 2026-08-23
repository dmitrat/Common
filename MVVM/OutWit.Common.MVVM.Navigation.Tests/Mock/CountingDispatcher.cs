using System;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.MVVM.Interfaces;

namespace OutWit.Common.MVVM.Navigation.Tests.Mock
{
    /// <summary>
    /// An IDispatcher that reports "not on the UI thread" and counts how often work was
    /// marshalled through it. The work itself runs inline.
    /// </summary>
    public sealed class CountingDispatcher : IDispatcher
    {
        #region Fields

        private int m_invocations;

        #endregion

        #region IDispatcher

        public bool CheckAccess()
        {
            return HasAccess;
        }

        public void Invoke(Action action)
        {
            Interlocked.Increment(ref m_invocations);
            action();
        }

        public Task InvokeAsync(Action action)
        {
            Interlocked.Increment(ref m_invocations);
            action();
            return Task.CompletedTask;
        }

        public TResult Invoke<TResult>(Func<TResult> func)
        {
            Interlocked.Increment(ref m_invocations);
            return func();
        }

        public Task<TResult> InvokeAsync<TResult>(Func<TResult> func)
        {
            Interlocked.Increment(ref m_invocations);
            return Task.FromResult(func());
        }

        #endregion

        #region Properties

        public bool HasAccess { get; set; }

        public int Invocations => m_invocations;

        #endregion
    }
}
