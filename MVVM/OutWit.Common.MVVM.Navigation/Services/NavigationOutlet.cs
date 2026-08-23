using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Abstract;
using OutWit.Common.Aspects;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Model;

namespace OutWit.Common.MVVM.Navigation.Services
{
    /// <summary>
    /// The state behind <see cref="INavigationOutlet"/>: the current view model, the journal,
    /// the cache of Cached view models and the in-flight bookkeeping. State changes happen on
    /// the UI thread (the pipeline runs there); the in-flight bookkeeping is locked so that
    /// tests with an immediate dispatcher stay deterministic.
    /// </summary>
    internal sealed class NavigationOutlet : NotifyPropertyChangedBase, INavigationOutlet
    {
        #region Fields

        private readonly object m_sync = new();
        private readonly SemaphoreSlim m_gate = new(1, 1);
        private readonly List<NavigationEntry> m_history = new();
        private readonly Dictionary<string, NavigationViewModelHandle> m_cache = new(StringComparer.Ordinal);
        private readonly int m_historyDepth;

        private NavigationTicket? m_inFlight;
        private CancellationTokenSource? m_contentCancellation;
        private int m_pending;
        private int m_historyIndex = -1;

        #endregion

        #region Constructors

        public NavigationOutlet(string name, int historyDepth)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Outlet name must be a non-empty string.", nameof(name));

            Name = name;
            History = new ReadOnlyCollection<NavigationEntry>(m_history);
            Parameters = NavigationParameters.EMPTY;
            m_historyDepth = Math.Max(0, historyDepth);
        }

        #endregion

        #region Queue

        public bool IsShowing(NavigationRoute route, NavigationParameters parameters)
        {
            return Route != null
                   && Route.Key == route.Key
                   && Parameters.Is(parameters);
        }

        public void PreemptInFlight()
        {
            lock (m_sync)
            {
                if (m_inFlight is { IsPreemptible: true } ticket)
                    ticket.Cancel();
            }
        }

        public void EnterQueue()
        {
            if (Interlocked.Increment(ref m_pending) == 1)
                IsNavigating = true;
        }

        public void ExitQueue()
        {
            if (Interlocked.Decrement(ref m_pending) == 0)
                IsNavigating = false;
        }

        public Task WaitAsync(CancellationToken cancellation)
        {
            return m_gate.WaitAsync(cancellation);
        }

        public NavigationTicket Begin()
        {
            var ticket = new NavigationTicket();

            lock (m_sync)
                m_inFlight = ticket;

            return ticket;
        }

        public void MakeFinal(NavigationTicket ticket)
        {
            lock (m_sync)
                ticket.MakeFinal();
        }

        public void End(NavigationTicket ticket)
        {
            lock (m_sync)
            {
                if (ReferenceEquals(m_inFlight, ticket))
                    m_inFlight = null;
            }

            ticket.Dispose();
            m_gate.Release();
        }

        #endregion

        #region View Models

        public bool TryGetCached(string routeKey, out NavigationViewModelHandle handle)
        {
            return m_cache.TryGetValue(routeKey, out handle!);
        }

        /// <summary>
        /// Returns the cached handle for a Cached route, or a fresh one from the factory.
        /// A fresh handle is not cached until <see cref="Commit"/>: a navigation that is
        /// rejected or cancelled after creating it disposes it instead.
        /// </summary>
        public NavigationViewModelHandle GetOrCreate(NavigationRoute route, Func<NavigationRoute, NavigationViewModelHandle> factory, out bool created)
        {
            if (route.Mode == NavigationRouteMode.Cached && m_cache.TryGetValue(route.Key, out var cached))
            {
                created = false;
                return cached;
            }

            created = true;
            return factory(route);
        }

        /// <summary>
        /// Shows a view model. Cancels the token handed to the previous OnNavigatedToAsync —
        /// that screen is gone, and whatever it was still loading is no longer wanted — and
        /// returns the token for the new one.
        /// </summary>
        public CancellationToken Commit(NavigationViewModelHandle target, NavigationParameters parameters, NavigationKind kind, int historyIndex)
        {
            if (target.Route.Mode == NavigationRouteMode.Cached)
                m_cache[target.Route.Key] = target;

            var previousCancellation = m_contentCancellation;
            m_contentCancellation = new CancellationTokenSource();
            var token = m_contentCancellation.Token;

            // Route and Parameters first: whoever reacts to the Content change (the outlet
            // control deciding whether to cache a view, a zone updating its selection) must
            // see the new route, not the old one
            Current = target;
            Route = target.Route;
            Parameters = parameters;
            Content = target.ViewModel;

            switch (kind)
            {
                case NavigationKind.New:
                    WriteHistory(target.Route.Key, parameters);
                    break;

                case NavigationKind.Back:
                case NavigationKind.Forward:
                    m_historyIndex = historyIndex;
                    break;

                case NavigationKind.Refresh:
                    break;
            }

            RaiseHistoryChanged();

            if (previousCancellation != null)
            {
                previousCancellation.Cancel();
                previousCancellation.Dispose();
            }

            return token;
        }

/// <summary>
        /// Drops a cached view model, holding the outlet's slot while it does.
        /// </summary>
        /// <remarks>
        /// The slot is the point. Without it an eviction can land between a navigation's
        /// cache read and its commit: the pipeline is already holding the instance, this
        /// method disposes it and removes it from the cache, and the commit then puts the
        /// disposed instance back and shows it. Waiting for the slot costs an eviction
        /// nothing — it is a maintenance call, not something a user is waiting on.
        /// </remarks>
        public async ValueTask<bool> EvictAsync(string routeKey)
        {
            await m_gate.WaitAsync();

            try
            {
                if (!m_cache.TryGetValue(routeKey, out var handle))
                    return false;

                if (ReferenceEquals(handle, Current))
                    return false;

                m_cache.Remove(routeKey);
                await handle.DisposeAsync();

                return true;
            }
            finally
            {
                m_gate.Release();
            }
        }

        #endregion

        #region History

        public NavigationEntry? PeekBack()
        {
            return CanGoBack ? m_history[m_historyIndex - 1] : null;
        }

        public NavigationEntry? PeekForward()
        {
            return CanGoForward ? m_history[m_historyIndex + 1] : null;
        }

        public void ClearHistory()
        {
            m_history.Clear();
            m_historyIndex = -1;

            RaiseHistoryChanged();
        }

        private void WriteHistory(string routeKey, NavigationParameters parameters)
        {
            if (m_historyDepth == 0)
                return;

            if (m_historyIndex < m_history.Count - 1)
                m_history.RemoveRange(m_historyIndex + 1, m_history.Count - m_historyIndex - 1);

            m_history.Add(new NavigationEntry(routeKey, parameters));

            while (m_history.Count > m_historyDepth)
                m_history.RemoveAt(0);

            m_historyIndex = m_history.Count - 1;
        }

        private void RaiseHistoryChanged()
        {
            OnPropertyChanged(nameof(History));
            OnPropertyChanged(nameof(HistoryIndex));
            OnPropertyChanged(nameof(CanGoBack));
            OnPropertyChanged(nameof(CanGoForward));
        }

        #endregion

        #region Properties

        public string Name { get; }

        [Notify]
        public object? Content { get; private set; }

        [Notify(NotifyAlso = nameof(RouteKey))]
        public NavigationRoute? Route { get; private set; }

        public string? RouteKey => Route?.Key;

        [Notify]
        public NavigationParameters Parameters { get; private set; }

        [Notify]
        public bool IsNavigating { get; private set; }

        public bool CanGoBack => m_historyIndex > 0;

        public bool CanGoForward => m_historyIndex >= 0 && m_historyIndex < m_history.Count - 1;

        public IReadOnlyList<NavigationEntry> History { get; }

        public int HistoryIndex => m_historyIndex;

        public NavigationViewModelHandle? Current { get; private set; }

        #endregion
    }
}
