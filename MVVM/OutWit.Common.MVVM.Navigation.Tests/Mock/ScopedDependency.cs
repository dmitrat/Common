using System;
using System.Threading;

namespace OutWit.Common.MVVM.Navigation.Tests.Mock
{
    /// <summary>
    /// Registered AddScoped: proves that a Transient route gets its own scope and that the
    /// scope is disposed with the view model.
    /// </summary>
    public sealed class ScopedDependency : IDisposable
    {
        #region Fields

        private static int s_instances;

        #endregion

        #region Constructors

        public ScopedDependency()
        {
            Id = Interlocked.Increment(ref s_instances);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            IsDisposed = true;
        }

        #endregion

        #region Properties

        public int Id { get; }

        public bool IsDisposed { get; private set; }

        #endregion
    }
}
