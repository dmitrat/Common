using System;
using System.Windows.Input;

namespace OutWit.Common.MVVM.Commands
{
    /// <summary>
    /// Synchronous relay command implementation for cross-platform MVVM.
    /// </summary>
    public class RelayCommand : ICommand
    {
        #region Fields

        private readonly Predicate<object?>? m_canExecute;
        private readonly Action<object?> m_execute;

        #endregion

        #region Constructors

        public RelayCommand(Action execute)
            : this(execute, null)
        {
        }

        public RelayCommand(Action execute, Func<bool>? canExecute)
            : this(_ => execute(), canExecute != null ? _ => canExecute() : null)
        {
        }

        public RelayCommand(Action<object?> execute)
            : this(execute, null)
        {
        }

        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute)
        {
            m_execute = execute ?? throw new ArgumentNullException(nameof(execute));
            m_canExecute = canExecute;
        }

        #endregion

        #region Functions

        public bool CanExecute(object? parameter)
        {
            return m_canExecute == null || m_canExecute(parameter);
        }

        public void Execute(object? parameter)
        {
            m_execute(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Events

        public event EventHandler? CanExecuteChanged;

        #endregion
    }
}
