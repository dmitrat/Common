using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using OutWit.Common.MVVM.Interfaces;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.WPF.Interfaces;

namespace OutWit.Common.MVVM.Navigation.WPF.Dialogs
{
    /// <summary>
    /// Shows dialogs as modal windows over the active window — no external dependency, and
    /// dialogs nest. A view that is itself a <see cref="Window"/> is shown as is; any other
    /// view is wrapped in a window that takes its style from the application resource
    /// <see cref="STYLE_KEY"/> when one is defined.
    /// </summary>
    public sealed class DialogHostWindow : IDialogHost
    {
        #region Constants

        /// <summary>
        /// Resource key of an optional <c>Style TargetType="Window"</c> applied to wrapper windows.
        /// </summary>
        public const string STYLE_KEY = "OutWit.Navigation.DialogWindow";

        #endregion

        #region Fields

        private readonly Dictionary<string, Stack<Entry>> m_open = new(StringComparer.Ordinal);

        private readonly ITopLevelProvider m_topLevels;
        private readonly IDispatcher m_dispatcher;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the host. Resolved from DI by <c>services.AddWpfNavigation()</c>.
        /// </summary>
        /// <param name="topLevels">Where to find the owner window.</param>
        /// <param name="dispatcher">The UI-thread dispatcher.</param>
        public DialogHostWindow(ITopLevelProvider topLevels, IDispatcher dispatcher)
        {
            m_topLevels = topLevels ?? throw new ArgumentNullException(nameof(topLevels));
            m_dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        #endregion

        #region IDialogHost

        public bool IsOpen(string host)
        {
            return m_open.TryGetValue(host, out var stack) && stack.Count > 0;
        }

        public async Task ShowAsync(string host, object view, Func<Task<bool>> canDismiss, CancellationToken cancellation)
        {
            var owner = m_topLevels.GetActive();
            var window = view as Window ?? Wrap(view);

            if (owner != null && !ReferenceEquals(owner, window))
                window.Owner = owner;

            var entry = new Entry(window, canDismiss);
            var closed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            window.Closing += entry.OnClosing;
            window.Closed += (_, _) => closed.TrySetResult(true);
            Push(host, entry);

            using var registration = cancellation.Register(() => m_dispatcher.Invoke(entry.CloseByHost));

            try
            {
                // ShowDialog runs a nested message loop and does not return until the window
                // closes. Posting it lets this method return now, so the dialog service can run
                // OnOpenedAsync while the window is up; the nested loop keeps pumping everything.
                _ = window.Dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        if (window.Owner != null)
                            window.ShowDialog();
                        else
                            window.Show();
                    }
                    catch (InvalidOperationException)
                    {
                        // closed before it could be shown — the cancellation token was already set
                        closed.TrySetResult(true);
                    }
                });

                await closed.Task;
            }
            finally
            {
                window.Closing -= entry.OnClosing;
                Pop(host, entry);
            }
        }

        public void Close(string host)
        {
            if (m_open.TryGetValue(host, out var stack) && stack.Count > 0)
                stack.Peek().CloseByHost();
        }

        #endregion

        #region Functions

        private void Push(string host, Entry entry)
        {
            if (!m_open.TryGetValue(host, out var stack))
            {
                stack = new Stack<Entry>();
                m_open[host] = stack;
            }

            stack.Push(entry);
        }

        private void Pop(string host, Entry entry)
        {
            if (!m_open.TryGetValue(host, out var stack))
                return;

            if (stack.Count > 0 && ReferenceEquals(stack.Peek(), entry))
                stack.Pop();

            if (stack.Count == 0)
                m_open.Remove(host);
        }

        private static Window Wrap(object view)
        {
            var window = new Window
            {
                Content = view,
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false
            };

            window.SetResourceReference(FrameworkElement.StyleProperty, STYLE_KEY);

            return window;
        }

        #endregion

        #region Properties

        public bool SupportsNesting => true;

        #endregion

        #region Classes

        private sealed class Entry
        {
            private readonly Func<Task<bool>> m_canDismiss;
            private bool m_closingByHost;
            private bool m_asking;

            public Entry(Window window, Func<Task<bool>> canDismiss)
            {
                Window = window;
                m_canDismiss = canDismiss;
            }

            public Window Window { get; }

            public void CloseByHost()
            {
                m_closingByHost = true;
                Window.Close();
            }

            public async void OnClosing(object? sender, CancelEventArgs e)
            {
                if (m_closingByHost)
                    return;

                // the user closes from the UI: veto now, ask, and close for real if allowed
                e.Cancel = true;

                if (m_asking)
                    return;

                m_asking = true;

                try
                {
                    // WPF refuses Close() from inside Closing; when the guard answered synchronously
                    // we are still inside it, so the real close is always posted
                    if (await m_canDismiss())
                        Window.Dispatcher.BeginInvoke(new Action(CloseByHost));
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
