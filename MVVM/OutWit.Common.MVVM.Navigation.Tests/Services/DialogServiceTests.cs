using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Model;
using OutWit.Common.MVVM.Navigation.Tests.Mock;

namespace OutWit.Common.MVVM.Navigation.Tests.Services
{
    [TestFixture]
    public class DialogServiceTests
    {
        #region Fields

        private ServiceProvider m_provider = null!;
        private IDialogService m_dialogs = null!;
        private FakeDialogHost m_host = null!;
        private FakeViewFactory m_views = null!;

        #endregion

        [SetUp]
        public void Setup()
        {
            m_host = new FakeDialogHost();
            m_views = new FakeViewFactory(typeof(DialogViewModel));

            m_provider = NavigationTestHost.Build(services: services =>
            {
                services.AddSingleton<IDialogHost>(m_host);
                services.AddSingleton<IViewFactory>(m_views);
            });

            m_dialogs = m_provider.GetRequiredService<IDialogService>();
        }

        [TearDown]
        public void TearDown()
        {
            m_provider.Dispose();
        }

        #region Result Tests

        [Test]
        public async Task ShowReturnsConfirmedResultWhenViewModelRequestsCloseTest()
        {
            var viewModel = new DialogViewModel();

            var pending = m_dialogs.ShowAsync(viewModel);
            Assert.That(m_dialogs.IsOpen(), Is.True);
            Assert.That(((FakeView)m_host.TopView()!).ViewModel, Is.SameAs(viewModel));

            viewModel.RequestClose(DialogResult<string>.Confirmed("renamed"));
            var result = await pending;

            Assert.That(result.IsConfirmed, Is.True);
            Assert.That(result.Value, Is.EqualTo("renamed"));
            Assert.That(m_dialogs.IsOpen(), Is.False);
        }

        [Test]
        public async Task ShowReturnsCancelledWhenViewModelCancelsTest()
        {
            var viewModel = new DialogViewModel();

            var pending = m_dialogs.ShowAsync(viewModel);
            viewModel.RequestClose(DialogResult<string>.Cancelled());
            var result = await pending;

            Assert.That(result.IsConfirmed, Is.False);
            Assert.That(result.Value, Is.Null);
        }

        [Test]
        public async Task ShowReturnsCancelledWhenDismissedFromUiTest()
        {
            var viewModel = new DialogViewModel();

            var pending = m_dialogs.ShowAsync(viewModel);
            var dismissed = await m_host.DismissAsync();
            var result = await pending;

            Assert.That(dismissed, Is.True);
            Assert.That(result.IsConfirmed, Is.False);
            Assert.That(viewModel.CanCloseCalls, Is.EqualTo(1));
        }

        [Test]
        public async Task OnOpenedReceivesParametersTest()
        {
            var parameters = new NavigationParameters(("name", "old"));

            var pending = m_dialogs.ShowAsync<DialogViewModel, string>(parameters);
            var viewModel = (DialogViewModel)((FakeView)m_host.TopView()!).ViewModel;

            Assert.That(viewModel.OpenedWith!.Is(parameters), Is.True);

            viewModel.RequestClose(DialogResult<string>.Confirmed("new"));
            var result = await pending;

            Assert.That(result.Value, Is.EqualTo("new"));
        }

        #endregion

        #region CanClose Tests

        [Test]
        public async Task CanCloseFalseKeepsDialogOpenTest()
        {
            var viewModel = new DialogViewModel { CanClose = false };

            var pending = m_dialogs.ShowAsync(viewModel);
            viewModel.RequestClose(DialogResult<string>.Confirmed("x"));

            Assert.That(m_dialogs.IsOpen(), Is.True);
            Assert.That(pending.IsCompleted, Is.False);

            viewModel.CanClose = true;
            viewModel.RequestClose(DialogResult<string>.Confirmed("y"));
            var result = await pending;

            Assert.That(result.Value, Is.EqualTo("y"));
        }

        [Test]
        public async Task DismissGoesThroughCanCloseTest()
        {
            var viewModel = new DialogViewModel { CanClose = false };

            var pending = m_dialogs.ShowAsync(viewModel);
            var dismissed = await m_host.DismissAsync();

            Assert.That(dismissed, Is.False);
            Assert.That(m_dialogs.IsOpen(), Is.True);

            viewModel.CanClose = true;
            Assert.That(await m_host.DismissAsync(), Is.True);
            Assert.That((await pending).IsConfirmed, Is.False);
        }

        [Test]
        public async Task CloseFromServiceAsksCanCloseTest()
        {
            var viewModel = new DialogViewModel { CanClose = false };

            var pending = m_dialogs.ShowAsync(viewModel);
            m_dialogs.Close();
            await Task.Yield();

            Assert.That(m_dialogs.IsOpen(), Is.True);

            viewModel.CanClose = true;
            m_dialogs.Close();
            var result = await pending;

            Assert.That(result.IsConfirmed, Is.False);
            Assert.That(m_dialogs.IsOpen(), Is.False);
        }

        #endregion

        #region Nesting Tests

        [Test]
        public async Task SecondShowOnBusyHostWithoutNestingReturnsCancelledTest()
        {
            m_host.SupportsNesting = false;
            var first = new DialogViewModel();
            var second = new DialogViewModel();

            var pendingFirst = m_dialogs.ShowAsync(first);
            var secondResult = await m_dialogs.ShowAsync(second);

            Assert.That(secondResult.IsConfirmed, Is.False);
            Assert.That(m_host.ShownViews, Has.Count.EqualTo(1));

            first.RequestClose(DialogResult<string>.Confirmed("a"));
            Assert.That((await pendingFirst).Value, Is.EqualTo("a"));
        }

        [Test]
        public async Task NestingStacksDialogsAndCloseTakesTopmostTest()
        {
            m_host.SupportsNesting = true;
            var first = new DialogViewModel();
            var second = new DialogViewModel();

            var pendingFirst = m_dialogs.ShowAsync(first);
            var pendingSecond = m_dialogs.ShowAsync(second);

            Assert.That(m_host.ShownViews, Has.Count.EqualTo(2));
            Assert.That(((FakeView)m_host.TopView()!).ViewModel, Is.SameAs(second));

            m_dialogs.Close();
            var secondResult = await pendingSecond;

            Assert.That(secondResult.IsConfirmed, Is.False);
            Assert.That(pendingFirst.IsCompleted, Is.False);
            Assert.That(m_dialogs.IsOpen(), Is.True);

            first.RequestClose(DialogResult<string>.Confirmed("a"));
            Assert.That((await pendingFirst).Value, Is.EqualTo("a"));
            Assert.That(m_dialogs.IsOpen(), Is.False);
        }

        [Test]
        public async Task HostsAreIndependentTest()
        {
            var first = new DialogViewModel();
            var second = new DialogViewModel();

            var pendingFirst = m_dialogs.ShowAsync(first);
            var pendingSecond = m_dialogs.ShowAsync(second, host: "Inspector");

            Assert.That(m_dialogs.IsOpen(), Is.True);
            Assert.That(m_dialogs.IsOpen("Inspector"), Is.True);

            second.RequestClose(DialogResult<string>.Confirmed("b"));
            first.RequestClose(DialogResult<string>.Confirmed("a"));

            Assert.That((await pendingFirst).Value, Is.EqualTo("a"));
            Assert.That((await pendingSecond).Value, Is.EqualTo("b"));
        }

        #endregion

        #region Failure Tests

        [Test]
        public async Task CancellationTokenClosesAsCancelledTest()
        {
            var viewModel = new DialogViewModel { CanClose = false };
            using var cancellation = new CancellationTokenSource();

            var pending = m_dialogs.ShowAsync(viewModel, cancellation: cancellation.Token);
            cancellation.Cancel();
            var result = await pending;

            Assert.That(result.IsConfirmed, Is.False);
            Assert.That(m_dialogs.IsOpen(), Is.False);
        }

        [Test]
        public async Task ExceptionInOnOpenedClosesDialogTest()
        {
            var viewModel = new DialogViewModel { ThrowOnOpened = new InvalidOperationException("load failed") };

            var result = await m_dialogs.ShowAsync(viewModel);

            Assert.That(result.IsConfirmed, Is.False);
            Assert.That(m_dialogs.IsOpen(), Is.False);
        }

        [Test]
        public async Task ViewFactoryFailureReturnsCancelledWithoutShowingTest()
        {
            m_views.ThrowOnBuild = new InvalidOperationException("no view");

            var result = await m_dialogs.ShowAsync(new DialogViewModel());

            Assert.That(result.IsConfirmed, Is.False);
            Assert.That(m_host.ShownViews, Is.Empty);
            Assert.That(m_dialogs.IsOpen(), Is.False);
        }

        #endregion

        #region Scope Tests

        [Test]
        public async Task TypedShowCreatesViewModelInScopeAndDisposesItTest()
        {
            var pending = m_dialogs.ShowAsync<DialogViewModel, string>();
            var viewModel = (DialogViewModel)((FakeView)m_host.TopView()!).ViewModel;

            Assert.That(viewModel.Dependency, Is.Not.Null);
            Assert.That(viewModel.Dependency!.IsDisposed, Is.False);

            viewModel.RequestClose(DialogResult<string>.Confirmed("done"));
            var result = await pending;

            Assert.That(result.Value, Is.EqualTo("done"));
            Assert.That(viewModel.IsDisposed, Is.True);
            Assert.That(viewModel.Dependency.IsDisposed, Is.True);
        }

        #endregion
    }
}
