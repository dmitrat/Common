using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Settings.Providers;

namespace OutWit.Common.Settings.Csv.Tests
{
    [TestFixture]
    public class CsvSettingsProviderConcurrencyTests
    {
        #region Fields

        private string m_testDir = null!;

        #endregion

        #region Setup

        [SetUp]
        public void Setup()
        {
            m_testDir = Path.Combine(Path.GetTempPath(), "OutWit.Settings.CsvConcurrencyTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(m_testDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(m_testDir))
                Directory.Delete(m_testDir, recursive: true);
        }

        #endregion

        #region Concurrent Read Tests

        [Test]
        public void ConcurrentReadsDoNotThrowTest()
        {
            var path = WriteCsvFile("Group,Key,Value,ValueKind,Tag,Hidden\nGeneral,Name,admin,String,,False\nGeneral,Mode,Dark,String,,False\n");

            var provider = new CsvSettingsProvider(path);

            var tasks = new List<Task>();
            for (var i = 0; i < 20; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    var entries = provider.Read("General");
                    Assert.That(entries, Has.Count.EqualTo(2));
                }));
            }

            Assert.DoesNotThrowAsync(async () => await Task.WhenAll(tasks));
        }

        #endregion

        #region Concurrent Write Tests

        [Test]
        public void ConcurrentWritesDoNotCorruptFileTest()
        {
            var path = Path.Combine(m_testDir, "concurrent-write.csv");
            var provider = new CsvSettingsProvider(path);

            var tasks = new List<Task>();
            for (var i = 0; i < 10; i++)
            {
                var idx = i;
                tasks.Add(Task.Run(() =>
                {
                    provider.Write($"Group{idx}", new List<SettingsEntry>
                    {
                        new()
                        {
                            Group = $"Group{idx}",
                            Key = "Setting",
                            Value = $"value{idx}",
                            ValueKind = "String"
                        }
                    });
                }));
            }

            Assert.DoesNotThrowAsync(async () => await Task.WhenAll(tasks));

            var groups = provider.GetGroups();
            Assert.That(groups.Count, Is.GreaterThan(0));
        }

        #endregion

        #region Concurrent Read-Write Tests

        [Test]
        public void ConcurrentReadWriteDoesNotThrowTest()
        {
            var path = WriteCsvFile("Group,Key,Value,ValueKind,Tag,Hidden\nGeneral,Name,admin,String,,False\n");

            var provider = new CsvSettingsProvider(path);

            using var cts = new CancellationTokenSource(millisecondsDelay: 2000);
            var token = cts.Token;

            var readTask = Task.Run(() =>
            {
                while (!token.IsCancellationRequested)
                {
                    _ = provider.Read("General");
                }
            }, token);

            var writeTask = Task.Run(() =>
            {
                var counter = 0;
                while (!token.IsCancellationRequested)
                {
                    provider.Write("General", new List<SettingsEntry>
                    {
                        new()
                        {
                            Group = "General",
                            Key = "Name",
                            Value = $"user{counter++}",
                            ValueKind = "String"
                        }
                    });
                }
            }, token);

            Assert.DoesNotThrowAsync(async () =>
            {
                try
                {
                    await Task.WhenAll(readTask, writeTask);
                }
                catch (OperationCanceledException)
                {
                    // Expected
                }
            });
        }

        #endregion

        #region Concurrent Delete Tests

        [Test]
        public void ConcurrentDeleteDoesNotThrowTest()
        {
            var path = WriteCsvFile("Group,Key,Value,ValueKind,Tag,Hidden\nGeneral,Name,admin,String,,False\n");

            var provider = new CsvSettingsProvider(path);

            var tasks = new List<Task>();
            for (var i = 0; i < 10; i++)
            {
                tasks.Add(Task.Run(() => provider.Delete()));
            }

            Assert.DoesNotThrowAsync(async () => await Task.WhenAll(tasks));
        }

        #endregion

        #region Tools

        private string WriteCsvFile(string content)
        {
            var path = Path.Combine(m_testDir, $"{Guid.NewGuid()}.csv");
            File.WriteAllText(path, content);
            return path;
        }

        #endregion
    }
}
