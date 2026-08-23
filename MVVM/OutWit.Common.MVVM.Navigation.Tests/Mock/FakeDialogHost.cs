using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.MVVM.Navigation.Interfaces;

namespace OutWit.Common.MVVM.Navigation.Tests.Mock
{
    /// <summary>
    /// An IDialogHost that keeps a stack of open views per host name and lets a test play
    /// the UI: <see cref="DismissAsync"/> is the user clicking outside or on the close button.
    /// </summary>
    public sealed class FakeDialogHost : IDialogHost
    {
        #region Fields

        private readonly Dictionary<string, Stack<Entry>> m_open = new(StringComparer.Ordinal);

        #endregion

        #region IDialogHost

        public bool IsOpen(string host)
        {
            return m_open.TryGetValue(host, out var stack) && stack.Count > 0;
        }

        public Task ShowAsync(string host, object view, Func<Task<bool>> canDismiss, CancellationToken cancellation)
        {
            var entry = new Entry(view, canDismiss);

            if (!m_open.TryGetValue(host, out var stack))
            {
                stack = new Stack<Entry>();
                m_open[host] = stack;
            }

            stack.Push(entry);
            ShownViews.Add(view);

            cancellation.Register(() => Complete(host, entry));

            return entry.Closed.Task;
        }

        public void Close(string host)
        {
            if (!m_open.TryGetValue(host, out var stack) || stack.Count == 0)
                return;

            Complete(host, stack.Peek());
        }

        #endregion

        #region Functions

        /// <summary>
        /// The user tries to close the topmost dialog of the host from the UI.
        /// </summary>
        public async Task<bool> DismissAsync(string host = DialogHosts.ROOT)
        {
            if (!m_open.TryGetValue(host, out var stack) || stack.Count == 0)
                return false;

            var entry = stack.Peek();

            if (!await entry.CanDismiss())
                return false;

            Complete(host, entry);
            return true;
        }

        public object? TopView(string host = DialogHosts.ROOT)
        {
            return m_open.TryGetValue(host, out var stack) && stack.Count > 0 ? stack.Peek().View : null;
        }

        private void Complete(string host, Entry entry)
        {
            if (m_open.TryGetValue(host, out var stack) && stack.Count > 0 && ReferenceEquals(stack.Peek(), entry))
                stack.Pop();

            entry.Closed.TrySetResult(true);
        }

        #endregion

        #region Properties

        public bool SupportsNesting { get; set; }

        public List<object> ShownViews { get; } = new();

        #endregion

        #region Classes

        private sealed class Entry
        {
            public Entry(object view, Func<Task<bool>> canDismiss)
            {
                View = view;
                CanDismiss = canDismiss;
            }

            public object View { get; }

            public Func<Task<bool>> CanDismiss { get; }

            public TaskCompletionSource<bool> Closed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        #endregion
    }
}
