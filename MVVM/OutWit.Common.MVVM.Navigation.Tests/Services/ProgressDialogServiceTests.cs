using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Model;
using OutWit.Common.MVVM.Navigation.Tests.Mock;
using OutWit.Common.MVVM.Navigation.ViewModels;

namespace OutWit.Common.MVVM.Navigation.Tests.Services
{
    /// <summary>
    /// The timing is the contract: a fast operation shows nothing, a borderline one does not
    /// flash, and a cancel keeps the dialog up until the work has actually stopped.
    /// </summary>
    [TestFixture]
    public class ProgressDialogServiceTests
    {
        #region Constants

        private static readonly TimeSpan SHORT = TimeSpan.FromMilliseconds(50);
        private static readonly TimeSpan LONG = TimeSpan.FromMilliseconds(400);

        #endregion

        #region Fields

        private ServiceProvider m_provider = null!;
        private IProgressDialogService m_progress = null!;
        private FakeDialogHost m_host = null!;

        #endregion

        [SetUp]
        public void Setup()
        {
            m_host = new FakeDialogHost();

            m_provider = NavigationTestHost.Build(services: services =>
            {
                services.AddSingleton<IDialogHost>(m_host);
                services.AddSingleton<IViewFactory>(new FakeViewFactory(typeof(ProgressDialogViewModel)));
            });

            m_progress = m_provider.GetRequiredService<IProgressDialogService>();
        }

        [TearDown]
        public void TearDown()
        {
            m_provider.Dispose();
        }

        #region Timing Tests

        [Test, CancelAfter(10000)]
        public async Task OperationFasterThanTheDelayShowsNothingTest()
        {
            var options = new ProgressOptions { Delay = LONG, MinimumDuration = LONG };

            var result = await m_progress.RunAsync((_, _) => Task.FromResult(42), options);

            Assert.That(result.IsCompleted, Is.True);
            Assert.That(result.Value, Is.EqualTo(42));
            Assert.That(m_host.ShownViews, Is.Empty, "a fast operation must not flash a dialog");
        }

        [Test, CancelAfter(10000)]
        public async Task OperationOutlastingTheDelayShowsTheDialogTest()
        {
            var options = new ProgressOptions { Delay = SHORT, MinimumDuration = TimeSpan.Zero };
            var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            var running = m_progress.RunAsync(async (_, _) => { await gate.Task; return 7; }, options);

            await WaitUntil(() => m_host.IsOpen(DialogHosts.ROOT));
            Assert.That(m_host.ShownViews, Has.Count.EqualTo(1));

            gate.SetResult(true);
            var result = await running;

            Assert.That(result.Value, Is.EqualTo(7));
            Assert.That(m_host.IsOpen(DialogHosts.ROOT), Is.False, "the dialog must close when the work is over");
        }

        [Test, CancelAfter(10000)]
        public async Task ShownDialogStaysUpForTheMinimumDurationTest()
        {
            var options = new ProgressOptions { Delay = TimeSpan.Zero, MinimumDuration = LONG };
            var watch = Stopwatch.StartNew();

            await m_progress.RunAsync(async (_, _) => { await Task.Delay(SHORT); return 1; }, options);

            // the work took SHORT; the dialog was up from the start, so the run cannot be
            // shorter than the minimum
            Assert.That(watch.Elapsed, Is.GreaterThanOrEqualTo(LONG - TimeSpan.FromMilliseconds(80)));
            Assert.That(m_host.ShownViews, Has.Count.EqualTo(1));
        }

        #endregion

        #region Reporting Tests

        [Test, CancelAfter(10000)]
        public async Task ReportedStatusAndProgressReachTheViewModelTest()
        {
            var options = new ProgressOptions { Delay = TimeSpan.Zero, MinimumDuration = TimeSpan.Zero, Title = "Importing" };
            var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            IProgressReporter? captured = null;

            // the work starts before the dialog is shown — it has to, or the delay could never
            // save a fast operation from one — so this also checks that early reports survive
            var running = m_progress.RunAsync(async (reporter, _) =>
            {
                captured = reporter;
                reporter.Report("halfway", 0.5);

                await gate.Task;

                reporter.Report(4);
                return true;
            }, options);

            await WaitUntil(() => m_host.IsOpen(DialogHosts.ROOT));
            var viewModel = (ProgressDialogViewModel)((FakeView)m_host.TopView()!).ViewModel;

            Assert.That(captured, Is.SameAs(viewModel));
            Assert.That(viewModel.Title, Is.EqualTo("Importing"));
            Assert.That(viewModel.Status, Is.EqualTo("halfway"));
            Assert.That(viewModel.Progress, Is.EqualTo(0.5));
            Assert.That(viewModel.IsIndeterminate, Is.False);

            gate.SetResult(true);
            await running;

            Assert.That(viewModel.Progress, Is.EqualTo(1), "a fraction is clamped, not trusted");
        }

        [Test, CancelAfter(10000)]
        public void ProgressStartsIndeterminateTest()
        {
            using var cancellation = new CancellationTokenSource();
            var viewModel = new ProgressDialogViewModel(new ProgressOptions(), cancellation);

            Assert.That(viewModel.Progress, Is.Null);
            Assert.That(viewModel.IsIndeterminate, Is.True);

            viewModel.Report(0.25);

            Assert.That(viewModel.IsIndeterminate, Is.False);
        }

        #endregion

        #region Cancellation Tests

        [Test, CancelAfter(10000)]
        public async Task CancelKeepsTheDialogUpUntilTheWorkStopsTest()
        {
            var options = new ProgressOptions { Delay = TimeSpan.Zero, MinimumDuration = TimeSpan.Zero };
            var stopped = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var sawCancellation = false;

            var running = m_progress.RunAsync(async (reporter, token) =>
            {
                await stopped.Task;
                sawCancellation = token.IsCancellationRequested && reporter.IsCancellationRequested;
                token.ThrowIfCancellationRequested();

                return 1;
            }, options);

            await WaitUntil(() => m_host.IsOpen(DialogHosts.ROOT));
            var viewModel = (ProgressDialogViewModel)((FakeView)m_host.TopView()!).ViewModel;

            viewModel.CancelCommand.Execute(null);

            Assert.That(viewModel.IsCancellationRequested, Is.True);
            Assert.That(m_host.IsOpen(DialogHosts.ROOT), Is.True, "the dialog stays until the work actually stops");
            Assert.That(running.IsCompleted, Is.False);

            stopped.SetResult(true);
            var result = await running;

            Assert.That(sawCancellation, Is.True);
            Assert.That(result.IsCancelled, Is.True);
            Assert.That(result.IsCompleted, Is.False);
            Assert.That(m_host.IsOpen(DialogHosts.ROOT), Is.False);
        }

        [Test, CancelAfter(10000)]
        public async Task DismissingTheDialogCancelsRatherThanClosingTest()
        {
            var options = new ProgressOptions { Delay = TimeSpan.Zero, MinimumDuration = TimeSpan.Zero };
            var stopped = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            var running = m_progress.RunAsync(async (_, token) =>
            {
                await stopped.Task;
                token.ThrowIfCancellationRequested();
            }, options);

            await WaitUntil(() => m_host.IsOpen(DialogHosts.ROOT));
            var viewModel = (ProgressDialogViewModel)((FakeView)m_host.TopView()!).ViewModel;

            var dismissed = await m_host.DismissAsync();

            Assert.That(dismissed, Is.False, "Escape must not close a progress dialog");
            Assert.That(viewModel.IsCancellationRequested, Is.True);

            stopped.SetResult(true);

            Assert.That((await running).IsCancelled, Is.True);
        }

        [Test, CancelAfter(10000)]
        public async Task CallerTokenCancelsTheOperationTest()
        {
            var options = new ProgressOptions { Delay = TimeSpan.Zero, MinimumDuration = TimeSpan.Zero };
            using var cancellation = new CancellationTokenSource();

            var running = m_progress.RunAsync(async (_, token) =>
            {
                await Task.Delay(Timeout.Infinite, token);
                return 1;
            }, options, cancellation: cancellation.Token);

            await WaitUntil(() => m_host.IsOpen(DialogHosts.ROOT));
            cancellation.Cancel();

            Assert.That((await running).IsCancelled, Is.True);
            Assert.That(m_host.IsOpen(DialogHosts.ROOT), Is.False);
        }

        [Test, CancelAfter(10000)]
        public async Task NonCancellableDialogIgnoresDismissTest()
        {
            var options = new ProgressOptions { Delay = TimeSpan.Zero, MinimumDuration = TimeSpan.Zero, IsCancellable = false };
            var stopped = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            var running = m_progress.RunAsync(async (_, _) => { await stopped.Task; return 1; }, options);

            await WaitUntil(() => m_host.IsOpen(DialogHosts.ROOT));
            var viewModel = (ProgressDialogViewModel)((FakeView)m_host.TopView()!).ViewModel;

            await m_host.DismissAsync();

            Assert.That(viewModel.IsCancellationRequested, Is.False);
            Assert.That(viewModel.CancelCommand.CanExecute(null), Is.False);

            stopped.SetResult(true);

            Assert.That((await running).IsCompleted, Is.True);
        }

        #endregion

        #region Failure Tests

        [Test, CancelAfter(10000)]
        public async Task FailingOperationReturnsFailedInsteadOfThrowingTest()
        {
            var options = new ProgressOptions { Delay = TimeSpan.Zero, MinimumDuration = TimeSpan.Zero };

            var result = await m_progress.RunAsync<int>((_, _) => throw new InvalidOperationException("no"), options);

            Assert.That(result.IsCompleted, Is.False);
            Assert.That(result.IsCancelled, Is.False);
            Assert.That(result.Error, Is.InstanceOf<InvalidOperationException>());
            Assert.That(m_host.IsOpen(DialogHosts.ROOT), Is.False, "a failure must not leave the dialog up");
        }

        [Test, CancelAfter(10000)]
        public async Task VoidOverloadReportsCompletionTest()
        {
            var ran = false;
            var options = new ProgressOptions { Delay = TimeSpan.Zero, MinimumDuration = TimeSpan.Zero };

            var result = await m_progress.RunAsync((_, _) => { ran = true; return Task.CompletedTask; }, options);

            Assert.That(ran, Is.True);
            Assert.That(result.IsCompleted, Is.True);
        }

        #endregion

        #region Tools

        private static async Task WaitUntil(Func<bool> condition, int timeoutMilliseconds = 5000)
        {
            var watch = Stopwatch.StartNew();

            while (!condition() && watch.ElapsedMilliseconds < timeoutMilliseconds)
                await Task.Delay(10);

            Assert.That(condition(), Is.True, "condition never became true");
        }

        #endregion
    }
}
