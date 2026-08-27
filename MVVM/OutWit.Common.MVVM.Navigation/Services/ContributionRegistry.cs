using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.Extensions.Logging;
using OutWit.Common.MVVM.Commands;
using OutWit.Common.MVVM.Interfaces;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Model;

namespace OutWit.Common.MVVM.Navigation.Services
{
    /// <summary>
    /// Default <see cref="IContributionRegistry"/>. Applies every collection change on the
    /// UI thread, attaches navigation commands to items with a route key, and keeps the
    /// zones' selection aligned with <see cref="INavigationService.Navigated"/>.
    /// </summary>
    public sealed class ContributionRegistry : IContributionRegistry
    {
        #region Fields

        private readonly object m_sync = new();
        private readonly Dictionary<string, ContributionZone> m_zones = new(StringComparer.Ordinal);
        private readonly List<ContributionZone> m_order = new();
        private readonly Dictionary<ContributionItem, PropertyChangedEventHandler> m_subscriptions = new();

        private readonly INavigationService m_navigation;
        private readonly IRouteFacts m_routes;
        private readonly IDispatcher m_dispatcher;
        private readonly ILogger<ContributionRegistry>? m_logger;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the registry. Resolved from DI by <c>services.AddNavigation()</c>.
        /// </summary>
        /// <param name="navigation">The navigation service; commands navigate through it and selection follows its events.</param>
        /// <param name="routes">The route registry; resolves an item's default outlet and group membership.</param>
        /// <param name="dispatcher">The UI-thread dispatcher.</param>
        /// <param name="options">What AddNavigation collected; null means no pre-created zones.</param>
        /// <param name="logger">Optional logger.</param>
        public ContributionRegistry(INavigationService navigation,
                                    IRouteRegistry routes,
                                    IDispatcher dispatcher,
                                    NavigationOptions? options = null,
                                    ILogger<ContributionRegistry>? logger = null)
        {
            m_navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            m_routes = new RouteFacts(routes ?? throw new ArgumentNullException(nameof(routes)));
            m_dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            m_logger = logger;

            if (options != null)
            {
                foreach (var zone in options.Zones)
                    GetOrCreateZone(zone);
            }

            InitEvents();
        }

        #endregion

        #region Initialization

        private void InitEvents()
        {
            m_navigation.Navigated += OnNavigated;
        }

        #endregion

        #region IContributionRegistry

        public void Add(ContributionItem item)
        {
            Validate(item);

            m_dispatcher.Invoke(() => AddCore(item));
        }

        public void AddRange(IEnumerable<ContributionItem> items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            var list = items.ToList();
            foreach (var item in list)
                Validate(item);

            m_dispatcher.Invoke(() =>
            {
                foreach (var item in list)
                    AddCore(item);
            });
        }

        public bool Remove(string zone, string key)
        {
            var target = GetZone(zone);
            if (target == null)
                return false;

            return m_dispatcher.Invoke(() =>
            {
                var removed = target.Remove(key);
                if (removed == null)
                    return false;

                DetachCommand(removed);
                return true;
            });
        }

        public void Clear(string zone)
        {
            var target = GetZone(zone);
            if (target == null)
                return;

            m_dispatcher.Invoke(() =>
            {
                foreach (var item in target.Clear())
                    DetachCommand(item);
            });
        }

        public IContributionZone Zone(string name)
        {
            return GetOrCreateZone(name);
        }

        public ContributionItem? Find(string zone, string key)
        {
            return GetZone(zone)?.Find(key);
        }

        #endregion

        #region Functions

        private void AddCore(ContributionItem item)
        {
            var zone = GetOrCreateZone(item.Zone);

            AttachCommand(item);

            var replaced = zone.Add(item);
            if (replaced != null && !ReferenceEquals(replaced, item))
                DetachCommand(replaced);

            foreach (var outlet in m_navigation.Outlets)
                zone.UpdateSelection(outlet, m_routes);
        }

        private void AttachCommand(ContributionItem item)
        {
            if (item.RouteKey == null || m_subscriptions.ContainsKey(item))
                return;

            var command = new RelayCommandAsync(
                () => m_navigation.NavigateAsync(item.RouteKey, item.Parameters, item.Outlet),
                () => item.IsEnabled);

            void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == nameof(ContributionItem.IsEnabled))
                    command.RaiseCanExecuteChanged();
            }

            item.PropertyChanged += OnItemPropertyChanged;
            m_subscriptions[item] = OnItemPropertyChanged;
            item.Command = command;
        }

        private void DetachCommand(ContributionItem item)
        {
            if (!m_subscriptions.TryGetValue(item, out var handler))
                return;

            item.PropertyChanged -= handler;
            m_subscriptions.Remove(item);
            item.Command = null;
        }

        private ContributionZone GetOrCreateZone(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Zone name must be a non-empty string.", nameof(name));

            lock (m_sync)
            {
                if (m_zones.TryGetValue(name, out var existing))
                    return existing;

                var zone = new ContributionZone(name);
                m_zones[name] = zone;
                m_order.Add(zone);

                return zone;
            }
        }

        private ContributionZone? GetZone(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            lock (m_sync)
                return m_zones.TryGetValue(name, out var zone) ? zone : null;
        }

        private static void Validate(ContributionItem item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (string.IsNullOrEmpty(item.Zone))
                throw new ArgumentException("ContributionItem.Zone must be a non-empty string.", nameof(item));

            if (string.IsNullOrEmpty(item.Key))
                throw new ArgumentException("ContributionItem.Key must be a non-empty string.", nameof(item));
        }

        #endregion

        #region Event Handlers

        private void OnNavigated(INavigationOutlet? outlet, NavigationResult result)
        {
            if (outlet == null)
                return;

            ContributionZone[] zones;
            lock (m_sync)
                zones = m_order.ToArray();

            try
            {
                foreach (var zone in zones)
                    zone.UpdateSelection(outlet, m_routes);
            }
            catch (Exception e)
            {
                m_logger?.LogError(e, "Updating zone selection after navigation to {Route} failed", result.RouteKey);
            }
        }

        #endregion

        #region Properties

        public IReadOnlyList<string> Zones
        {
            get
            {
                lock (m_sync)
                    return m_order.Select(zone => zone.Name).ToArray();
            }
        }

        #endregion
    }
}
