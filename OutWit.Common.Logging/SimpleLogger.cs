using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OutWit.Common.Aspects;

namespace OutWit.Common.Logging
{
    public class SimpleLogger : ILogger, IEnumerable<string>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        #region Events

        public event NotifyCollectionChangedEventHandler CollectionChanged = delegate { };

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        #endregion

        #region Fields

        private readonly ObservableCollection<string> m_entries = new();
        
        private readonly SimpleLoggerFormatter m_formatter = new ();

        #endregion
        
        #region Constructors

        public SimpleLogger(string name, LogLevel minimumLevel = LogLevel.Trace)
        {
            Name = name;
            MinimumLevel = minimumLevel;
            
            InitEvents();
        }

        #endregion

        #region Initialization

        private void InitEvents()
        {
            m_entries.CollectionChanged += OnEntriesCollectionChanged;
        }

        #endregion

        #region ILogger

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            m_entries.Add(m_formatter.Format(new LogEntry<TState>(logLevel, Name, eventId, state, exception, formatter)));
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= MinimumLevel;
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            return default!;
        }

        #endregion

        #region Event Handlers
        
        private void OnEntriesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged(sender, e);
        } 
        
        #endregion

        #region Properties

        [Notify]
        public LogLevel MinimumLevel { get; set; }
        
        public string Name { get; }

        #endregion

        #region IEnumerable

        public IEnumerator<string> GetEnumerator()
        {
            return m_entries.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        } 
        
        #endregion


    }
}
