using System.Threading.Tasks;

namespace OutWit.Common.MVVM.Navigation.Services
{
    /// <summary>
    /// One open dialog, untyped: what the service keeps on its per-host stack.
    /// </summary>
    internal abstract class DialogSession
    {
        #region Constructors

        protected DialogSession(string host)
        {
            Host = host;
        }

        #endregion

        #region Functions

        /// <summary>
        /// Asks the dialog to close as cancelled; it may refuse.
        /// </summary>
        public abstract Task RequestCancelAsync();

        #endregion

        #region Properties

        public string Host { get; }

        #endregion
    }
}
