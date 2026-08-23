using System.Threading;

namespace OutWit.Common.MVVM.Navigation.Tests.Mock
{
    /// <summary>
    /// A view model with no navigation interfaces and one DI dependency.
    /// </summary>
    public sealed class PlainViewModel
    {
        #region Fields

        private static int s_instances;

        #endregion

        #region Constructors

        public PlainViewModel(CallLog log)
        {
            Log = log;
            Id = Interlocked.Increment(ref s_instances);
            log.Add($"Plain#{Id}.Created");
        }

        #endregion

        #region Properties

        public CallLog Log { get; }

        public int Id { get; }

        #endregion
    }
}
