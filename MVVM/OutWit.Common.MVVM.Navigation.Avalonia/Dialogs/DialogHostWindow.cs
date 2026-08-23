using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using OutWit.Common.MVVM.Interfaces;
using OutWit.Common.MVVM.Navigation.Avalonia.Interfaces;
using OutWit.Common.MVVM.Navigation.Interfaces;

namespace OutWit.Common.MVVM.Navigation.Avalonia.Dialogs
{
    /// <summary>
    /// Shows dialogs as modal windows over the active window — no external dependency, and
    /// dialogs nest. A view that is itself a <see cref="Window"/> is shown as is; any other
    /// view is wrapped in a window carrying the <c>navigation-dialog</c> class for styling.
    /// </summary>
    public sealed class DialogHostWindow : IDialogHost
    {
        #region Constants

        public const string CLASS_NAME = "navigation-dialog";

        #endregion

        #region Fields

        private readonly Dictionary<string, Stack<Entry>> m_open = new(StringComparer.Ordinal);

        private readonly ITopLevelProvider m_topLevels;
        private readonly IDispatcher m_dispatcher;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the host. Resolved from DI by <c>services.AddAvaloniaNavigation(o => o.UseWindowDialogs())</c>.
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
            var owner = m_topLevels.GetActive() as Window;
            var window = view as Window ?? Wrap(view);
            var entry = new Entry(window, canDismiss);

            window.Closing += entry.OnClosing;
            Push(host, entry);

            using var registration = cancellation.Register(() => m_dispatcher.Invoke(entry.CloseByHost));

            try
            {
                if (owner != null && !ReferenceEquals(owner, window))
                {
                    await window.ShowDialog(owner);
                }
                else
                {
                    var closed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    window.Closed += (_, _) => closed.TrySetResult(true);
                    window.Show();
                    await closed.Task;
                }
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
                CanResize = false,
                ShowInTaskbar = false
            };

            window.Classes.Add(CLASS_NAME);

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

            public async void OnClosing(object? sender, WindowClosingEventArgs e)
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
                    if (await m_canDismiss())
                        CloseByHost();
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
