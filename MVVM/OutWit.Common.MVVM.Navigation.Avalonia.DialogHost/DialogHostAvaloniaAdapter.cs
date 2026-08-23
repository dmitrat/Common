using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DialogHostAvalonia;
using OutWit.Common.MVVM.Interfaces;
using OutWit.Common.MVVM.Navigation.Interfaces;

namespace OutWit.Common.MVVM.Navigation.Avalonia.DialogHost
{
    /// <summary>
    /// Shows navigation dialogs through <c>DialogHost.Avalonia</c>, for an application that is
    /// already themed with Material.Avalonia and wants its dialogs to look like the rest of it.
    /// </summary>
    /// <remarks>
    /// The application places a <c>DialogHostAvalonia.DialogHost</c> in its window with an
    /// <c>Identifier</c> matching the host name — <c>"Root"</c> for the default — and picks this
    /// adapter with <c>UseDialogHost&lt;DialogHostAvaloniaAdapter&gt;()</c>. Everything above
    /// stays the same: view models still implement <see cref="IDialogAware{TResult}"/> and know
    /// nothing about which host shows them.
    /// </remarks>
    public sealed class DialogHostAvaloniaAdapter : IDialogHost
    {
        #region Fields

        private readonly Dictionary<string, Session> m_open = new(StringComparer.Ordinal);

        private readonly IDispatcher m_dispatcher;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the adapter. Resolved from DI by
        /// <c>AddAvaloniaNavigation(o =&gt; o.UseDialogHost&lt;DialogHostAvaloniaAdapter&gt;())</c>.
        /// </summary>
        /// <param name="dispatcher">The UI-thread dispatcher.</param>
        public DialogHostAvaloniaAdapter(IDispatcher dispatcher)
        {
            m_dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        #endregion

        #region IDialogHost

        public bool IsOpen(string host)
        {
            return DialogHostAvalonia.DialogHost.IsDialogOpen(host);
        }

        public async Task ShowAsync(string host, object view, Func<Task<bool>> canDismiss, CancellationToken cancellation)
        {
            if (m_open.ContainsKey(host))
                throw new InvalidOperationException($"Host '{host}' already shows a dialog, and DialogHost.Avalonia does not nest.");

            var session = new Session(canDismiss);
            m_open[host] = session;

            using var registration = cancellation.Register(() => m_dispatcher.Invoke(() => Close(host)));

            try
            {
                await DialogHostAvalonia.DialogHost.Show(view, host, session.OnClosing);
            }
            finally
            {
                if (m_open.TryGetValue(host, out var current) && ReferenceEquals(current, session))
                    m_open.Remove(host);
            }
        }

        public void Close(string host)
        {
            if (!m_open.TryGetValue(host, out var session))
                return;

            session.CloseByHost();

            DialogHostAvalonia.DialogHost.GetDialogSession(host)?.Close();
        }

        #endregion

        #region Properties

        /// <summary>
        /// False: DialogHost.Avalonia keeps one session per identifier.
        /// </summary>
        public bool SupportsNesting => false;

        #endregion

        #region Classes

        /// <summary>
        /// One open dialog. Its job is to tell a close the dialog service asked for from one
        /// the user started — a click on the overlay, or the library's own close command —
        /// because only the second has to be put to <see cref="IDialogAware{TResult}.CanCloseAsync"/>.
        /// </summary>
        private sealed class Session
        {
            private readonly Func<Task<bool>> m_canDismiss;

            private bool m_closingByHost;
            private bool m_asking;

            public Session(Func<Task<bool>> canDismiss)
            {
                m_canDismiss = canDismiss;
            }

            public void CloseByHost()
            {
                m_closingByHost = true;
            }

            public async void OnClosing(object? sender, DialogClosingEventArgs e)
            {
                if (m_closingByHost || !e.CanBeCancelled)
                    return;

                // veto now, ask, and close for real only if the dialog agrees
                e.Cancel();

                if (m_asking)
                    return;

                m_asking = true;

                try
                {
                    if (!await m_canDismiss())
                        return;

                    m_closingByHost = true;
                    e.Session.Close();
                }
                catch
                {
                    // the dialog service logs; a failing guard keeps the dialog open
                }
                finally
                {
                    m_asking = false;
                }
            }
        }

        #endregion
    }
}
