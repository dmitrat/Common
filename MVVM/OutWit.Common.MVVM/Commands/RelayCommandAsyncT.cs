using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OutWit.Common.MVVM.Commands
{
    /// <summary>
    /// Strongly-typed asynchronous relay command for cross-platform MVVM.
    /// Automatically disables the command while executing.
    /// </summary>
    /// <typeparam name="T">The type of the command parameter.</typeparam>
    public class RelayCommandAsync<T> : ICommand
    {
        #region Fields

        private readonly Predicate<T?>? m_canExecute;
        private readonly Func<T?, Task> m_executeAsync;
        private bool m_isExecuting;

        #endregion

        #region Constructors

        public RelayCommandAsync(Func<T?, Task> executeAsync)
            : this(executeAsync, null)
        {
        }

        public RelayCommandAsync(Func<T?, Task> executeAsync, Predicate<T?>? canExecute)
        {
            m_executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            m_canExecute = canExecute;
        }

        #endregion

        #region Functions

        public bool CanExecute(object? parameter)
        {
            return !m_isExecuting && (m_canExecute == null || m_canExecute((T?)parameter));
        }

        public async void Execute(object? parameter)
        {
            if (m_isExecuting)
                return;

            try
            {
                m_isExecuting = true;
                RaiseCanExecuteChanged();
                await m_executeAsync((T?)parameter);
            }
            finally
            {
                m_isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Events

        public event EventHandler? CanExecuteChanged;

        #endregion

        #region Properties

        public bool IsExecuting => m_isExecuting;

        #endregion
    }
}
