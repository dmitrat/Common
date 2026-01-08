using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OutWit.Common.MVVM.Commands
{
    /// <summary>
    /// Asynchronous relay command implementation for cross-platform MVVM.
    /// Automatically disables the command while executing.
    /// </summary>
    public class RelayCommandAsync : ICommand
    {
        #region Fields

        private readonly Predicate<object?>? m_canExecute;
        private readonly Func<object?, Task> m_executeAsync;
        private bool m_isExecuting;

        #endregion

        #region Constructors

        public RelayCommandAsync(Func<Task> executeAsync)
            : this(executeAsync, null)
        {
        }

        public RelayCommandAsync(Func<Task> executeAsync, Func<bool>? canExecute)
            : this(_ => executeAsync(), canExecute != null ? _ => canExecute() : null)
        {
        }

        public RelayCommandAsync(Func<object?, Task> executeAsync)
            : this(executeAsync, null)
        {
        }

        public RelayCommandAsync(Func<object?, Task> executeAsync, Predicate<object?>? canExecute)
        {
            m_executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            m_canExecute = canExecute;
        }

        #endregion

        #region Functions

        public bool CanExecute(object? parameter)
        {
            return !m_isExecuting && (m_canExecute == null || m_canExecute(parameter));
        }

        public async void Execute(object? parameter)
        {
            if (m_isExecuting)
                return;

            try
            {
                m_isExecuting = true;
                RaiseCanExecuteChanged();
                await m_executeAsync(parameter);
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
