using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OutWit.Common.MVVM.Interfaces;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Model;

namespace OutWit.Common.MVVM.Navigation.Services
{
    /// <summary>
    /// Default <see cref="INavigationService"/>. Holds the outlets, resolves routes, hops to
    /// the UI thread once and hands the work to <see cref="NavigationPipeline"/>.
    /// </summary>
    public sealed class NavigationService : INavigationService
    {
        #region Events

        public event NavigatedEventHandler? Navigated;
        public event NavigatedEventHandler? NavigationFailed;

        #endregion

        #region Fields

        private readonly object m_sync = new();
        private readonly Dictionary<string, NavigationOutlet> m_outlets = new(StringComparer.Ordinal);
        private readonly List<NavigationOutlet> m_outletsOrder = new();

        private readonly IServiceProvider m_provider;
        private readonly IRouteRegistry m_routes;
        private readonly IDispatcher m_dispatcher;
        private readonly ILogger<NavigationService>? m_logger;
        private readonly NavigationPipeline m_pipeline;
        private readonly int m_historyDepth;

        private IReadOnlyList<INavigationGuard>? m_guards;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the service. Resolved from DI by <c>services.AddNavigation()</c>.
        /// </summary>
        /// <param name="provider">The root service provider; view models and their scopes come from it.</param>
        /// <param name="routes">The route registry.</param>
        /// <param name="dispatcher">The UI-thread dispatcher.</param>
        /// <param name="options">What AddNavigation collected; null means defaults.</param>
        /// <param name="logger">Optional logger.</param>
        public NavigationService(IServiceProvider provider,
                                 IRouteRegistry routes,
                                 IDispatcher dispatcher,
                                 NavigationOptions? options = null,
                                 ILogger<NavigationService>? logger = null)
        {
            m_provider = provider ?? throw new ArgumentNullException(nameof(provider));
            m_routes = routes ?? throw new ArgumentNullException(nameof(routes));
            m_dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            m_logger = logger;
            m_historyDepth = options?.HistoryDepth ?? NavigationOptions.DEFAULT_HISTORY_DEPTH;
            m_pipeline = new NavigationPipeline(provider, ResolveGuards, RaiseNavigated, logger);

            foreach (var name in options?.Outlets ?? new List<string> { NavigationOutlets.MAIN })
                AddOutlet(name);
        }

        #endregion

        #region Outlets

        public INavigationOutlet Outlet(string? name = null)
        {
            return GetOutlet(name ?? NavigationOutlets.MAIN)
                   ?? throw new InvalidOperationException($"Outlet '{name ?? NavigationOutlets.MAIN}' is not registered. Declare it in AddNavigation or call AddOutlet.");
        }

        public bool HasOutlet(string name)
        {
            return GetOutlet(name) != null;
        }

        public INavigationOutlet AddOutlet(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Outlet name must be a non-empty string.", nameof(name));

            lock (m_sync)
            {
                if (m_outlets.TryGetValue(name, out var existing))
                    return existing;

                var outlet = new NavigationOutlet(name, m_historyDepth);
                m_outlets[name] = outlet;
                m_outletsOrder.Add(outlet);

                return outlet;
            }
        }

        private NavigationOutlet? GetOutlet(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            lock (m_sync)
                return m_outlets.TryGetValue(name, out var outlet) ? outlet : null;
        }

        #endregion

        #region Navigation

        public Task<NavigationResult> NavigateAsync(string routeKey,
                                                    NavigationParameters? parameters = null,
                                                    string? outlet = null,
                                                    CancellationToken cancellation = default)
        {
            if (routeKey == null)
                throw new ArgumentNullException(nameof(routeKey));

            if (!m_routes.TryGet(routeKey, out var route))
                return Task.FromResult(Fail(null, NavigationStatus.RouteNotFound, routeKey, outlet ?? string.Empty));

            return NavigateAsync(route, parameters, outlet, cancellation);
        }

        public Task<NavigationResult> NavigateAsync<TViewModel>(NavigationParameters? parameters = null,
                                                                string? outlet = null,
                                                                CancellationToken cancellation = default)
            where TViewModel : class
        {
            if (!m_routes.TryGetFor<TViewModel>(out var route))
                return Task.FromResult(Fail(null, NavigationStatus.RouteNotFound, typeof(TViewModel).FullName ?? typeof(TViewModel).Name, outlet ?? string.Empty));

            return NavigateAsync(route, parameters, outlet, cancellation);
        }

        private Task<NavigationResult> NavigateAsync(NavigationRoute route, NavigationParameters? parameters, string? outlet, CancellationToken cancellation)
        {
            var outletName = outlet ?? route.Outlet;
            var target = GetOutlet(outletName);

            if (target == null)
                return Task.FromResult(Fail(null, NavigationStatus.OutletNotFound, route.Key, outletName));

            return RunAsync(target, () => Task.FromResult<NavigationRequest?>(
                new NavigationRequest(target, route, parameters ?? NavigationParameters.EMPTY, NavigationKind.New, -1, cancellation)));
        }

        public Task<bool> CanNavigateAsync(string routeKey,
                                           NavigationParameters? parameters = null,
                                           string? outlet = null,
                                           CancellationToken cancellation = default)
        {
            if (routeKey == null)
                throw new ArgumentNullException(nameof(routeKey));

            if (!m_routes.TryGet(routeKey, out var route))
                return Task.FromResult(false);

            var target = GetOutlet(outlet ?? route.Outlet);
            if (target == null)
                return Task.FromResult(false);

            return RunOnDispatcherAsync(async () =>
            {
                try
                {
                    return await m_pipeline.CanNavigateAsync(target, route, parameters ?? NavigationParameters.EMPTY, cancellation);
                }
                catch (Exception e)
                {
                    m_logger?.LogError(e, "CanNavigate {Route} in {Outlet} failed", route.Key, target.Name);
                    return false;
                }
            });
        }

        public Task<NavigationResult> GoBackAsync(string? outlet = null, CancellationToken cancellation = default)
        {
            return MoveAsync(outlet, NavigationKind.Back, cancellation);
        }

        public Task<NavigationResult> GoForwardAsync(string? outlet = null, CancellationToken cancellation = default)
        {
            return MoveAsync(outlet, NavigationKind.Forward, cancellation);
        }

        public Task<NavigationResult> RefreshAsync(string? outlet = null, CancellationToken cancellation = default)
        {
            return MoveAsync(outlet, NavigationKind.Refresh, cancellation);
        }

        private Task<NavigationResult> MoveAsync(string? outlet, NavigationKind kind, CancellationToken cancellation)
        {
            var outletName = outlet ?? NavigationOutlets.MAIN;
            var target = GetOutlet(outletName);

            if (target == null)
                return Task.FromResult(Fail(null, NavigationStatus.OutletNotFound, string.Empty, outletName));

            // the outlet's journal is read on the UI thread, inside the hop
            return RunAsync(target, () =>
            {
                NavigationEntry? entry;
                int index;

                switch (kind)
                {
                    case NavigationKind.Back:
                        entry = target.PeekBack();
                        index = target.HistoryIndex - 1;
                        break;

                    case NavigationKind.Forward:
                        entry = target.PeekForward();
                        index = target.HistoryIndex + 1;
                        break;

                    default:
                        entry = target.Route != null ? new NavigationEntry(target.Route.Key, target.Parameters) : null;
                        index = target.HistoryIndex;
                        break;
                }

                if (entry == null)
                    return Task.FromResult<NavigationRequest?>(null);

                if (!m_routes.TryGet(entry.RouteKey, out var route))
                    throw new NavigationRouteMissingException(entry.RouteKey);

                return Task.FromResult<NavigationRequest?>(new NavigationRequest(target, route, entry.Parameters, kind, index, cancellation));
            });
        }

        public void ClearHistory(string? outlet = null)
        {
            var target = GetOutlet(outlet ?? NavigationOutlets.MAIN);
            if (target == null)
                return;

            m_dispatcher.Invoke(target.ClearHistory);
        }

        public Task<bool> EvictAsync(string routeKey, string? outlet = null)
        {
            if (routeKey == null)
                throw new ArgumentNullException(nameof(routeKey));

            var outletName = outlet ?? (m_routes.TryGet(routeKey, out var route) ? route.Outlet : NavigationOutlets.MAIN);
            var target = GetOutlet(outletName);

            if (target == null)
                return Task.FromResult(false);

            return RunOnDispatcherAsync(async () => await target.EvictAsync(routeKey));
        }

        #endregion

        #region Tools

        /// <summary>
        /// Hops to the UI thread, builds the request there (journal reads must happen on
        /// that thread), runs the pipeline and raises the failure event. <see cref="Navigated"/>
        /// is raised by the pipeline itself, the moment the outlet's content changes — before
        /// the screen has finished loading. A null request means "nothing to do" — Rejected.
        /// </summary>
        private Task<NavigationResult> RunAsync(NavigationOutlet outlet, Func<Task<NavigationRequest?>> requestFactory)
        {
            return RunOnDispatcherAsync(async () =>
            {
                NavigationRequest? request;

                try
                {
                    request = await requestFactory();
                }
                catch (NavigationRouteMissingException e)
                {
                    return Fail(outlet, NavigationStatus.RouteNotFound, e.RouteKey, outlet.Name);
                }

                if (request == null)
                    return new NavigationResult(NavigationStatus.Rejected, outlet.RouteKey ?? string.Empty, outlet.Name);

                var result = await m_pipeline.RunAsync(request);

                if (IsFailure(result.Status))
                    NavigationFailed?.Invoke(outlet, result);

                return result;
            });
        }

        private void RaiseNavigated(NavigationOutlet outlet, NavigationResult result)
        {
            Navigated?.Invoke(outlet, result);
        }

        private NavigationResult Fail(NavigationOutlet? outlet, NavigationStatus status, string routeKey, string outletName)
        {
            var result = new NavigationResult(status, routeKey, outletName);

            m_logger?.LogWarning("Navigation to {Route} in {Outlet}: {Status}", routeKey, outletName, status);
            NavigationFailed?.Invoke(outlet, result);

            return result;
        }

        private Task<TResult> RunOnDispatcherAsync<TResult>(Func<Task<TResult>> action)
        {
            if (m_dispatcher.CheckAccess())
                return action();

            return m_dispatcher.InvokeAsync(action).Unwrap();
        }

        private IReadOnlyList<INavigationGuard> ResolveGuards()
        {
            return m_guards ??= m_provider.GetServices<INavigationGuard>().ToArray();
        }

        private static bool IsFailure(NavigationStatus status)
        {
            return status is NavigationStatus.Failed
                or NavigationStatus.RouteNotFound
                or NavigationStatus.OutletNotFound;
        }

        #endregion

        #region Properties

        public IReadOnlyList<INavigationOutlet> Outlets
        {
            get
            {
                lock (m_sync)
                    return m_outletsOrder.ToArray();
            }
        }

        #endregion

        #region Classes

        private sealed class NavigationRouteMissingException : Exception
        {
            public NavigationRouteMissingException(string routeKey)
                : base($"Route '{routeKey}' is no longer registered.")
            {
                RouteKey = routeKey;
            }

            public string RouteKey { get; }
        }

        #endregion
    }
}
