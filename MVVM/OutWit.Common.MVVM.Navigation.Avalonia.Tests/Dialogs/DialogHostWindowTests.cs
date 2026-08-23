using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using NUnit.Framework;
using OutWit.Common.MVVM.Abstractions;
using OutWit.Common.MVVM.Navigation.Avalonia.Dialogs;
using OutWit.Common.MVVM.Navigation.Avalonia.Tests.Mock;

namespace OutWit.Common.MVVM.Navigation.Avalonia.Tests.Dialogs
{
    [TestFixture]
    public class DialogHostWindowTests
    {
        #region Show And Close Tests

        [AvaloniaTest]
        public async Task ShowWrapsViewInOwnedWindowAndCloseClosesItTest()
        {
            var owner = new Window();
            owner.Show();
            var host = new DialogHostWindow(new FixedTopLevelProvider(owner), DispatcherImmediate.Instance);
            var view = new TextBlock { Text = "dialog" };

            var showing = host.ShowAsync(DialogHosts.ROOT, view, () => Task.FromResult(true), CancellationToken.None);
            await Task.Yield();

            Assert.That(host.IsOpen(DialogHosts.ROOT), Is.True);
            Assert.That(host.SupportsNesting, Is.True);
            var dialog = owner.OwnedWindows.Single();
            Assert.That(dialog.Content, Is.SameAs(view));
            Assert.That(dialog.Classes.Contains(DialogHostWindow.CLASS_NAME), Is.True);

            host.Close(DialogHosts.ROOT);
            await showing;

            Assert.That(host.IsOpen(DialogHosts.ROOT), Is.False);
            Assert.That(owner.OwnedWindows, Is.Empty);

            owner.Close();
        }

        [AvaloniaTest]
        public async Task WindowViewIsShownAsIsTest()
        {
            var owner = new Window();
            owner.Show();
            var host = new DialogHostWindow(new FixedTopLevelProvider(owner), DispatcherImmediate.Instance);
            var dialog = new Window();

            var showing = host.ShowAsync(DialogHosts.ROOT, dialog, () => Task.FromResult(true), CancellationToken.None);
            await Task.Yield();

            Assert.That(owner.OwnedWindows.Single(), Is.SameAs(dialog));

            host.Close(DialogHosts.ROOT);
            await showing;

            owner.Close();
        }

        [AvaloniaTest]
        public async Task NestedDialogsStackAndCloseTakesTopmostTest()
        {
            var owner = new Window();
            owner.Show();
            var host = new DialogHostWindow(new FixedTopLevelProvider(owner), DispatcherImmediate.Instance);
            var first = new Window();
            var second = new Window();

            var showingFirst = host.ShowAsync(DialogHosts.ROOT, first, () => Task.FromResult(true), CancellationToken.None);
            await Task.Yield();
            var showingSecond = host.ShowAsync(DialogHosts.ROOT, second, () => Task.FromResult(true), CancellationToken.None);
            await Task.Yield();

            host.Close(DialogHosts.ROOT);
            await showingSecond;

            Assert.That(showingFirst.IsCompleted, Is.False);
            Assert.That(host.IsOpen(DialogHosts.ROOT), Is.True);

            host.Close(DialogHosts.ROOT);
            await showingFirst;

            Assert.That(host.IsOpen(DialogHosts.ROOT), Is.False);

            owner.Close();
        }

        [AvaloniaTest]
        public async Task CancellationClosesWindowTest()
        {
            var owner = new Window();
            owner.Show();
            var host = new DialogHostWindow(new FixedTopLevelProvider(owner), DispatcherImmediate.Instance);
            using var cancellation = new CancellationTokenSource();

            var showing = host.ShowAsync(DialogHosts.ROOT, new Window(), () => Task.FromResult(true), cancellation.Token);
            await Task.Yield();
            cancellation.Cancel();
            await showing;

            Assert.That(host.IsOpen(DialogHosts.ROOT), Is.False);

            owner.Close();
        }

        #endregion

        #region Dismiss Tests

        [AvaloniaTest]
        public async Task UserCloseGoesThroughCanDismissTest()
        {
            var owner = new Window();
            owner.Show();
            var host = new DialogHostWindow(new FixedTopLevelProvider(owner), DispatcherImmediate.Instance);
            var dialog = new Window();
            var allow = false;
            var asked = 0;

            var showing = host.ShowAsync(DialogHosts.ROOT, dialog, () => { asked++; return Task.FromResult(allow); }, CancellationToken.None);
            await Task.Yield();

            dialog.Close();
            await Task.Yield();

            Assert.That(asked, Is.EqualTo(1));
            Assert.That(host.IsOpen(DialogHosts.ROOT), Is.True);
            Assert.That(showing.IsCompleted, Is.False);

            allow = true;
            dialog.Close();
            await showing;

            Assert.That(asked, Is.EqualTo(2));
            Assert.That(host.IsOpen(DialogHosts.ROOT), Is.False);

            owner.Close();
        }

        #endregion
    }
}
