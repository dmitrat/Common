using System;
using System.Threading.Tasks;

namespace OutWit.Common.MVVM.WPF.Commands
{
    /// <summary>
    /// Asynchronous delegate command for WPF with CommandManager integration.
    /// Automatically disables the command while executing.
    /// </summary>
    public class DelegateCommandAsync : Command
    {
        #region Fields

        private readonly Predicate<object?>? m_canExecute;
        private readonly Func<object?, Task> m_executeAsync;
        private bool m_isExecuting;

        #endregion

        #region Constructors

        public DelegateCommandAsync(Func<Task> executeAsync)
            : this(executeAsync, null)
        {
        }

        public DelegateCommandAsync(Func<Task> executeAsync, Func<bool>? canExecute)
            : this(_ => executeAsync(), canExecute != null ? _ => canExecute() : null)
        {
        }

        public DelegateCommandAsync(Func<object?, Task> executeAsync)
            : this(executeAsync, null)
        {
        }

        public DelegateCommandAsync(Func<object?, Task> executeAsync, Predicate<object?>? canExecute)
        {
            m_executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            m_canExecute = canExecute;
        }

        #endregion

        #region Functions

        public override bool CanExecute(object? parameter)
        {
            return !m_isExecuting && (m_canExecute == null || m_canExecute(parameter));
        }

        public override async void Execute(object? parameter)
        {
            if (m_isExecuting)
                return;

            try
            {
                m_isExecuting = true;
                OnCanExecuteChanged();
                await m_executeAsync(parameter);
            }
            finally
            {
                m_isExecuting = false;
                OnCanExecuteChanged();
            }
        }

        #endregion

        #region Properties

        public bool IsExecuting => m_isExecuting;

        #endregion
    }
}
