using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Settings.Configuration;
using OutWit.Common.Settings.Providers;
using OutWit.Common.Settings.Tests.Utils;

namespace OutWit.Common.Settings.Tests
{
    [TestFixture]
    public class SettingsManagerConcurrencyTests
    {
        #region Concurrent Load Tests

        [Test]
        public void ConcurrentLoadDoesNotCorruptCollectionsTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            for (var i = 0; i < 50; i++)
            {
                defaultProvider.AddEntry("General", new SettingsEntry
                {
                    Key = $"Setting{i}",
                    Value = $"value{i}",
                    ValueKind = "String"
                });
            }

            var manager = new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .Build();

            var tasks = new List<Task>();
            for (var t = 0; t < 10; t++)
            {
                tasks.Add(Task.Run(() => manager.Load()));
            }

            Assert.DoesNotThrowAsync(async () => await Task.WhenAll(tasks));

            manager.Load();
            Assert.That(manager["General"].Count, Is.EqualTo(50));
        }

        #endregion

        #region Concurrent Read Tests

        [Test]
        public void ConcurrentCollectionsAccessDuringLoadDoesNotThrowTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            for (var i = 0; i < 20; i++)
            {
                defaultProvider.AddEntry("General", new SettingsEntry
                {
                    Key = $"Setting{i}",
                    Value = $"value{i}",
                    ValueKind = "String"
                });
            }

            var manager = new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .Build();

            manager.Load();

            using var cts = new CancellationTokenSource(millisecondsDelay: 2000);
            var token = cts.Token;

            var readTask = Task.Run(() =>
            {
                while (!token.IsCancellationRequested)
                {
                    var collections = manager.Collections;
                    if (collections.Count > 0)
                    {
                        _ = collections[0].Group;
                    }
                }
            }, token);

            var loadTask = Task.Run(() =>
            {
                while (!token.IsCancellationRequested)
                {
                    manager.Load();
                }
            }, token);

            Assert.DoesNotThrowAsync(async () =>
            {
                try
                {
                    await Task.WhenAll(readTask, loadTask);
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation token fires
                }
            });
        }

        #endregion

        #region Concurrent Save Tests

        [Test]
        public void ConcurrentSaveDoesNotThrowTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            for (var i = 0; i < 10; i++)
            {
                defaultProvider.AddEntry("General", new SettingsEntry
                {
                    Key = $"Setting{i}",
                    Value = $"value{i}",
                    ValueKind = "String"
                });
            }

            var userProvider = new MemorySettingsProvider();

            var manager = new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .AddProvider(SettingsScope.User, userProvider)
                .Build();

            manager.Load();

            var tasks = new List<Task>();
            for (var t = 0; t < 10; t++)
            {
                var idx = t;
                tasks.Add(Task.Run(() =>
                {
                    manager["General"][$"Setting{idx}"].Value = $"modified{idx}";
                    manager.Save();
                }));
            }

            Assert.DoesNotThrowAsync(async () => await Task.WhenAll(tasks));
        }

        #endregion

        #region Concurrent Merge Tests

        [Test]
        public void ConcurrentMergeDoesNotThrowTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            for (var i = 0; i < 10; i++)
            {
                defaultProvider.AddEntry("General", new SettingsEntry
                {
                    Key = $"Setting{i}",
                    Value = $"value{i}",
                    ValueKind = "String"
                });
            }

            var userProvider = new MemorySettingsProvider();
            userProvider.AddEntry("General", new SettingsEntry
            {
                Key = "Setting0",
                Value = "old",
                ValueKind = "String"
            });

            var manager = new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .AddProvider(SettingsScope.User, userProvider)
                .Build();

            var tasks = new List<Task>();
            for (var t = 0; t < 10; t++)
            {
                tasks.Add(Task.Run(() => manager.Merge()));
            }

            Assert.DoesNotThrowAsync(async () => await Task.WhenAll(tasks));
        }

        #endregion

        #region Mixed Concurrent Operations Tests

        [Test]
        public void ConcurrentLoadSaveMergeDoesNotThrowTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            for (var i = 0; i < 10; i++)
            {
                defaultProvider.AddEntry("General", new SettingsEntry
                {
                    Key = $"Setting{i}",
                    Value = $"value{i}",
                    ValueKind = "String"
                });
            }

            var userProvider = new MemorySettingsProvider();

            var manager = new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .AddProvider(SettingsScope.User, userProvider)
                .Build();

            manager.Merge();
            manager.Load();

            using var cts = new CancellationTokenSource(millisecondsDelay: 2000);
            var token = cts.Token;

            var loadTask = Task.Run(() =>
            {
                while (!token.IsCancellationRequested)
                    manager.Load();
            }, token);

            var saveTask = Task.Run(() =>
            {
                while (!token.IsCancellationRequested)
                    manager.Save();
            }, token);

            var mergeTask = Task.Run(() =>
            {
                while (!token.IsCancellationRequested)
                    manager.Merge();
            }, token);

            var readTask = Task.Run(() =>
            {
                while (!token.IsCancellationRequested)
                    _ = manager.Collections;
            }, token);

            Assert.DoesNotThrowAsync(async () =>
            {
                try
                {
                    await Task.WhenAll(loadTask, saveTask, mergeTask, readTask);
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation token fires
                }
            });
        }

        #endregion
    }
}
