using System;
using System.Threading.Tasks;
using System.Windows.Threading;
using OutWit.Common.MVVM.Abstractions;

namespace OutWit.Common.MVVM.WPF.Abstractions
{
    public class WpfDispatcher : IDispatcher
    {
        #region Fields

        private readonly Dispatcher m_dispatcher;

        #endregion

        #region Constructors

        public WpfDispatcher(Dispatcher dispatcher)
        {
            m_dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        public static WpfDispatcher CurrentDispatcher => new WpfDispatcher(Dispatcher.CurrentDispatcher);

        #endregion

        #region IDispatcher

        public bool CheckAccess()
        {
            return m_dispatcher.CheckAccess();
        }

        public void Invoke(Action action)
        {
            if (m_dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                m_dispatcher.Invoke(action);
            }
        }

        public Task InvokeAsync(Action action)
        {
            if (m_dispatcher.CheckAccess())
            {
                action();
                return Task.CompletedTask;
            }

            return m_dispatcher.InvokeAsync(action).Task;
        }

        public TResult Invoke<TResult>(Func<TResult> func)
        {
            if (m_dispatcher.CheckAccess())
            {
                return func();
            }

            return m_dispatcher.Invoke(func);
        }

        public Task<TResult> InvokeAsync<TResult>(Func<TResult> func)
        {
            if (m_dispatcher.CheckAccess())
            {
                return Task.FromResult(func());
            }

            return m_dispatcher.InvokeAsync(func).Task;
        }

        #endregion
    }
}
