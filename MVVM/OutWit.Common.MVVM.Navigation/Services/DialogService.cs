using System;
using System.Collections.Generic;
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
    /// Default <see cref="IDialogService"/>. Platform-neutral: builds the view through
    /// <see cref="IViewFactory"/> and shows it through <see cref="IDialogHost"/>. Runs on
    /// the UI thread; callers may come from anywhere.
    /// </summary>
    public sealed class DialogService : IDialogService
    {
        #region Fields

        private readonly Dictionary<string, Stack<DialogSession>> m_sessions = new(StringComparer.Ordinal);

        private readonly IServiceProvider m_provider;
        private readonly IViewFactory m_viewFactory;
        private readonly IDialogHost m_host;
        private readonly IDispatcher m_dispatcher;
        private readonly ILogger<DialogService>? m_logger;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the service. Resolved from DI by <c>services.AddNavigation()</c> once the
        /// platform package has registered <see cref="IViewFactory"/> and <see cref="IDialogHost"/>.
        /// </summary>
        /// <param name="provider">The root service provider; dialog view models and their scopes come from it.</param>
        /// <param name="viewFactory">Builds views for dialog view models.</param>
        /// <param name="host">Shows the views.</param>
        /// <param name="dispatcher">The UI-thread dispatcher.</param>
        /// <param name="logger">Optional logger.</param>
        public DialogService(IServiceProvider provider,
                             IViewFactory viewFactory,
                             IDialogHost host,
                             IDispatcher dispatcher,
                             ILogger<DialogService>? logger = null)
        {
            m_provider = provider ?? throw new ArgumentNullException(nameof(provider));
            m_viewFactory = viewFactory ?? throw new ArgumentNullException(nameof(viewFactory));
            m_host = host ?? throw new ArgumentNullException(nameof(host));
            m_dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            m_logger = logger;
        }

        #endregion

        #region IDialogService

        public Task<DialogResult<TResult>> ShowAsync<TResult>(IDialogAware<TResult> viewModel,
                                                              string? host = null,
                                                              CancellationToken cancellation = default)
        {
            if (viewModel == null)
                throw new ArgumentNullException(nameof(viewModel));

            return RunOnDispatcherAsync(() => ShowCoreAsync(viewModel, NavigationParameters.EMPTY, host ?? DialogHosts.ROOT, cancellation));
        }

        public Task<DialogResult<TResult>> ShowAsync<TViewModel, TResult>(NavigationParameters? parameters = null,
                                                                          string? host = null,
                                                                          CancellationToken cancellation = default)
            where TViewModel : class, IDialogAware<TResult>
        {
            return RunOnDispatcherAsync(async () =>
            {
                using var scope = m_provider.CreateScope();

                var viewModel = ActivatorUtilities.CreateInstance<TViewModel>(scope.ServiceProvider);

                try
                {
                    return await ShowCoreAsync(viewModel, parameters ?? NavigationParameters.EMPTY, host ?? DialogHosts.ROOT, cancellation);
                }
                finally
                {
                    await DisposeSafelyAsync(viewModel);
                }
            });
        }

        public bool IsOpen(string? host = null)
        {
            return m_host.IsOpen(host ?? DialogHosts.ROOT);
        }

        public void Close(string? host = null)
        {
            var hostName = host ?? DialogHosts.ROOT;

            _ = RunOnDispatcherAsync(async () =>
            {
                try
                {
                    var session = Peek(hostName);
                    if (session != null)
                        await session.RequestCancelAsync();
                }
                catch (Exception e)
                {
                    m_logger?.LogError(e, "Closing dialog on host {Host} failed", hostName);
                }

                return true;
            });
        }

        #endregion

        #region Functions

        private async Task<DialogResult<TResult>> ShowCoreAsync<TResult>(IDialogAware<TResult> viewModel,
                                                                         NavigationParameters parameters,
                                                                         string host,
                                                                         CancellationToken cancellation)
        {
            if (m_host.IsOpen(host) && !m_host.SupportsNesting)
            {
                m_logger?.LogWarning("Dialog {ViewModel} not shown: host {Host} is busy and does not nest", viewModel.GetType().FullName, host);
                return DialogResult<TResult>.Cancelled();
            }

            object view;

            try
            {
                view = m_viewFactory.Build(viewModel);
            }
            catch (Exception e)
            {
                m_logger?.LogError(e, "Dialog {ViewModel} not shown: building its view failed", viewModel.GetType().FullName);
                return DialogResult<TResult>.Cancelled();
            }

            var session = new DialogSession<TResult>(host, viewModel, m_host, cancellation, m_logger);
            var closed = false;

            Push(session);
            session.Attach();

            try
            {
                var showing = m_host.ShowAsync(host, view, session.CanDismissAsync, cancellation);

                try
                {
                    await viewModel.OnOpenedAsync(parameters, cancellation);
                }
                catch (Exception e)
                {
                    m_logger?.LogError(e, "Dialog {ViewModel}: OnOpenedAsync failed, closing", viewModel.GetType().FullName);
                    m_host.Close(host);
                }

                await showing;
                closed = true;
            }
            catch (OperationCanceledException)
            {
                closed = true;
            }
            catch (Exception e)
            {
                m_logger?.LogError(e, "Dialog {ViewModel} failed", viewModel.GetType().FullName);
            }
            finally
            {
                session.Detach();
                Pop(session);

                if (!closed)
                    CloseSafely(host);
            }

            return session.Result;
        }

        private void Push(DialogSession session)
        {
            if (!m_sessions.TryGetValue(session.Host, out var stack))
            {
                stack = new Stack<DialogSession>();
                m_sessions[session.Host] = stack;
            }

            stack.Push(session);
        }

        private void Pop(DialogSession session)
        {
            if (!m_sessions.TryGetValue(session.Host, out var stack))
                return;

            if (stack.Count > 0 && ReferenceEquals(stack.Peek(), session))
                stack.Pop();

            if (stack.Count == 0)
                m_sessions.Remove(session.Host);
        }

        private DialogSession? Peek(string host)
        {
            return m_sessions.TryGetValue(host, out var stack) && stack.Count > 0 ? stack.Peek() : null;
        }

        private void CloseSafely(string host)
        {
            try
            {
                if (m_host.IsOpen(host))
                    m_host.Close(host);
            }
            catch (Exception e)
            {
                m_logger?.LogError(e, "Closing host {Host} after a failure failed", host);
            }
        }

        private async ValueTask DisposeSafelyAsync(object viewModel)
        {
            try
            {
                if (viewModel is IAsyncDisposable asyncDisposable)
                    await asyncDisposable.DisposeAsync();
                else if (viewModel is IDisposable disposable)
                    disposable.Dispose();
            }
            catch (Exception e)
            {
                m_logger?.LogError(e, "Disposing dialog {ViewModel} failed", viewModel.GetType().FullName);
            }
        }

        private Task<TResult> RunOnDispatcherAsync<TResult>(Func<Task<TResult>> action)
        {
            if (m_dispatcher.CheckAccess())
                return action();

            return m_dispatcher.InvokeAsync(action).Unwrap();
        }

        #endregion
    }
}
