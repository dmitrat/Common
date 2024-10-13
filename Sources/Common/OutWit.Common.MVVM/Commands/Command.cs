using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Input;
using System.Windows.Threading;

namespace OutWit.Common.MVVM.Commands
{
    public abstract class Command : ICommand
    {
        #region Fields

        private readonly Dispatcher m_dispatcher;

        #endregion

        #region Constructors

        protected Command()
        {
            m_dispatcher = Dispatcher.CurrentDispatcher;

            Debug.Assert(m_dispatcher != null);
        }

        #endregion

        #region Functions

        public abstract bool CanExecute(object parameter);


        public abstract void Execute(object parameter);

        #endregion

        #region Events

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        #endregion

        #region Event Handlers

        protected virtual void OnCanExecuteChanged()
        {
            if (!m_dispatcher.CheckAccess())
                m_dispatcher.Invoke((ThreadStart)OnCanExecuteChanged, DispatcherPriority.Normal);
            else
                CommandManager.InvalidateRequerySuggested();
        } 

        #endregion
    }  
}
