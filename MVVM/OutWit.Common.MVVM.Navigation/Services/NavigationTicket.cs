using System;
using System.Threading;

namespace OutWit.Common.MVVM.Navigation.Services
{
    /// <summary>
    /// Marks one navigation in flight on an outlet. A newer navigation into the same
    /// outlet cancels the ticket while it is preemptible; past the point of no return
    /// it is final and the newer navigation waits instead.
    /// </summary>
    internal sealed class NavigationTicket : IDisposable
    {
        #region Fields

        private readonly CancellationTokenSource m_cancellation = new();

        #endregion

        #region Functions

        public void Cancel()
        {
            if (!m_cancellation.IsCancellationRequested)
                m_cancellation.Cancel();
        }

        public void MakeFinal()
        {
            IsPreemptible = false;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            m_cancellation.Dispose();
        }

        #endregion

        #region Properties

        public CancellationToken Token => m_cancellation.Token;

        public bool IsPreemptible { get; private set; } = true;

        #endregion
    }
}
