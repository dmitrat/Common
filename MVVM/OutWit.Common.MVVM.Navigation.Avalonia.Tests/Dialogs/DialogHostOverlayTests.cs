using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using NUnit.Framework;
using OutWit.Common.MVVM.Abstractions;
using OutWit.Common.MVVM.Navigation.Avalonia.Dialogs;
using OutWit.Common.MVVM.Navigation.Avalonia.Tests.Mock;

namespace OutWit.Common.MVVM.Navigation.Avalonia.Tests.Dialogs
{
    [TestFixture]
    public class DialogHostOverlayTests
    {
        #region Show And Close Tests

        [AvaloniaTest]
        public async Task ShowPutsOverlayIntoLayerAndCloseRemovesItTest()
        {
            var window = new Window { Width = 400, Height = 300 };
            window.Show();
            var host = new DialogHostOverlay(new FixedTopLevelProvider(window), DispatcherImmediate.Instance);
            var view = new TextBlock { Text = "dialog" };

            var showing = host.ShowAsync(DialogHosts.ROOT, view, () => Task.FromResult(true), CancellationToken.None);
            var layer = OverlayLayer.GetOverlayLayer(window)!;

            Assert.That(host.IsOpen(DialogHosts.ROOT), Is.True);
            Assert.That(layer.Children.OfType<DialogOverlay>().Count(), Is.EqualTo(1));
            Assert.That(layer.Children.OfType<DialogOverlay>().Single().Content, Is.SameAs(view));
            Assert.That(showing.IsCompleted, Is.False);

            host.Close(DialogHosts.ROOT);
            await showing;

            Assert.That(host.IsOpen(DialogHosts.ROOT), Is.False);
            Assert.That(layer.Children.OfType<DialogOverlay>(), Is.Empty);

            window.Close();
        }

        [AvaloniaTest]
        public async Task OverlayDoesNotNestTest()
        {
            var window = new Window();
            window.Show();
            var host = new DialogHostOverlay(new FixedTopLevelProvider(window), DispatcherImmediate.Instance);

            Assert.That(host.SupportsNesting, Is.False);

            var first = host.ShowAsync(DialogHosts.ROOT, new TextBlock(), () => Task.FromResult(true), CancellationToken.None);

            Assert.That(() => host.ShowAsync(DialogHosts.ROOT, new TextBlock(), () => Task.FromResult(true), CancellationToken.None),
                Throws.InvalidOperationException);

            host.Close(DialogHosts.ROOT);
            await first;

            Assert.That(host.IsOpen(DialogHosts.ROOT), Is.False);

            window.Close();
        }

        [AvaloniaTest]
        public async Task CancellationClosesOverlayTest()
        {
            var window = new Window();
            window.Show();
            var host = new DialogHostOverlay(new FixedTopLevelProvider(window), DispatcherImmediate.Instance);
            using var cancellation = new CancellationTokenSource();

            var showing = host.ShowAsync(DialogHosts.ROOT, new TextBlock(), () => Task.FromResult(true), cancellation.Token);
            cancellation.Cancel();
            await showing;

            Assert.That(host.IsOpen(DialogHosts.ROOT), Is.False);

            window.Close();
        }

        [AvaloniaTest]
        public void NoTopLevelThrowsTest()
        {
            var host = new DialogHostOverlay(new FixedTopLevelProvider(null), DispatcherImmediate.Instance);

            Assert.That(() => host.ShowAsync(DialogHosts.ROOT, new TextBlock(), () => Task.FromResult(true), CancellationToken.None),
                Throws.InvalidOperationException);
        }

        #endregion

        #region Dismiss Tests

        [AvaloniaTest]
        public async Task EscapeGoesThroughCanDismissTest()
        {
            var window = new Window();
            window.Show();
            var host = new DialogHostOverlay(new FixedTopLevelProvider(window), DispatcherImmediate.Instance);
            var allow = false;
            var asked = 0;

            var showing = host.ShowAsync(DialogHosts.ROOT, new TextBlock(), () => { asked++; return Task.FromResult(allow); }, CancellationToken.None);
            var overlay = OverlayLayer.GetOverlayLayer(window)!.Children.OfType<DialogOverlay>().Single();

            PressEscape(overlay);
            await Task.Yield();

            Assert.That(asked, Is.EqualTo(1));
            Assert.That(host.IsOpen(DialogHosts.ROOT), Is.True);

            allow = true;
            PressEscape(overlay);
            await showing;

            Assert.That(asked, Is.EqualTo(2));
            Assert.That(host.IsOpen(DialogHosts.ROOT), Is.False);

            window.Close();
        }

        #endregion

        #region Tools

        private static void PressEscape(DialogOverlay overlay)
        {
            overlay.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = Key.Escape,
                Source = overlay
            });
        }

        #endregion
    }
}
