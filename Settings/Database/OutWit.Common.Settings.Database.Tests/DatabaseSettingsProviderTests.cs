using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using OutWit.Common.Settings.Database.Tests.Utils;
using OutWit.Common.Settings.Providers;
using OutWit.Database.EntityFramework.Extensions;

namespace OutWit.Common.Settings.Database.Tests
{
    [TestFixture]
    public class DatabaseSettingsProviderTests
    {
        #region Fields

        private string m_testDir = null!;

        #endregion

        #region Setup

        [SetUp]
        public void Setup()
        {
            m_testDir = Path.Combine(Path.GetTempPath(), "OutWit.Settings.Db.Tests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(m_testDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(m_testDir))
                Directory.Delete(m_testDir, recursive: true);
        }

        #endregion

        #region Read Tests

        [Test]
        public void ReadReturnsEntriesFromDatabaseTest()
        {
            var dbPath = GetDbPath();
            SeedDatabase(dbPath,
                ("General", "UserName", "admin", "String", "", false),
                ("General", "DarkMode", "True", "Boolean", "", false));

            var provider = CreateProvider(dbPath);
            var entries = provider.Read("General");

            Assert.That(entries, Has.Count.EqualTo(2));
            Assert.That(entries[0].Group, Is.EqualTo("General"));
            Assert.That(entries[0].Key, Is.EqualTo("UserName"));
            Assert.That(entries[0].Value, Is.EqualTo("admin"));
            Assert.That(entries[0].ValueKind, Is.EqualTo("String"));
            Assert.That(entries[1].Key, Is.EqualTo("DarkMode"));
            Assert.That(entries[1].Value, Is.EqualTo("True"));
        }

        [Test]
        public void ReadReturnsEmptyForMissingGroupTest()
        {
            var dbPath = GetDbPath();
            SeedDatabase(dbPath,
                ("General", "Name", "admin", "String", "", false));

            var provider = CreateProvider(dbPath);

            Assert.That(provider.Read("Advanced"), Is.Empty);
        }

        [Test]
        public void ReadReturnsEmptyForEmptyDatabaseTest()
        {
            var dbPath = GetDbPath();
            var provider = CreateProvider(dbPath);

            Assert.That(provider.Read("General"), Is.Empty);
        }

        [Test]
        public void ReadParsesTagAndHiddenTest()
        {
            var dbPath = GetDbPath();
            SeedDatabase(dbPath,
                ("General", "Mode", "Alpha", "Enum", "MyApp.TestEnum, MyApp", true));

            var provider = CreateProvider(dbPath);
            var entries = provider.Read("General");

            Assert.That(entries[0].Tag, Is.EqualTo("MyApp.TestEnum, MyApp"));
            Assert.That(entries[0].Hidden, Is.True);
        }

        [Test]
        public void ReadHandlesEmptyTagTest()
        {
            var dbPath = GetDbPath();
            SeedDatabase(dbPath,
                ("General", "Name", "admin", "String", "", false));

            var provider = CreateProvider(dbPath);
            var entries = provider.Read("General");

            Assert.That(entries[0].Tag, Is.EqualTo(""));
            Assert.That(entries[0].Hidden, Is.False);
        }

        [Test]
        public void ReadFiltersOnlyRequestedGroupTest()
        {
            var dbPath = GetDbPath();
            SeedDatabase(dbPath,
                ("General", "Name", "admin", "String", "", false),
                ("Advanced", "Timeout", "30", "Integer", "", false),
                ("General", "Port", "8080", "Integer", "", false));

            var provider = CreateProvider(dbPath);

            var general = provider.Read("General");
            Assert.That(general, Has.Count.EqualTo(2));

            var advanced = provider.Read("Advanced");
            Assert.That(advanced, Has.Count.EqualTo(1));
            Assert.That(advanced[0].Key, Is.EqualTo("Timeout"));
        }

        #endregion

        #region Write Tests

        [Test]
        public void WriteCreatesEntriesInDatabaseTest()
        {
            var dbPath = GetDbPath();
            var provider = CreateProvider(dbPath);

            provider.Write("General", new[]
            {
                new SettingsEntry { Group = "General", Key = "Name", Value = "admin", ValueKind = "String" },
                new SettingsEntry { Group = "General", Key = "Port", Value = "8080", ValueKind = "Integer" }
            });

            var readBack = provider.Read("General");
            Assert.That(readBack, Has.Count.EqualTo(2));
            Assert.That(readBack[0].Key, Is.EqualTo("Name"));
            Assert.That(readBack[0].Value, Is.EqualTo("admin"));
            Assert.That(readBack[1].Key, Is.EqualTo("Port"));
            Assert.That(readBack[1].Value, Is.EqualTo("8080"));
        }

        [Test]
        public void WritePreservesOtherGroupsTest()
        {
            var dbPath = GetDbPath();
            SeedDatabase(dbPath,
                ("General", "Name", "admin", "String", "", false),
                ("Advanced", "Timeout", "30", "Integer", "", false));

            var provider = CreateProvider(dbPath);

            provider.Write("General", new[]
            {
                new SettingsEntry { Group = "General", Key = "Name", Value = "john", ValueKind = "String" }
            });

            var general = provider.Read("General");
            Assert.That(general, Has.Count.EqualTo(1));
            Assert.That(general[0].Value, Is.EqualTo("john"));

            var advanced = provider.Read("Advanced");
            Assert.That(advanced, Has.Count.EqualTo(1));
            Assert.That(advanced[0].Key, Is.EqualTo("Timeout"));
            Assert.That(advanced[0].Value, Is.EqualTo("30"));
        }

        [Test]
        public void WriteDoesNothingWhenReadOnlyTest()
        {
            var dbPath = GetDbPath();
            var provider = CreateProvider(dbPath, isReadOnly: true);

            provider.Write("General", new[]
            {
                new SettingsEntry { Key = "Name", Value = "admin", ValueKind = "String" }
            });

            Assert.That(provider.Read("General"), Is.Empty);
        }

        [Test]
        public void WriteReplacesExistingGroupTest()
        {
            var dbPath = GetDbPath();
            SeedDatabase(dbPath,
                ("General", "Name", "admin", "String", "", false),
                ("General", "Port", "8080", "Integer", "", false));

            var provider = CreateProvider(dbPath);

            provider.Write("General", new[]
            {
                new SettingsEntry { Group = "General", Key = "Name", Value = "john", ValueKind = "String" }
            });

            var entries = provider.Read("General");
            Assert.That(entries, Has.Count.EqualTo(1));
            Assert.That(entries[0].Key, Is.EqualTo("Name"));
            Assert.That(entries[0].Value, Is.EqualTo("john"));
        }

        [Test]
        public void WriteStoresTagAndHiddenTest()
        {
            var dbPath = GetDbPath();
            var provider = CreateProvider(dbPath);

            provider.Write("General", new[]
            {
                new SettingsEntry { Group = "General", Key = "Mode", Value = "Alpha", ValueKind = "Enum", Tag = "MyApp.TestEnum, MyApp", Hidden = true }
            });

            var readBack = provider.Read("General");
            Assert.That(readBack[0].Tag, Is.EqualTo("MyApp.TestEnum, MyApp"));
            Assert.That(readBack[0].Hidden, Is.True);
        }

        #endregion

        #region GetGroups Tests

        [Test]
        public void GetGroupsReturnsAllGroupNamesTest()
        {
            var dbPath = GetDbPath();
            SeedDatabase(dbPath,
                ("General", "Name", "admin", "String", "", false),
                ("Advanced", "Timeout", "30", "Integer", "", false));

            var provider = CreateProvider(dbPath);
            var groups = provider.GetGroups();

            Assert.That(groups, Has.Count.EqualTo(2));
            Assert.That(groups, Does.Contain("General"));
            Assert.That(groups, Does.Contain("Advanced"));
        }

        [Test]
        public void GetGroupsReturnsEmptyForEmptyDatabaseTest()
        {
            var dbPath = GetDbPath();
            var provider = CreateProvider(dbPath);

            Assert.That(provider.GetGroups(), Is.Empty);
        }

        #endregion

        #region Properties Tests

        [Test]
        public void DefaultIsReadOnlyIsFalseTest()
        {
            var dbPath = GetDbPath();
            var provider = CreateProvider(dbPath);

            Assert.That(provider.IsReadOnly, Is.False);
        }

        [Test]
        public void ConstructorSetsIsReadOnlyTest()
        {
            var dbPath = GetDbPath();
            var provider = CreateProvider(dbPath, isReadOnly: true);

            Assert.That(provider.IsReadOnly, Is.True);
        }

        #endregion

        #region Shared Context Tests

        [Test]
        public void SharedContextReadWriteRoundTripTest()
        {
            var dbPath = GetDbPath();
            InitSharedDatabase(dbPath);

            var provider = CreateSharedProvider(dbPath);

            provider.Write("General", new[]
            {
                new SettingsEntry { Group = "General", Key = "Name", Value = "admin", ValueKind = "String" },
                new SettingsEntry { Group = "General", Key = "Port", Value = "8080", ValueKind = "Integer" }
            });

            var entries = provider.Read("General");
            Assert.That(entries, Has.Count.EqualTo(2));
            Assert.That(entries[0].Key, Is.EqualTo("Name"));
            Assert.That(entries[0].Value, Is.EqualTo("admin"));
            Assert.That(entries[1].Key, Is.EqualTo("Port"));
            Assert.That(entries[1].Value, Is.EqualTo("8080"));
        }

        [Test]
        public void SharedContextGetGroupsTest()
        {
            var dbPath = GetDbPath();
            InitSharedDatabase(dbPath);

            var provider = CreateSharedProvider(dbPath);

            provider.Write("General", new[]
            {
                new SettingsEntry { Group = "General", Key = "Name", Value = "admin", ValueKind = "String" }
            });
            provider.Write("Advanced", new[]
            {
                new SettingsEntry { Group = "Advanced", Key = "Timeout", Value = "30", ValueKind = "Integer" }
            });

            var groups = provider.GetGroups();
            Assert.That(groups, Has.Count.EqualTo(2));
            Assert.That(groups, Does.Contain("General"));
            Assert.That(groups, Does.Contain("Advanced"));
        }

        [Test]
        public void SharedContextRespectsReadOnlyTest()
        {
            var dbPath = GetDbPath();
            InitSharedDatabase(dbPath);

            var provider = CreateSharedProvider(dbPath, isReadOnly: true);

            provider.Write("General", new[]
            {
                new SettingsEntry { Key = "Name", Value = "admin", ValueKind = "String" }
            });

            Assert.That(provider.Read("General"), Is.Empty);
        }

        [Test]
        public void SharedContextStoresTagAndHiddenTest()
        {
            var dbPath = GetDbPath();
            InitSharedDatabase(dbPath);

            var provider = CreateSharedProvider(dbPath);

            provider.Write("General", new[]
            {
                new SettingsEntry { Group = "General", Key = "Mode", Value = "Alpha", ValueKind = "Enum", Tag = "MyApp.TestEnum, MyApp", Hidden = true }
            });

            var entries = provider.Read("General");
            Assert.That(entries[0].Tag, Is.EqualTo("MyApp.TestEnum, MyApp"));
            Assert.That(entries[0].Hidden, Is.True);
        }

        [Test]
        public void SharedContextApplySettingsConfigurationCreatesTableTest()
        {
            var dbPath = GetDbPath();

            var options = new DbContextOptionsBuilder<TestAppDbContext>()
                .UseWitDb($"Data Source={dbPath}")
                .Options;

            using (var context = new TestAppDbContext(options))
            {
                context.Database.EnsureCreated();
            }

            var provider = new DatabaseSettingsProvider(
                () => new TestAppDbContext(options));

            provider.Write("General", new[]
            {
                new SettingsEntry { Group = "General", Key = "Name", Value = "test", ValueKind = "String" }
            });

            var entries = provider.Read("General");
            Assert.That(entries, Has.Count.EqualTo(1));
            Assert.That(entries[0].Value, Is.EqualTo("test"));
        }

        [Test]
        public void SharedContextPreservesOtherGroupsTest()
        {
            var dbPath = GetDbPath();
            InitSharedDatabase(dbPath);

            var provider = CreateSharedProvider(dbPath);

            provider.Write("General", new[]
            {
                new SettingsEntry { Group = "General", Key = "Name", Value = "admin", ValueKind = "String" }
            });
            provider.Write("Advanced", new[]
            {
                new SettingsEntry { Group = "Advanced", Key = "Timeout", Value = "30", ValueKind = "Integer" }
            });

            provider.Write("General", new[]
            {
                new SettingsEntry { Group = "General", Key = "Name", Value = "john", ValueKind = "String" }
            });

            var general = provider.Read("General");
            Assert.That(general, Has.Count.EqualTo(1));
            Assert.That(general[0].Value, Is.EqualTo("john"));

            var advanced = provider.Read("Advanced");
            Assert.That(advanced, Has.Count.EqualTo(1));
            Assert.That(advanced[0].Value, Is.EqualTo("30"));
        }

        #endregion

        #region Group Metadata Tests

        [Test]
        public void ReadGroupInfoReturnsEmptyForEmptyDatabaseTest()
        {
            var dbPath = GetDbPath();
            var provider = CreateProvider(dbPath);

            Assert.That(provider.ReadGroupInfo(), Is.Empty);
        }

        [Test]
        public void WriteGroupInfoStoresMetadataTest()
        {
            var dbPath = GetDbPath();
            var provider = CreateProvider(dbPath);

            provider.WriteGroupInfo(new[]
            {
                new SettingsGroupInfo { Group = "General", DisplayName = "Main", Priority = 1 },
                new SettingsGroupInfo { Group = "Advanced", DisplayName = "Extra", Priority = 2 }
            });

            var readBack = provider.ReadGroupInfo();
            Assert.That(readBack, Has.Count.EqualTo(2));
            Assert.That(readBack.First(g => g.Group == "General").DisplayName, Is.EqualTo("Main"));
            Assert.That(readBack.First(g => g.Group == "General").Priority, Is.EqualTo(1));
            Assert.That(readBack.First(g => g.Group == "Advanced").Priority, Is.EqualTo(2));
        }

        [Test]
        public void WriteGroupInfoDoesNothingWhenReadOnlyTest()
        {
            var dbPath = GetDbPath();
            var provider = CreateProvider(dbPath, isReadOnly: true);

            provider.WriteGroupInfo(new[]
            {
                new SettingsGroupInfo { Group = "General", Priority = 1 }
            });

            var readWrite = CreateProvider(dbPath);
            Assert.That(readWrite.ReadGroupInfo(), Is.Empty);
        }

        [Test]
        public void WriteGroupInfoReplacesExistingMetadataTest()
        {
            var dbPath = GetDbPath();
            var provider = CreateProvider(dbPath);

            provider.WriteGroupInfo(new[]
            {
                new SettingsGroupInfo { Group = "General", DisplayName = "Old", Priority = 1 }
            });

            provider.WriteGroupInfo(new[]
            {
                new SettingsGroupInfo { Group = "General", DisplayName = "New", Priority = 5 }
            });

            var readBack = provider.ReadGroupInfo();
            Assert.That(readBack, Has.Count.EqualTo(1));
            Assert.That(readBack[0].DisplayName, Is.EqualTo("New"));
            Assert.That(readBack[0].Priority, Is.EqualTo(5));
        }

        [Test]
        public void SharedContextGroupMetadataRoundTripTest()
        {
            var dbPath = GetDbPath();
            InitSharedDatabase(dbPath);

            var provider = CreateSharedProvider(dbPath);

            provider.WriteGroupInfo(new[]
            {
                new SettingsGroupInfo { Group = "General", DisplayName = "Shared Main", Priority = 1 }
            });

            var readBack = provider.ReadGroupInfo();
            Assert.That(readBack, Has.Count.EqualTo(1));
            Assert.That(readBack[0].Group, Is.EqualTo("General"));
            Assert.That(readBack[0].DisplayName, Is.EqualTo("Shared Main"));
            Assert.That(readBack[0].Priority, Is.EqualTo(1));
        }

        #endregion

        #region Helpers

        private string GetDbPath()
        {
            return Path.Combine(m_testDir, $"{Guid.NewGuid()}.witdb");
        }

        private static DatabaseSettingsProvider CreateProvider(string dbPath, bool isReadOnly = false)
        {
            return new DatabaseSettingsProvider(
                options => options.UseWitDb($"Data Source={dbPath}"),
                isReadOnly);
        }

        private static void SeedDatabase(string dbPath, params (string Group, string Key, string Value, string ValueKind, string Tag, bool Hidden)[] entries)
        {
            var provider = CreateProvider(dbPath);
            foreach (var group in entries.GroupBy(e => e.Group))
            {
                provider.Write(group.Key, group.Select(e => new SettingsEntry
                {
                    Group = e.Group,
                    Key = e.Key,
                    Value = e.Value,
                    ValueKind = e.ValueKind,
                    Tag = e.Tag,
                    Hidden = e.Hidden
                }).ToArray());
            }
        }

        private static void InitSharedDatabase(string dbPath)
        {
            var options = new DbContextOptionsBuilder<TestAppDbContext>()
                .UseWitDb($"Data Source={dbPath}")
                .Options;

            using var context = new TestAppDbContext(options);
            context.Database.EnsureCreated();
        }

        private static DatabaseSettingsProvider CreateSharedProvider(string dbPath, bool isReadOnly = false)
        {
            var options = new DbContextOptionsBuilder<TestAppDbContext>()
                .UseWitDb($"Data Source={dbPath}")
                .Options;

            return new DatabaseSettingsProvider(
                () => new TestAppDbContext(options),
                isReadOnly);
        }

        #endregion
    }
}
