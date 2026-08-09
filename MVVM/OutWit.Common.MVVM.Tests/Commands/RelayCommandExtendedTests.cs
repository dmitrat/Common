using System.Threading.Tasks;
using NUnit.Framework;

namespace OutWit.Common.MVVM.Commands.Tests;

[TestFixture]
public class RelayCommandExtendedTests
{
    #region Action without parameters

    [Test]
    public void ActionWithoutParametersExecutesTest()
    {
        var executed = false;
        var command = new RelayCommand(() => executed = true);

        command.Execute(null);

        Assert.That(executed, Is.True);
    }

    [Test]
    public void ActionWithoutParametersCanExecuteTest()
    {
        var canExecute = true;
        var command = new RelayCommand(() => { }, () => canExecute);

        Assert.That(command.CanExecute(null), Is.True);

        canExecute = false;
        command.RaiseCanExecuteChanged();

        Assert.That(command.CanExecute(null), Is.False);
    }

    #endregion

    // ICommand.Execute is fire-and-forget: there is no task to await, so these
    // tests poll instead of sleeping a fixed amount. Fixed sleeps failed roughly
    // one run in three once the whole solution ran its suites in parallel.
    private const int POLL_TIMEOUT_MS = 5000;
    private const int POLL_INTERVAL_MS = 10;

    #region Async support

    [Test]
    public void AsyncFuncTaskExecutesTest()
    {
        var executed = false;
        var command = new RelayCommandAsync(async () =>
        {
            await Task.Delay(10);
            executed = true;
        });

        command.Execute(null);

        Assert.That(() => executed, Is.True.After(POLL_TIMEOUT_MS, POLL_INTERVAL_MS));
    }

    [Test]
    public void AsyncFuncTaskWithParameterExecutesTest()
    {
        var receivedParam = "";
        var command = new RelayCommandAsync(async (object? param) =>
        {
            await Task.Delay(10);
            receivedParam = param?.ToString() ?? "";
        });

        command.Execute("test");

        Assert.That(() => receivedParam, Is.EqualTo("test").After(POLL_TIMEOUT_MS, POLL_INTERVAL_MS));
    }

    [Test]
    public void AsyncCommandDisablesWhileExecutingTest()
    {
        var tcs = new TaskCompletionSource<bool>();
        var command = new RelayCommandAsync(async () => await tcs.Task);

        Assert.That(command.CanExecute(null), Is.True);
        Assert.That(command.IsExecuting, Is.False);

        command.Execute(null);

        Assert.That(() => command.IsExecuting, Is.True.After(POLL_TIMEOUT_MS, POLL_INTERVAL_MS), "IsExecuting should be true");
        Assert.That(command.CanExecute(null), Is.False, "Command should be disabled during execution");

        tcs.SetResult(true);

        Assert.That(() => command.IsExecuting, Is.False.After(POLL_TIMEOUT_MS, POLL_INTERVAL_MS), "IsExecuting should be false");
        Assert.That(command.CanExecute(null), Is.True, "Command should be enabled after execution");
    }

    [Test]
    public void AsyncCommandWithCanExecuteTest()
    {
        var canExecute = true;
        var executed = false;
        var command = new RelayCommandAsync(
            async () =>
            {
                await Task.Delay(10);
                executed = true;
            },
            () => canExecute);

        Assert.That(command.CanExecute(null), Is.True);

        canExecute = false;
        command.RaiseCanExecuteChanged();

        Assert.That(command.CanExecute(null), Is.False);

        canExecute = true;
        command.RaiseCanExecuteChanged();
        command.Execute(null);

        Assert.That(() => executed, Is.True.After(POLL_TIMEOUT_MS, POLL_INTERVAL_MS));
    }

    #endregion

    #region Mixed usage

    [Test]
    public void SyncAndAsyncCommandsWorkIndependentlyTest()
    {
        var syncExecuted = false;
        var asyncExecuted = false;

        var syncCommand = new RelayCommand(() => syncExecuted = true);
        var asyncCommand = new RelayCommandAsync(async () =>
        {
            await Task.Delay(10);
            asyncExecuted = true;
        });

        syncCommand.Execute(null);
        asyncCommand.Execute(null);

        Assert.That(syncExecuted, Is.True, "Sync command should execute immediately");

        // Polled rather than slept on: ICommand.Execute is fire-and-forget, so
        // there is no task to await, and a fixed 50 ms was not enough for a 10 ms
        // continuation once the whole solution runs its suites in parallel. The
        // test then failed roughly one run in three, on timing rather than on
        // behaviour.
        Assert.That(() => asyncExecuted, Is.True.After(POLL_TIMEOUT_MS, POLL_INTERVAL_MS), "Async command should complete");
    }

    #endregion
}
