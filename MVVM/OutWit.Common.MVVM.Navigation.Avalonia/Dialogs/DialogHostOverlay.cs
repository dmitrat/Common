using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using OutWit.Common.MVVM.Interfaces;
using OutWit.Common.MVVM.Navigation.Avalonia.Interfaces;
using OutWit.Common.MVVM.Navigation.Interfaces;

namespace OutWit.Common.MVVM.Navigation.Avalonia.Dialogs
{
    /// <summary>
    /// Shows dialogs on the <see cref="OverlayLayer"/> of the active top level — in-window,
    /// no external dependency. One dialog per host: overlays do not nest.
    /// </summary>
    public sealed class DialogHostOverlay : IDialogHost
    {
        #region Fields

        private readonly Dictionary<string, Entry> m_open = new(StringComparer.Ordinal);

        private readonly ITopLevelProvider m_topLevels;
        private readonly IDispatcher m_dispatcher;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the host. Resolved from DI by <c>services.AddAvaloniaNavigation(o => o.UseOverlayDialogs())</c>.
        /// </summary>
        /// <param name="topLevels">Where to find the active top level.</param>
        /// <param name="dispatcher">The UI-thread dispatcher.</param>
        public DialogHostOverlay(ITopLevelProvider topLevels, IDispatcher dispatcher)
        {
            m_topLevels = topLevels ?? throw new ArgumentNullException(nameof(topLevels));
            m_dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        #endregion

        #region IDialogHost

        public bool IsOpen(string host)
        {
            return m_open.ContainsKey(host);
        }

        public async Task ShowAsync(string host, object view, Func<Task<bool>> canDismiss, CancellationToken cancellation)
        {
            if (m_open.ContainsKey(host))
                throw new InvalidOperationException($"Host '{host}' already shows a dialog, and overlays do not nest.");

            var topLevel = m_topLevels.GetActive()
                           ?? throw new InvalidOperationException("There is no active TopLevel to show the dialog on.");

            var layer = OverlayLayer.GetOverlayLayer(topLevel)
                        ?? throw new InvalidOperationException("The active TopLevel has no OverlayLayer.");

            var overlay = new DialogOverlay { Content = view };
            var entry = new Entry(overlay, layer, canDismiss);

            overlay.DismissRequested += entry.OnDismissRequested;
            layer.SizeChanged += entry.OnLayerSizeChanged;
            entry.Fit();

            layer.Children.Add(overlay);
            m_open[host] = entry;

            overlay.Focus();

            using var registration = cancellation.Register(() => m_dispatcher.Invoke(() => Close(host)));

            try
            {
                await entry.Closed.Task;
            }
            finally
            {
                if (m_open.TryGetValue(host, out var current) && ReferenceEquals(current, entry))
                    m_open.Remove(host);

                overlay.DismissRequested -= entry.OnDismissRequested;
                layer.SizeChanged -= entry.OnLayerSizeChanged;
                layer.Children.Remove(overlay);
            }
        }

        public void Close(string host)
        {
            if (m_open.TryGetValue(host, out var entry))
                entry.Closed.TrySetResult(true);
        }

        #endregion

        #region Properties

        public bool SupportsNesting => false;

        #endregion

        #region Classes

        private sealed class Entry
        {
            private readonly Func<Task<bool>> m_canDismiss;
            private bool m_asking;

            public Entry(DialogOverlay overlay, OverlayLayer layer, Func<Task<bool>> canDismiss)
            {
                Overlay = overlay;
                Layer = layer;
                m_canDismiss = canDismiss;
            }

            public DialogOverlay Overlay { get; }

            public OverlayLayer Layer { get; }

            public TaskCompletionSource<bool> Closed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

            public void Fit()
            {
                Overlay.Width = Layer.Bounds.Width;
                Overlay.Height = Layer.Bounds.Height;
            }

            public void OnLayerSizeChanged(object? sender, SizeChangedEventArgs e)
            {
                Overlay.Width = e.NewSize.Width;
                Overlay.Height = e.NewSize.Height;
            }

            public async void OnDismissRequested()
            {
                if (m_asking || Closed.Task.IsCompleted)
                    return;

                m_asking = true;

                try
                {
                    if (await m_canDismiss())
                        Closed.TrySetResult(true);
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
