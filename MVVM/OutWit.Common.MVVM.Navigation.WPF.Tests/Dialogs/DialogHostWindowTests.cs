using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using NUnit.Framework;
using OutWit.Common.MVVM.Abstractions;
using OutWit.Common.MVVM.Navigation.WPF.Dialogs;
using OutWit.Common.MVVM.Navigation.WPF.Tests.Mock;

namespace OutWit.Common.MVVM.Navigation.WPF.Tests.Dialogs
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class DialogHostWindowTests
    {
        [SetUp]
        public void Setup()
        {
            WpfTestHost.InstallSynchronizationContext();
        }

        #region Show And Close Tests

        [Test]
        public void OwnerlessViewIsShownAsWindowAndCloseClosesItTest()
        {
            var host = new DialogHostWindow(new FixedTopLevelProvider(null), DispatcherImmediate.Instance);
            var view = new TextBlock { Text = "dialog" };

            var showing = host.ShowAsync(DialogHosts.ROOT, view, () => Task.FromResult(true), CancellationToken.None);
            WpfTestHost.PumpUntil(() => view.IsLoaded);

            Assert.That(host.IsOpen(DialogHosts.ROOT), Is.True);
            Assert.That(host.SupportsNesting, Is.True);
            Assert.That(view.IsLoaded, Is.True);
            Assert.That(showing.IsCompleted, Is.False);

            host.Close(DialogHosts.ROOT);
            WpfTestHost.PumpUntil(() => showing.IsCompleted);

            Assert.That(showing.IsCompletedSuccessfully, Is.True);
            Assert.That(host.IsOpen(DialogHosts.ROOT), Is.False);
        }

        [Test]
        public void WindowViewWithOwnerIsShownModallyTest()
        {
            var owner = new Window { Width = 100, Height = 100, ShowInTaskbar = false, WindowStyle = WindowStyle.None };
            owner.Show();
            var host = new DialogHostWindow(new FixedTopLevelProvider(owner), DispatcherImmediate.Instance);
            var dialog = new Window { Width = 50, Height = 50, ShowInTaskbar = false, WindowStyle = WindowStyle.None };

            // ShowDialog runs a nested loop on this thread; queue the close before pumping so the
            // loop has something that ends it
            var showing = host.ShowAsync(DialogHosts.ROOT, dialog, () => Task.FromResult(true), CancellationToken.None);
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new System.Action(() =>
            {
                Assert.That(dialog.IsVisible, Is.True);
                Assert.That(dialog.Owner, Is.SameAs(owner));
                host.Close(DialogHosts.ROOT);
            }));

            WpfTestHost.PumpUntil(() => showing.IsCompleted);

            Assert.That(showing.IsCompletedSuccessfully, Is.True);
            Assert.That(host.IsOpen(DialogHosts.ROOT), Is.False);

            owner.Close();
        }

        [Test]
        public void UserCloseGoesThroughCanDismissTest()
        {
            var host = new DialogHostWindow(new FixedTopLevelProvider(null), DispatcherImmediate.Instance);
            var dialog = new Window { Width = 50, Height = 50, ShowInTaskbar = false, WindowStyle = WindowStyle.None };
            var allow = false;
            var asked = 0;

            var showing = host.ShowAsync(DialogHosts.ROOT, dialog, () => { asked++; return Task.FromResult(allow); }, CancellationToken.None);
            WpfTestHost.PumpUntil(() => dialog.IsVisible);

            dialog.Close();
            WpfTestHost.DoEvents();

            Assert.That(asked, Is.EqualTo(1));
            Assert.That(dialog.IsVisible, Is.True);
            Assert.That(host.IsOpen(DialogHosts.ROOT), Is.True);

            allow = true;
            dialog.Close();
            WpfTestHost.PumpUntil(() => showing.IsCompleted);

            Assert.That(asked, Is.EqualTo(2));
            Assert.That(host.IsOpen(DialogHosts.ROOT), Is.False);
        }

        [Test]
        public void CancellationClosesWindowTest()
        {
            var host = new DialogHostWindow(new FixedTopLevelProvider(null), DispatcherImmediate.Instance);
            var dialog = new Window { Width = 50, Height = 50, ShowInTaskbar = false, WindowStyle = WindowStyle.None };
            using var cancellation = new CancellationTokenSource();

            var showing = host.ShowAsync(DialogHosts.ROOT, dialog, () => Task.FromResult(true), cancellation.Token);
            WpfTestHost.PumpUntil(() => dialog.IsVisible);

            cancellation.Cancel();
            WpfTestHost.PumpUntil(() => showing.IsCompleted);

            Assert.That(host.IsOpen(DialogHosts.ROOT), Is.False);
            Assert.That(dialog.IsVisible, Is.False);
        }

        #endregion
    }
}
