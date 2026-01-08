using System;
using System.Windows.Input;

namespace OutWit.Common.MVVM.Commands
{
    /// <summary>
    /// Strongly-typed synchronous relay command for cross-platform MVVM.
    /// </summary>
    /// <typeparam name="T">The type of the command parameter.</typeparam>
    public class RelayCommand<T> : ICommand
    {
        #region Fields

        private readonly Predicate<T?>? m_canExecute;
        private readonly Action<T?> m_execute;

        #endregion

        #region Constructors

        public RelayCommand(Action<T?> execute)
            : this(execute, null)
        {
        }

        public RelayCommand(Action<T?> execute, Predicate<T?>? canExecute)
        {
            m_execute = execute ?? throw new ArgumentNullException(nameof(execute));
            m_canExecute = canExecute;
        }

        #endregion

        #region Functions

        public bool CanExecute(object? parameter)
        {
            return m_canExecute == null || m_canExecute((T?)parameter);
        }

        public void Execute(object? parameter)
        {
            m_execute((T?)parameter);
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
