using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using OutWit.Common.MVVM.Abstractions;

namespace OutWit.Common.MVVM.Collections
{
    /// <summary>
    /// Thread-safe ObservableCollection that marshals change notifications to the dispatcher thread
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection</typeparam>
    public class SafeObservableCollection<T> : ObservableCollection<T>
    {
        #region Fields

        private readonly IDispatcher? m_dispatcher;

        #endregion

        #region Constructors

        public SafeObservableCollection()
        {
        }

        public SafeObservableCollection(IDispatcher dispatcher)
        {
            m_dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        #endregion

        #region ObservableCollection

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (m_dispatcher != null && !m_dispatcher.CheckAccess())
            {
                m_dispatcher.Invoke(() => base.OnCollectionChanged(e));
            }
            else
            {
                base.OnCollectionChanged(e);
            }
        }

        #endregion
    }
}
