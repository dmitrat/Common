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

    #region Async support

    [Test]
    public async Task AsyncFuncTaskExecutesTest()
    {
        var executed = false;
        var command = new RelayCommandAsync(async () =>
        {
            await Task.Delay(10);
            executed = true;
        });

        command.Execute(null);
        await Task.Delay(50); // Wait for async execution

        Assert.That(executed, Is.True);
    }

    [Test]
    public async Task AsyncFuncTaskWithParameterExecutesTest()
    {
        var receivedParam = "";
        var command = new RelayCommandAsync(async (object? param) =>
        {
            await Task.Delay(10);
            receivedParam = param?.ToString() ?? "";
        });

        command.Execute("test");
        await Task.Delay(50);

        Assert.That(receivedParam, Is.EqualTo("test"));
    }

    [Test]
    public async Task AsyncCommandDisablesWhileExecutingTest()
    {
        var tcs = new TaskCompletionSource<bool>();
        var command = new RelayCommandAsync(async () => await tcs.Task);

        Assert.That(command.CanExecute(null), Is.True);
        Assert.That(command.IsExecuting, Is.False);

        command.Execute(null);
        await Task.Delay(10); // Let execution start

        Assert.That(command.CanExecute(null), Is.False, "Command should be disabled during execution");
        Assert.That(command.IsExecuting, Is.True, "IsExecuting should be true");

        tcs.SetResult(true);
        await Task.Delay(10); // Let execution complete

        Assert.That(command.CanExecute(null), Is.True, "Command should be enabled after execution");
        Assert.That(command.IsExecuting, Is.False, "IsExecuting should be false");
    }

    [Test]
    public async Task AsyncCommandWithCanExecuteTest()
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
        await Task.Delay(50);

        Assert.That(executed, Is.True);
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

        Task.Delay(50).Wait();
        Assert.That(asyncExecuted, Is.True, "Async command should complete");
    }

    #endregion
}
