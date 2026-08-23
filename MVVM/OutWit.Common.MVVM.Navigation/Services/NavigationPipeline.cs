using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Model;

namespace OutWit.Common.MVVM.Navigation.Services
{
    /// <summary>
    /// One navigation, start to finish, on one outlet. Runs on the UI thread; see the
    /// specification's §6 for the numbered steps the code below follows.
    /// </summary>
    /// <remarks>
    /// The outlet's slot is held only up to and including the commit. The target's
    /// <see cref="INavigationAware.OnNavigatedToAsync"/> — the screen loading its data — runs
    /// with the slot already free, so a slow screen never blocks the next navigation, and a
    /// view model may navigate onwards from it. The token that call receives is cancelled as
    /// soon as the outlet shows something else.
    /// </remarks>
    internal sealed class NavigationPipeline
    {
        #region Fields

        /// <summary>
        /// The outlets whose gated section the current async flow is inside. A navigation
        /// into one of them from a guard would wait for a slot its own caller holds, so it is
        /// refused instead of hanging.
        /// </summary>
        private static readonly AsyncLocal<HashSet<string>?> GATED = new();

        private readonly IServiceProvider m_provider;
        private readonly Func<IReadOnlyList<INavigationGuard>> m_guards;
        private readonly Action<NavigationOutlet, NavigationResult> m_navigated;
        private readonly ILogger? m_logger;

        #endregion

        #region Constructors

        public NavigationPipeline(IServiceProvider provider,
                                  Func<IReadOnlyList<INavigationGuard>> guards,
                                  Action<NavigationOutlet, NavigationResult> navigated,
                                  ILogger? logger)
        {
            m_provider = provider ?? throw new ArgumentNullException(nameof(provider));
            m_guards = guards ?? throw new ArgumentNullException(nameof(guards));
            m_navigated = navigated ?? throw new ArgumentNullException(nameof(navigated));
            m_logger = logger;
        }

        #endregion

        #region Functions

        public async Task<NavigationResult> RunAsync(NavigationRequest request)
        {
            var outlet = request.Outlet;
            var route = request.Route;
            var parameters = request.Parameters;
            var context = new NavigationContext(outlet.Name, route, parameters, request.Kind);

            // 3. already there
            if (IsUnchanged(request))
                return Result(request, NavigationStatus.Unchanged);

            if (IsInsideGate(outlet.Name))
            {
                var error = new InvalidOperationException(
                    $"Navigation into outlet '{outlet.Name}' was requested from a guard of that same outlet. " +
                    "Guards run while the outlet is held; navigate from OnNavigatedToAsync, or after the guard has returned.");

                m_logger?.LogError(error, "Re-entrant navigation to {Route} in {Outlet} refused", route.Key, outlet.Name);

                return Result(request, NavigationStatus.Failed, error);
            }

            // 4. take the outlet's slot, displacing a preemptible navigation
            outlet.PreemptInFlight();
            outlet.EnterQueue();

            NavigationViewModelHandle? target = null;
            NavigationViewModelHandle? previous = null;
            NavigationTicket? ticket = null;
            CancellationToken contentToken = default;
            var targetCreated = false;
            var committed = false;
            var released = false;

            try
            {
                try
                {
                    await outlet.WaitAsync(request.Cancellation);
                }
                catch (OperationCanceledException)
                {
                    return Result(request, NavigationStatus.Cancelled);
                }

                ticket = outlet.Begin();
                EnterGate(outlet.Name);

                try
                {
                    using (var linked = CancellationTokenSource.CreateLinkedTokenSource(request.Cancellation, ticket.Token))
                    {
                        var token = linked.Token;

                        // 3 again: the state may have changed while we waited
                        if (IsUnchanged(request))
                            return Result(request, NavigationStatus.Unchanged);

                        var guards = m_guards();

                        // 5. global guards, leaving
                        foreach (var guard in guards)
                        {
                            if (!await guard.CanNavigateFromAsync(context, token))
                                return Result(request, NavigationStatus.Rejected);
                        }

                        if (token.IsCancellationRequested)
                            return Result(request, NavigationStatus.Cancelled);

                        // 6. current view model, leaving
                        if (outlet.Current?.ViewModel is INavigationGuard currentGuard
                            && !await currentGuard.CanNavigateFromAsync(context, token))
                            return Result(request, NavigationStatus.Rejected);

                        if (token.IsCancellationRequested)
                            return Result(request, NavigationStatus.Cancelled);

                        // 7. global guards, arriving — before the target exists
                        foreach (var guard in guards)
                        {
                            if (!await guard.CanNavigateToAsync(context, token))
                                return Result(request, NavigationStatus.Rejected);
                        }

                        if (token.IsCancellationRequested)
                            return Result(request, NavigationStatus.Cancelled);

                        // 8. target view model
                        try
                        {
                            target = outlet.GetOrCreate(route, CreateHandle, out targetCreated);
                        }
                        catch (Exception e)
                        {
                            m_logger?.LogError(e, "Navigation to {Route} in {Outlet}: creating {ViewModel} failed", route.Key, outlet.Name, route.ViewModelType.FullName);
                            return Result(request, NavigationStatus.Failed, e);
                        }

                        // 9. target view model, arriving
                        if (target.ViewModel is INavigationGuard targetGuard
                            && !await targetGuard.CanNavigateToAsync(context, token))
                            return Result(request, NavigationStatus.Rejected);

                        if (token.IsCancellationRequested)
                            return Result(request, NavigationStatus.Cancelled);
                    }

                    // ───── point of no return ─────
                    outlet.MakeFinal(ticket);

                    previous = outlet.Current;
                    Exception? leaveError = null;

                    // 10. current view model, left
                    if (previous?.ViewModel is INavigationAware previousAware)
                    {
                        try
                        {
                            await previousAware.OnNavigatedFromAsync(context, request.Cancellation);
                        }
                        catch (OperationCanceledException) when (request.Cancellation.IsCancellationRequested)
                        {
                        }
                        catch (Exception e)
                        {
                            leaveError = e;
                            m_logger?.LogError(e, "Navigation to {Route} in {Outlet}: OnNavigatedFromAsync of {ViewModel} failed", route.Key, outlet.Name, previous.Route.ViewModelType.FullName);
                        }
                    }

                    // 11. commit
                    contentToken = outlet.Commit(target, parameters, request.Kind, request.HistoryIndex);
                    committed = true;

                    // 12. the previous Transient view model goes
                    if (previous != null && !ReferenceEquals(previous, target) && previous.Route.Mode == NavigationRouteMode.Transient)
                        await DisposeSafelyAsync(previous);

                    // 13. the slot is free from here: the screen is shown, and loading its data
                    //     must not hold up whoever navigates next
                    LeaveGate(outlet.Name);
                    outlet.End(ticket);
                    released = true;

                    m_navigated(outlet, new NavigationResult(NavigationStatus.Success, route.Key, outlet.Name));

                    if (leaveError != null)
                        return Result(request, NavigationStatus.Failed, leaveError);
                }
                finally
                {
                    if (!released)
                    {
                        LeaveGate(outlet.Name);

                        if (!committed && targetCreated && target != null)
                            await DisposeSafelyAsync(target);

                        outlet.End(ticket);
                    }
                }

                // 14. target view model, arrived — outside the slot, with a token that trips
                //     as soon as the outlet moves on
                return await LoadAsync(request, context, target!, contentToken);
            }
            finally
            {
                outlet.ExitQueue();
            }
        }

        private async Task<NavigationResult> LoadAsync(NavigationRequest request,
                                                       NavigationContext context,
                                                       NavigationViewModelHandle target,
                                                       CancellationToken contentToken)
        {
            if (target.ViewModel is not INavigationAware aware)
                return Result(request, NavigationStatus.Success);

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(request.Cancellation, contentToken);

            try
            {
                await aware.OnNavigatedToAsync(context, linked.Token);
            }
            catch (OperationCanceledException) when (linked.IsCancellationRequested)
            {
                return Result(request, NavigationStatus.Cancelled);
            }
            catch (Exception e)
            {
                m_logger?.LogError(e, "Navigation to {Route} in {Outlet}: OnNavigatedToAsync of {ViewModel} failed",
                    request.Route.Key, request.Outlet.Name, request.Route.ViewModelType.FullName);

                return Result(request, NavigationStatus.Failed, e);
            }

            return Result(request, linked.IsCancellationRequested ? NavigationStatus.Cancelled : NavigationStatus.Success);
        }

        /// <summary>
        /// The dry run behind INavigationService.CanNavigateAsync: global guards, the
        /// current view model, and the target only when a cached instance exists.
        /// </summary>
        public async Task<bool> CanNavigateAsync(NavigationOutlet outlet, NavigationRoute route, NavigationParameters parameters, CancellationToken cancellation)
        {
            var context = new NavigationContext(outlet.Name, route, parameters, NavigationKind.New);
            var guards = m_guards();

            foreach (var guard in guards)
            {
                if (!await guard.CanNavigateFromAsync(context, cancellation))
                    return false;
            }

            if (outlet.Current?.ViewModel is INavigationGuard currentGuard
                && !await currentGuard.CanNavigateFromAsync(context, cancellation))
                return false;

            foreach (var guard in guards)
            {
                if (!await guard.CanNavigateToAsync(context, cancellation))
                    return false;
            }

            if (outlet.TryGetCached(route.Key, out var cached)
                && cached.ViewModel is INavigationGuard targetGuard
                && !await targetGuard.CanNavigateToAsync(context, cancellation))
                return false;

            return true;
        }

        private NavigationViewModelHandle CreateHandle(NavigationRoute route)
        {
            if (route.Mode == NavigationRouteMode.Transient)
            {
                var scope = m_provider.CreateScope();

                try
                {
                    var viewModel = ActivatorUtilities.CreateInstance(scope.ServiceProvider, route.ViewModelType);
                    return new NavigationViewModelHandle(route, viewModel, scope);
                }
                catch
                {
                    scope.Dispose();
                    throw;
                }
            }

            return new NavigationViewModelHandle(route, ActivatorUtilities.CreateInstance(m_provider, route.ViewModelType), null);
        }

        private async ValueTask DisposeSafelyAsync(NavigationViewModelHandle handle)
        {
            try
            {
                await handle.DisposeAsync();
            }
            catch (Exception e)
            {
                m_logger?.LogError(e, "Disposing {ViewModel} of route {Route} failed", handle.Route.ViewModelType.FullName, handle.Route.Key);
            }
        }

        private static bool IsUnchanged(NavigationRequest request)
        {
            return request.Kind == NavigationKind.New
                   && request.Route.Mode == NavigationRouteMode.Cached
                   && request.Outlet.IsShowing(request.Route, request.Parameters);
        }

        private static NavigationResult Result(NavigationRequest request, NavigationStatus status, Exception? error = null)
        {
            return new NavigationResult(status, request.Route.Key, request.Outlet.Name, error);
        }

        #endregion

        #region Re-entrancy

        private static bool IsInsideGate(string outlet)
        {
            return GATED.Value?.Contains(outlet) == true;
        }

        private static void EnterGate(string outlet)
        {
            // a fresh set per assignment: AsyncLocal shares the instance with parallel flows,
            // and mutating it in place would leak the marker into siblings
            var gated = GATED.Value == null
                ? new HashSet<string>(StringComparer.Ordinal)
                : new HashSet<string>(GATED.Value, StringComparer.Ordinal);

            gated.Add(outlet);
            GATED.Value = gated;
        }

        private static void LeaveGate(string outlet)
        {
            if (GATED.Value == null)
                return;

            var gated = new HashSet<string>(GATED.Value, StringComparer.Ordinal);
            gated.Remove(outlet);

            GATED.Value = gated.Count == 0 ? null : gated;
        }

        #endregion
    }
}
