using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia.Threading;
using OutWit.Common.MVVM.Interfaces;

namespace OutWit.Common.MVVM.Avalonia.Collections
{
    /// <summary>
    /// Thread-safe ObservableCollection that marshals collection change notifications to the UI thread.
    /// </summary>
    public class SafeObservableCollection<T> : ObservableCollection<T>
    {
        #region Fields

        private readonly OutWit.Common.MVVM.Interfaces.IDispatcher m_dispatcher;

        #endregion

        #region Constructors

        public SafeObservableCollection()
            : this(new Abstractions.AvaloniaDispatcher(Dispatcher.UIThread))
        {
        }

        public SafeObservableCollection(Interfaces.IDispatcher dispatcher)
        {
            m_dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        #endregion

        #region ObservableCollection

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (m_dispatcher.CheckAccess())
            {
                base.OnCollectionChanged(e);
            }
            else
            {
                m_dispatcher.Invoke(() => base.OnCollectionChanged(e));
            }
        }

        #endregion
    }
}
