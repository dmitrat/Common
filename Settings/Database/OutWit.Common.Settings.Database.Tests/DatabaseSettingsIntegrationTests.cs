using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using OutWit.Common.Settings.Configuration;
using OutWit.Common.Settings.Database;
using OutWit.Common.Settings.Database.Tests.Utils;
using OutWit.Database.EntityFramework.Extensions;

namespace OutWit.Common.Settings.Database.Tests
{
    [TestFixture]
    public class DatabaseSettingsIntegrationTests
    {
        #region Fields

        private string m_testDir = null!;

        #endregion

        #region Setup

        [SetUp]
        public void Setup()
        {
            m_testDir = Path.Combine(Path.GetTempPath(), "OutWit.Settings.Db.Integration", Guid.NewGuid().ToString());
            Directory.CreateDirectory(m_testDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(m_testDir))
                Directory.Delete(m_testDir, recursive: true);
        }

        #endregion

        #region Load Tests

        [Test]
        public void LoadCreatesCollectionsFromDatabaseTest()
        {
            var defaultDb = GetDbPath("defaults");
            var provider = new DatabaseSettingsProvider(
                o => o.UseWitDb($"Data Source={defaultDb}"));

            provider.Write("General", new[]
            {
                new Providers.SettingsEntry { Group = "General", Key = "UserName", Value = "admin", ValueKind = "String" },
                new Providers.SettingsEntry { Group = "General", Key = "DarkMode", Value = "True", ValueKind = "Boolean" },
                new Providers.SettingsEntry { Group = "General", Key = "MaxRetries", Value = "5", ValueKind = "Integer" }
            });

            var manager = new SettingsBuilder()
                .UseDatabase(o => o.UseWitDb($"Data Source={defaultDb}"), SettingsScope.Default)
                .Build();

            manager.Load();

            var collection = manager["General"];
            Assert.That(collection, Has.Count.EqualTo(3));
            Assert.That(collection["UserName"].Value, Is.EqualTo("admin"));
            Assert.That(collection["DarkMode"].Value, Is.EqualTo(true));
            Assert.That(collection["MaxRetries"].Value, Is.EqualTo(5));
        }

        [Test]
        public void LoadMultipleGroupsFromDatabaseTest()
        {
            var defaultDb = GetDbPath("defaults");
            SeedDefaults(defaultDb,
                ("General", "AppName", "TestApp", "String"),
                ("Advanced", "Timeout", "30", "Integer"),
                ("Advanced", "Verbose", "False", "Boolean"));

            var manager = new SettingsBuilder()
                .UseDatabase(o => o.UseWitDb($"Data Source={defaultDb}"), SettingsScope.Default)
                .Build();

            manager.Load();

            Assert.That(manager.Collections, Has.Count.EqualTo(2));
            Assert.That(manager["General"]["AppName"].Value, Is.EqualTo("TestApp"));
            Assert.That(manager["Advanced"]["Timeout"].Value, Is.EqualTo(30));
            Assert.That(manager["Advanced"]["Verbose"].Value, Is.EqualTo(false));
        }

        [Test]
        public void LoadOverridesDefaultWithUserDatabaseTest()
        {
            var defaultDb = GetDbPath("defaults");
            var userDb = GetDbPath("user");

            SeedDefaults(defaultDb,
                ("General", "UserName", "admin", "String"),
                ("General", "DarkMode", "False", "Boolean"));

            var userProvider = new DatabaseSettingsProvider(
                o => o.UseWitDb($"Data Source={userDb}"));
            userProvider.Write("General", new[]
            {
                new Providers.SettingsEntry { Group = "General", Key = "UserName", Value = "john", ValueKind = "String" }
            });

            var manager = new SettingsBuilder()
                .UseDatabase(o => o.UseWitDb($"Data Source={defaultDb}"), SettingsScope.Default)
                .UseDatabase(o => o.UseWitDb($"Data Source={userDb}"), SettingsScope.User)
                .Build();

            manager.Load();

            Assert.That(manager["General"]["UserName"].Value, Is.EqualTo("john"));
            Assert.That(manager["General"]["UserName"].IsDefault, Is.False);
            Assert.That(manager["General"]["DarkMode"].Value, Is.EqualTo(false));
            Assert.That(manager["General"]["DarkMode"].IsDefault, Is.True);
        }

        [Test]
        public void LoadHandlesEnumWithTagTest()
        {
            var defaultDb = GetDbPath("defaults");
            var provider = new DatabaseSettingsProvider(
                o => o.UseWitDb($"Data Source={defaultDb}"));

            provider.Write("General", new[]
            {
                new Providers.SettingsEntry { Group = "General", Key = "Day", Value = "Tuesday", ValueKind = "Enum", Tag = "System.DayOfWeek, System.Runtime" }
            });

            var manager = new SettingsBuilder()
                .UseDatabase(o => o.UseWitDb($"Data Source={defaultDb}"), SettingsScope.Default)
                .Build();

            manager.Load();

            Assert.That(manager["General"]["Day"].Value, Is.EqualTo(DayOfWeek.Tuesday));
            Assert.That(manager["General"]["Day"].Tag, Is.EqualTo("System.DayOfWeek, System.Runtime"));
        }

        [Test]
        public void LoadHandlesHiddenSettingsTest()
        {
            var defaultDb = GetDbPath("defaults");
            var provider = new DatabaseSettingsProvider(
                o => o.UseWitDb($"Data Source={defaultDb}"));

            provider.Write("General", new[]
            {
                new Providers.SettingsEntry { Group = "General", Key = "ApiKey", Value = "secret123", ValueKind = "Password", Hidden = true },
                new Providers.SettingsEntry { Group = "General", Key = "Name", Value = "admin", ValueKind = "String" }
            });

            var manager = new SettingsBuilder()
                .UseDatabase(o => o.UseWitDb($"Data Source={defaultDb}"), SettingsScope.Default)
                .Build();

            manager.Load();

            Assert.That(manager["General"]["ApiKey"].Hidden, Is.True);
            Assert.That(manager["General"]["Name"].Hidden, Is.False);
        }

        [Test]
        public void LoadHandlesScalarTypesTest()
        {
            // WitDb has a 9-row-per-table limit — see _WitDatabase/SaveChanges-Limit-Bug.md
            // Split built-in types across two tests with separate databases.
            var defaultDb = GetDbPath("defaults");
            var provider = new DatabaseSettingsProvider(
                o => o.UseWitDb($"Data Source={defaultDb}"));

            provider.Write("Types", new[]
            {
                new Providers.SettingsEntry { Group = "Types", Key = "S", Value = "hello", ValueKind = "String" },
                new Providers.SettingsEntry { Group = "Types", Key = "I", Value = "42", ValueKind = "Integer" },
                new Providers.SettingsEntry { Group = "Types", Key = "L", Value = "9999999999", ValueKind = "Long" },
                new Providers.SettingsEntry { Group = "Types", Key = "D", Value = "3.14", ValueKind = "Double" },
                new Providers.SettingsEntry { Group = "Types", Key = "Dec", Value = "99.99", ValueKind = "Decimal" },
                new Providers.SettingsEntry { Group = "Types", Key = "B", Value = "True", ValueKind = "Boolean" }
            });

            var manager = new SettingsBuilder()
                .UseDatabase(o => o.UseWitDb($"Data Source={defaultDb}"), SettingsScope.Default)
                .Build();

            manager.Load();

            var c = manager["Types"];
            Assert.That(c["S"].Value, Is.EqualTo("hello"));
            Assert.That(c["I"].Value, Is.EqualTo(42));
            Assert.That(c["L"].Value, Is.EqualTo(9999999999L));
            Assert.That(c["D"].Value, Is.EqualTo(3.14));
            Assert.That(c["Dec"].Value, Is.EqualTo(99.99m));
            Assert.That(c["B"].Value, Is.EqualTo(true));
        }

        [Test]
        public void LoadHandlesComplexTypesTest()
        {
            // WitDb has a 9-row-per-table limit — see _WitDatabase/SaveChanges-Limit-Bug.md
            var defaultDb = GetDbPath("defaults");
            var provider = new DatabaseSettingsProvider(
                o => o.UseWitDb($"Data Source={defaultDb}"));

            provider.Write("Types", new[]
            {
                new Providers.SettingsEntry { Group = "Types", Key = "TS", Value = "01:30:00", ValueKind = "TimeSpan" },
                new Providers.SettingsEntry { Group = "Types", Key = "G", Value = "d3b07384-d113-4ec6-a7dc-e38b0d171b01", ValueKind = "Guid" },
                new Providers.SettingsEntry { Group = "Types", Key = "DT", Value = "2025-06-15T10:30:00.0000000Z", ValueKind = "DateTime" },
                new Providers.SettingsEntry { Group = "Types", Key = "SL", Value = "a, b, c", ValueKind = "StringList" },
                new Providers.SettingsEntry { Group = "Types", Key = "IL", Value = "1, 2, 3", ValueKind = "IntegerList" },
                new Providers.SettingsEntry { Group = "Types", Key = "DL", Value = "1.1, 2.2", ValueKind = "DoubleList" }
            });

            var manager = new SettingsBuilder()
                .UseDatabase(o => o.UseWitDb($"Data Source={defaultDb}"), SettingsScope.Default)
                .Build();

            manager.Load();

            var c = manager["Types"];
            Assert.That(c["TS"].Value, Is.EqualTo(TimeSpan.FromMinutes(90)));
            Assert.That(c["G"].Value, Is.EqualTo(Guid.Parse("d3b07384-d113-4ec6-a7dc-e38b0d171b01")));
            Assert.That(c["SL"].Value, Is.EquivalentTo(new[] { "a", "b", "c" }));
            Assert.That(c["IL"].Value, Is.EquivalentTo(new[] { 1, 2, 3 }));
            Assert.That(c["DL"].Value, Is.EquivalentTo(new[] { 1.1, 2.2 }));
        }

        #endregion

        #region Save Tests

        [Test]
        public void SaveWritesModifiedValuesToUserDatabaseTest()
        {
            var defaultDb = GetDbPath("defaults");
            var userDb = GetDbPath("user");

            SeedDefaults(defaultDb,
                ("General", "UserName", "admin", "String"),
                ("General", "DarkMode", "False", "Boolean"));

            var manager = new SettingsBuilder()
                .UseDatabase(o => o.UseWitDb($"Data Source={defaultDb}"), SettingsScope.Default)
                .UseDatabase(o => o.UseWitDb($"Data Source={userDb}"), SettingsScope.User)
                .Build();

            manager.Load();
            manager["General"]["UserName"].Value = "john";
            manager["General"]["DarkMode"].Value = true;
            manager.Save();

            var readBack = new DatabaseSettingsProvider(
                o => o.UseWitDb($"Data Source={userDb}"));
            var entries = readBack.Read("General");
            Assert.That(entries, Has.Count.EqualTo(2));
            Assert.That(entries.First(e => e.Key == "UserName").Value, Is.EqualTo("john"));
            Assert.That(entries.First(e => e.Key == "DarkMode").Value, Is.EqualTo("True"));
        }

        [Test]
        public void SavePreservesTagAndHiddenTest()
        {
            var defaultDb = GetDbPath("defaults");
            var userDb = GetDbPath("user");

            var defaultProvider = new DatabaseSettingsProvider(
                o => o.UseWitDb($"Data Source={defaultDb}"));
            defaultProvider.Write("General", new[]
            {
                new Providers.SettingsEntry { Group = "General", Key = "Mode", Value = "Monday", ValueKind = "Enum", Tag = "System.DayOfWeek, System.Runtime" },
                new Providers.SettingsEntry { Group = "General", Key = "ApiKey", Value = "secret", ValueKind = "Password", Hidden = true }
            });

            var manager = new SettingsBuilder()
                .UseDatabase(o => o.UseWitDb($"Data Source={defaultDb}"), SettingsScope.Default)
                .UseDatabase(o => o.UseWitDb($"Data Source={userDb}"), SettingsScope.User)
                .Build();

            manager.Load();
            manager.Save();

            var readBack = new DatabaseSettingsProvider(
                o => o.UseWitDb($"Data Source={userDb}"));
            var entries = readBack.Read("General");

            var mode = entries.First(e => e.Key == "Mode");
            Assert.That(mode.Tag, Is.EqualTo("System.DayOfWeek, System.Runtime"));

            var apiKey = entries.First(e => e.Key == "ApiKey");
            Assert.That(apiKey.Hidden, Is.True);
        }

        #endregion

        #region Full Round-Trip Tests

        [Test]
        public void FullRoundTripLoadModifySaveReloadTest()
        {
            var defaultDb = GetDbPath("defaults");
            var userDb = GetDbPath("user");

            SeedDefaults(defaultDb,
                ("General", "UserName", "admin", "String"),
                ("General", "DarkMode", "False", "Boolean"),
                ("General", "Port", "8080", "Integer"));

            var manager1 = new SettingsBuilder()
                .UseDatabase(o => o.UseWitDb($"Data Source={defaultDb}"), SettingsScope.Default)
                .UseDatabase(o => o.UseWitDb($"Data Source={userDb}"), SettingsScope.User)
                .Build();

            manager1.Load();
            manager1["General"]["UserName"].Value = "john";
            manager1["General"]["Port"].Value = 9090;
            manager1.Save();

            var manager2 = new SettingsBuilder()
                .UseDatabase(o => o.UseWitDb($"Data Source={defaultDb}"), SettingsScope.Default)
                .UseDatabase(o => o.UseWitDb($"Data Source={userDb}"), SettingsScope.User)
                .Build();

            manager2.Load();

            Assert.That(manager2["General"]["UserName"].Value, Is.EqualTo("john"));
            Assert.That(manager2["General"]["UserName"].IsDefault, Is.False);

            Assert.That(manager2["General"]["DarkMode"].Value, Is.EqualTo(false));
            Assert.That(manager2["General"]["DarkMode"].IsDefault, Is.True);

            Assert.That(manager2["General"]["Port"].Value, Is.EqualTo(9090));
            Assert.That(manager2["General"]["Port"].IsDefault, Is.False);
        }

        [Test]
        public void FullRoundTripMultipleGroupsTest()
        {
            var defaultDb = GetDbPath("defaults");
            var userDb = GetDbPath("user");

            SeedDefaults(defaultDb,
                ("General", "Name", "app", "String"),
                ("Advanced", "Timeout", "30", "Integer"),
                ("Advanced", "Verbose", "False", "Boolean"));

            var manager1 = new SettingsBuilder()
                .UseDatabase(o => o.UseWitDb($"Data Source={defaultDb}"), SettingsScope.Default)
                .UseDatabase(o => o.UseWitDb($"Data Source={userDb}"), SettingsScope.User)
                .Build();

            manager1.Load();
            manager1["General"]["Name"].Value = "myapp";
            manager1["Advanced"]["Timeout"].Value = 60;
            manager1.Save();

            var manager2 = new SettingsBuilder()
                .UseDatabase(o => o.UseWitDb($"Data Source={defaultDb}"), SettingsScope.Default)
                .UseDatabase(o => o.UseWitDb($"Data Source={userDb}"), SettingsScope.User)
                .Build();

            manager2.Load();

            Assert.That(manager2["General"]["Name"].Value, Is.EqualTo("myapp"));
            Assert.That(manager2["Advanced"]["Timeout"].Value, Is.EqualTo(60));
            Assert.That(manager2["Advanced"]["Verbose"].Value, Is.EqualTo(false));
            Assert.That(manager2["Advanced"]["Verbose"].IsDefault, Is.True);
        }

        [Test]
        public void FullRoundTripEnumValuesTest()
        {
            var defaultDb = GetDbPath("defaults");
            var userDb = GetDbPath("user");

            var defaultProvider = new DatabaseSettingsProvider(
                o => o.UseWitDb($"Data Source={defaultDb}"));
            defaultProvider.Write("General", new[]
            {
                new Providers.SettingsEntry { Group = "General", Key = "Day", Value = "Monday", ValueKind = "Enum", Tag = "System.DayOfWeek, System.Runtime" }
            });

            var manager1 = new SettingsBuilder()
                .UseDatabase(o => o.UseWitDb($"Data Source={defaultDb}"), SettingsScope.Default)
                .UseDatabase(o => o.UseWitDb($"Data Source={userDb}"), SettingsScope.User)
                .Build();

            manager1.Load();
            manager1["General"]["Day"].Value = DayOfWeek.Friday;
            manager1.Save();

            var manager2 = new SettingsBuilder()
                .UseDatabase(o => o.UseWitDb($"Data Source={defaultDb}"), SettingsScope.Default)
                .UseDatabase(o => o.UseWitDb($"Data Source={userDb}"), SettingsScope.User)
                .Build();

            manager2.Load();

            Assert.That(manager2["General"]["Day"].Value, Is.EqualTo(DayOfWeek.Friday));
            Assert.That(manager2["General"]["Day"].DefaultValue, Is.EqualTo(DayOfWeek.Monday));
            Assert.That(manager2["General"]["Day"].IsDefault, Is.False);
        }

        #endregion

        #region Merge Tests

        [Test]
        public void MergeAddsNewSettingsToUserDatabaseTest()
        {
            var defaultDb = GetDbPath("defaults");
            var userDb = GetDbPath("user");

            SeedDefaults(defaultDb,
                ("General", "Name", "admin", "String"),
                ("General", "Theme", "Light", "String"));

            var userProvider = new DatabaseSettingsProvider(
                o => o.UseWitDb($"Data Source={userDb}"));
            userProvider.Write("General", new[]
            {
                new Providers.SettingsEntry { Group = "General", Key = "Name", Value = "john", ValueKind = "String" }
            });

            var manager = new SettingsBuilder()
                .UseDatabase(o => o.UseWitDb($"Data Source={defaultDb}"), SettingsScope.Default)
                .UseDatabase(o => o.UseWitDb($"Data Source={userDb}"), SettingsScope.User)
                .Build();

            manager.Merge();

            var readBack = new DatabaseSettingsProvider(
                o => o.UseWitDb($"Data Source={userDb}"));
            var entries = readBack.Read("General");

            Assert.That(entries, Has.Count.EqualTo(2));
            Assert.That(entries.First(e => e.Key == "Name").Value, Is.EqualTo("john"));
            Assert.That(entries.First(e => e.Key == "Theme").Value, Is.EqualTo("Light"));
        }

        [Test]
        public void MergeRemovesObsoleteSettingsTest()
        {
            var defaultDb = GetDbPath("defaults");
            var userDb = GetDbPath("user");

            SeedDefaults(defaultDb,
                ("General", "Name", "admin", "String"));

            var userProvider = new DatabaseSettingsProvider(
                o => o.UseWitDb($"Data Source={userDb}"));
            userProvider.Write("General", new[]
            {
                new Providers.SettingsEntry { Group = "General", Key = "Name", Value = "john", ValueKind = "String" },
                new Providers.SettingsEntry { Group = "General", Key = "OldSetting", Value = "obsolete", ValueKind = "String" }
            });

            var manager = new SettingsBuilder()
                .UseDatabase(o => o.UseWitDb($"Data Source={defaultDb}"), SettingsScope.Default)
                .UseDatabase(o => o.UseWitDb($"Data Source={userDb}"), SettingsScope.User)
                .Build();

            manager.Merge();

            var readBack = new DatabaseSettingsProvider(
                o => o.UseWitDb($"Data Source={userDb}"));
            var entries = readBack.Read("General");

            Assert.That(entries, Has.Count.EqualTo(1));
            Assert.That(entries[0].Key, Is.EqualTo("Name"));
            Assert.That(entries[0].Value, Is.EqualTo("john"));
        }

        [Test]
        public void MergeThenLoadProducesCorrectValuesTest()
        {
            var defaultDb = GetDbPath("defaults");
            var userDb = GetDbPath("user");

            SeedDefaults(defaultDb,
                ("General", "Name", "admin", "String"),
                ("General", "NewSetting", "default", "String"));

            var userProvider = new DatabaseSettingsProvider(
                o => o.UseWitDb($"Data Source={userDb}"));
            userProvider.Write("General", new[]
            {
                new Providers.SettingsEntry { Group = "General", Key = "Name", Value = "john", ValueKind = "String" },
                new Providers.SettingsEntry { Group = "General", Key = "OldSetting", Value = "obsolete", ValueKind = "String" }
            });

            var manager = new SettingsBuilder()
                .UseDatabase(o => o.UseWitDb($"Data Source={defaultDb}"), SettingsScope.Default)
                .UseDatabase(o => o.UseWitDb($"Data Source={userDb}"), SettingsScope.User)
                .Build();

            manager.Merge();
            manager.Load();

            Assert.That(manager["General"]["Name"].Value, Is.EqualTo("john"));
            Assert.That(manager["General"]["Name"].IsDefault, Is.False);

            Assert.That(manager["General"]["NewSetting"].Value, Is.EqualTo("default"));
            Assert.That(manager["General"]["NewSetting"].IsDefault, Is.True);
        }

        #endregion

        #region Three-Scope Tests

        [Test]
        public void ThreeScopeResolutionTest()
        {
            var defaultDb = GetDbPath("defaults");
            var globalDb = GetDbPath("global");
            var userDb = GetDbPath("user");

            SeedDefaults(defaultDb,
                ("General", "A", "default_a", "String"),
                ("General", "B", "default_b", "String"),
                ("General", "C", "default_c", "String"));

            var globalProvider = new DatabaseSettingsProvider(
                o => o.UseWitDb($"Data Source={globalDb}"));
            globalProvider.Write("General", new[]
            {
                new Providers.SettingsEntry { Group = "General", Key = "A", Value = "global_a", ValueKind = "String" },
                new Providers.SettingsEntry { Group = "General", Key = "B", Value = "global_b", ValueKind = "String" }
            });

            var userProvider = new DatabaseSettingsProvider(
                o => o.UseWitDb($"Data Source={userDb}"));
            userProvider.Write("General", new[]
            {
                new Providers.SettingsEntry { Group = "General", Key = "A", Value = "user_a", ValueKind = "String" }
            });

            var manager = new SettingsBuilder()
                .UseDatabase(o => o.UseWitDb($"Data Source={defaultDb}"), SettingsScope.Default)
                .AddProvider(SettingsScope.Global, new DatabaseSettingsProvider(
                    o => o.UseWitDb($"Data Source={globalDb}")))
                .UseDatabase(o => o.UseWitDb($"Data Source={userDb}"), SettingsScope.User)
                .Build();

            manager.Load();

            Assert.That(manager["General"]["A"].Value, Is.EqualTo("user_a"));
            Assert.That(manager["General"]["B"].Value, Is.EqualTo("global_b"));
            Assert.That(manager["General"]["C"].Value, Is.EqualTo("default_c"));
            Assert.That(manager["General"]["C"].IsDefault, Is.True);
        }

        #endregion

        #region Edge Cases

        [Test]
        public void LoadWithEmptyUserDatabaseUsesDefaultsTest()
        {
            var defaultDb = GetDbPath("defaults");
            var userDb = GetDbPath("user");

            SeedDefaults(defaultDb,
                ("General", "Name", "admin", "String"));

            var manager = new SettingsBuilder()
                .UseDatabase(o => o.UseWitDb($"Data Source={defaultDb}"), SettingsScope.Default)
                .UseDatabase(o => o.UseWitDb($"Data Source={userDb}"), SettingsScope.User)
                .Build();

            manager.Load();

            Assert.That(manager["General"]["Name"].Value, Is.EqualTo("admin"));
            Assert.That(manager["General"]["Name"].IsDefault, Is.True);
        }

        [Test]
        public void UnknownValueKindIsSkippedDuringLoadTest()
        {
            var defaultDb = GetDbPath("defaults");
            var provider = new DatabaseSettingsProvider(
                o => o.UseWitDb($"Data Source={defaultDb}"));

            provider.Write("General", new[]
            {
                new Providers.SettingsEntry { Group = "General", Key = "Name", Value = "admin", ValueKind = "String" },
                new Providers.SettingsEntry { Group = "General", Key = "Custom", Value = "data", ValueKind = "UnknownType" }
            });

            var manager = new SettingsBuilder()
                .UseDatabase(o => o.UseWitDb($"Data Source={defaultDb}"), SettingsScope.Default)
                .Build();

            manager.Load();

            var collection = manager["General"];
            Assert.That(collection, Has.Count.EqualTo(1));
            Assert.That(collection["Name"].Value, Is.EqualTo("admin"));
        }

        #endregion

        #region Shared Context Integration Tests

        [Test]
        public void SharedContextLoadCreatesCollectionsTest()
        {
            var dbPath = GetDbPath("shared");
            var options = CreateSharedOptions(dbPath);
            InitSharedDatabase(options);

            SeedSharedDatabase(options,
                ("General", "UserName", "admin", "String"),
                ("General", "DarkMode", "True", "Boolean"));

            var manager = new SettingsBuilder()
                .UseDatabase(() => new TestAppDbContext(options), SettingsScope.Default)
                .Build();

            manager.Load();

            var collection = manager["General"];
            Assert.That(collection, Has.Count.EqualTo(2));
            Assert.That(collection["UserName"].Value, Is.EqualTo("admin"));
            Assert.That(collection["DarkMode"].Value, Is.EqualTo(true));
        }

        [Test]
        public void SharedContextFullRoundTripTest()
        {
            var dbPath = GetDbPath("shared");
            var options = CreateSharedOptions(dbPath);
            InitSharedDatabase(options);

            SeedSharedDatabase(options,
                ("General", "UserName", "admin", "String"),
                ("General", "DarkMode", "False", "Boolean"),
                ("General", "Port", "8080", "Integer"));

            var userDb = GetDbPath("shared_user");

            var manager1 = new SettingsBuilder()
                .UseDatabase(() => new TestAppDbContext(options), SettingsScope.Default)
                .UseDatabase(o => o.UseWitDb($"Data Source={userDb}"), SettingsScope.User)
                .Build();

            manager1.Load();
            manager1["General"]["UserName"].Value = "john";
            manager1["General"]["Port"].Value = 9090;
            manager1.Save();

            var manager2 = new SettingsBuilder()
                .UseDatabase(() => new TestAppDbContext(options), SettingsScope.Default)
                .UseDatabase(o => o.UseWitDb($"Data Source={userDb}"), SettingsScope.User)
                .Build();

            manager2.Load();

            Assert.That(manager2["General"]["UserName"].Value, Is.EqualTo("john"));
            Assert.That(manager2["General"]["UserName"].IsDefault, Is.False);
            Assert.That(manager2["General"]["DarkMode"].Value, Is.EqualTo(false));
            Assert.That(manager2["General"]["DarkMode"].IsDefault, Is.True);
            Assert.That(manager2["General"]["Port"].Value, Is.EqualTo(9090));
            Assert.That(manager2["General"]["Port"].IsDefault, Is.False);
        }

        [Test]
        public void SharedContextMergeTest()
        {
            var dbPath = GetDbPath("shared");
            var options = CreateSharedOptions(dbPath);
            InitSharedDatabase(options);

            SeedSharedDatabase(options,
                ("General", "Name", "admin", "String"),
                ("General", "Theme", "Light", "String"));

            var userDb = GetDbPath("shared_user");
            var userProvider = new DatabaseSettingsProvider(
                o => o.UseWitDb($"Data Source={userDb}"));
            userProvider.Write("General", new[]
            {
                new Providers.SettingsEntry { Group = "General", Key = "Name", Value = "john", ValueKind = "String" }
            });

            var manager = new SettingsBuilder()
                .UseDatabase(() => new TestAppDbContext(options), SettingsScope.Default)
                .UseDatabase(o => o.UseWitDb($"Data Source={userDb}"), SettingsScope.User)
                .Build();

            manager.Merge();

            var readBack = new DatabaseSettingsProvider(
                o => o.UseWitDb($"Data Source={userDb}"));
            var entries = readBack.Read("General");

            Assert.That(entries, Has.Count.EqualTo(2));
            Assert.That(entries.First(e => e.Key == "Name").Value, Is.EqualTo("john"));
            Assert.That(entries.First(e => e.Key == "Theme").Value, Is.EqualTo("Light"));
        }

        [Test]
        public void SharedContextBothScopesUseSharedDbContextTest()
        {
            var defaultDbPath = GetDbPath("shared_default");
            var defaultOptions = CreateSharedOptions(defaultDbPath);
            InitSharedDatabase(defaultOptions);

            var userDbPath = GetDbPath("shared_user");
            var userOptions = CreateSharedOptions(userDbPath);
            InitSharedDatabase(userOptions);

            SeedSharedDatabase(defaultOptions,
                ("General", "Name", "admin", "String"),
                ("General", "DarkMode", "False", "Boolean"));

            SeedSharedDatabase(userOptions,
                ("General", "Name", "john", "String"));

            var manager = new SettingsBuilder()
                .UseDatabase(() => new TestAppDbContext(defaultOptions), SettingsScope.Default)
                .UseDatabase(() => new TestAppDbContext(userOptions), SettingsScope.User)
                .Build();

            manager.Load();

            Assert.That(manager["General"]["Name"].Value, Is.EqualTo("john"));
            Assert.That(manager["General"]["Name"].IsDefault, Is.False);
            Assert.That(manager["General"]["DarkMode"].Value, Is.EqualTo(false));
            Assert.That(manager["General"]["DarkMode"].IsDefault, Is.True);
        }

        [Test]
        public void SharedContextMultipleGroupsTest()
        {
            var dbPath = GetDbPath("shared");
            var options = CreateSharedOptions(dbPath);
            InitSharedDatabase(options);

            SeedSharedDatabase(options,
                ("General", "Name", "app", "String"),
                ("Advanced", "Timeout", "30", "Integer"),
                ("Advanced", "Verbose", "False", "Boolean"));

            var manager = new SettingsBuilder()
                .UseDatabase(() => new TestAppDbContext(options), SettingsScope.Default)
                .Build();

            manager.Load();

            Assert.That(manager.Collections, Has.Count.EqualTo(2));
            Assert.That(manager["General"]["Name"].Value, Is.EqualTo("app"));
            Assert.That(manager["Advanced"]["Timeout"].Value, Is.EqualTo(30));
            Assert.That(manager["Advanced"]["Verbose"].Value, Is.EqualTo(false));
        }

        #endregion

        #region Group Metadata Integration Tests

        [Test]
        public void GroupMetadataRoundTripTest()
        {
            var defaultDb = GetDbPath("defaults");
            var userDb = GetDbPath("user");

            SeedDefaults(defaultDb,
                ("General", "Name", "admin", "String"),
                ("Advanced", "Timeout", "30", "Integer"));

            var defaultProvider = new DatabaseSettingsProvider(
                o => o.UseWitDb($"Data Source={defaultDb}"));
            defaultProvider.WriteGroupInfo(new[]
            {
                new Providers.SettingsGroupInfo { Group = "General", DisplayName = "Main", Priority = 1 },
                new Providers.SettingsGroupInfo { Group = "Advanced", DisplayName = "Extra", Priority = 2 }
            });

            var manager1 = new SettingsBuilder()
                .UseDatabase(o => o.UseWitDb($"Data Source={defaultDb}"), SettingsScope.Default)
                .UseDatabase(o => o.UseWitDb($"Data Source={userDb}"), SettingsScope.User)
                .Build();

            manager1.Load();

            Assert.That(manager1["General"].DisplayName, Is.EqualTo("Main"));
            Assert.That(manager1["General"].Priority, Is.EqualTo(1));

            manager1["General"].Priority = 10;
            manager1.Save();

            var userProvider = new DatabaseSettingsProvider(
                o => o.UseWitDb($"Data Source={userDb}"));
            var infos = userProvider.ReadGroupInfo();
            Assert.That(infos.First(g => g.Group == "General").Priority, Is.EqualTo(10));
        }

        [Test]
        public void BackwardCompatibilityWithoutGroupMetadataTest()
        {
            var defaultDb = GetDbPath("defaults");

            SeedDefaults(defaultDb,
                ("General", "Name", "admin", "String"));

            var manager = new SettingsBuilder()
                .UseDatabase(o => o.UseWitDb($"Data Source={defaultDb}"), SettingsScope.Default)
                .Build();

            manager.Load();

            Assert.That(manager["General"].DisplayName, Is.EqualTo("General"));
            Assert.That(manager["General"].Priority, Is.EqualTo(0));
            Assert.That(manager["General"]["Name"].Value, Is.EqualTo("admin"));
        }

        [Test]
        public void ConfigureGroupOverridesDatabaseMetadataTest()
        {
            var defaultDb = GetDbPath("defaults");

            SeedDefaults(defaultDb,
                ("General", "Name", "admin", "String"));

            var defaultProvider = new DatabaseSettingsProvider(
                o => o.UseWitDb($"Data Source={defaultDb}"));
            defaultProvider.WriteGroupInfo(new[]
            {
                new Providers.SettingsGroupInfo { Group = "General", DisplayName = "From DB", Priority = 5 }
            });

            var manager = new SettingsBuilder()
                .UseDatabase(o => o.UseWitDb($"Data Source={defaultDb}"), SettingsScope.Default)
                .ConfigureGroup("General", priority: 1, displayName: "From Code")
                .Build();

            manager.Load();

            Assert.That(manager["General"].DisplayName, Is.EqualTo("From Code"));
            Assert.That(manager["General"].Priority, Is.EqualTo(1));
        }

        #endregion

        #region Merge Integration Tests

        [Test]
        public void MergeRemovesStaleGroupsAndMetadataTest()
        {
            var defaultDb = GetDbPath("defaults");
            var userDb = GetDbPath("user");

            SeedDefaults(defaultDb,
                ("General", "Name", "admin", "String"));

            var defaultProvider = new DatabaseSettingsProvider(
                o => o.UseWitDb($"Data Source={defaultDb}"));
            defaultProvider.WriteGroupInfo(new[]
            {
                new Providers.SettingsGroupInfo { Group = "General", DisplayName = "Main", Priority = 1 }
            });

            SeedDefaults(userDb,
                ("General", "Name", "john", "String"),
                ("Legacy", "Old", "obsolete", "String"));

            var userProviderSeed = new DatabaseSettingsProvider(
                o => o.UseWitDb($"Data Source={userDb}"));
            userProviderSeed.WriteGroupInfo(new[]
            {
                new Providers.SettingsGroupInfo { Group = "General", DisplayName = "User Main", Priority = 5 },
                new Providers.SettingsGroupInfo { Group = "Legacy", DisplayName = "Old", Priority = 10 }
            });

            var manager = new SettingsBuilder()
                .UseDatabase(o => o.UseWitDb($"Data Source={defaultDb}"), SettingsScope.Default)
                .UseDatabase(o => o.UseWitDb($"Data Source={userDb}"), SettingsScope.User)
                .Build();

            manager.Merge();

            var userProvider = new DatabaseSettingsProvider(
                o => o.UseWitDb($"Data Source={userDb}"));
            Assert.That(userProvider.GetGroups(), Has.Count.EqualTo(1));
            Assert.That(userProvider.GetGroups()[0], Is.EqualTo("General"));
            Assert.That(userProvider.Read("Legacy"), Is.Empty);

            var infos = userProvider.ReadGroupInfo();
            Assert.That(infos, Has.Count.EqualTo(1));
            Assert.That(infos[0].Group, Is.EqualTo("General"));
            Assert.That(infos[0].DisplayName, Is.EqualTo("User Main"));
            Assert.That(infos[0].Priority, Is.EqualTo(5));
        }

        #endregion

        #region Helpers

        private string GetDbPath(string name)
        {
            return Path.Combine(m_testDir, $"{name}_{Guid.NewGuid():N}.witdb");
        }

        private static void SeedDefaults(string dbPath, params (string Group, string Key, string Value, string ValueKind)[] entries)
        {
            var provider = new DatabaseSettingsProvider(
                o => o.UseWitDb($"Data Source={dbPath}"));

            foreach (var group in entries.GroupBy(e => e.Group))
            {
                provider.Write(group.Key, group.Select(e => new Providers.SettingsEntry
                {
                    Group = e.Group,
                    Key = e.Key,
                    Value = e.Value,
                    ValueKind = e.ValueKind
                }).ToArray());
            }
        }

        private static DbContextOptions<TestAppDbContext> CreateSharedOptions(string dbPath)
        {
            return new DbContextOptionsBuilder<TestAppDbContext>()
                .UseWitDb($"Data Source={dbPath}")
                .Options;
        }

        private static void InitSharedDatabase(DbContextOptions<TestAppDbContext> options)
        {
            using var context = new TestAppDbContext(options);
            context.Database.EnsureCreated();
        }

        private static void SeedSharedDatabase(DbContextOptions<TestAppDbContext> options, params (string Group, string Key, string Value, string ValueKind)[] entries)
        {
            var provider = new DatabaseSettingsProvider(
                () => new TestAppDbContext(options));

            foreach (var group in entries.GroupBy(e => e.Group))
            {
                provider.Write(group.Key, group.Select(e => new Providers.SettingsEntry
                {
                    Group = e.Group,
                    Key = e.Key,
                    Value = e.Value,
                    ValueKind = e.ValueKind
                }).ToArray());
            }
        }

        #endregion
    }
}
