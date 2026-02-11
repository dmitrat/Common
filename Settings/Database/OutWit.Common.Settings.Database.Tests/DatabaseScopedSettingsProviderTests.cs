using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using OutWit.Common.Settings.Configuration;
using OutWit.Common.Settings.Providers;
using OutWit.Database.EntityFramework.Extensions;

namespace OutWit.Common.Settings.Database.Tests
{
    [TestFixture]
    public class DatabaseScopedSettingsProviderTests
    {
        #region Fields

        private string m_testDir = null!;

        #endregion

        #region Setup

        [SetUp]
        public void Setup()
        {
            m_testDir = Path.Combine(Path.GetTempPath(), "OutWit.Settings.Scoped.Tests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(m_testDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(m_testDir))
                Directory.Delete(m_testDir, recursive: true);
        }

        #endregion

        #region Default Scope Tests

        [Test]
        public void DefaultScopeReadWriteRoundTripTest()
        {
            var dbPath = GetDbPath();
            var provider = CreateProvider(dbPath, SettingsScope.Default, userId: null, isReadOnly: false);

            provider.Write("General", new[]
            {
                new SettingsEntry { Group = "General", Key = "Name", Value = "admin", ValueKind = "String" }
            });

            var entries = provider.Read("General");
            Assert.That(entries, Has.Count.EqualTo(1));
            Assert.That(entries[0].Key, Is.EqualTo("Name"));
            Assert.That(entries[0].Value, Is.EqualTo("admin"));
        }

        [Test]
        public void DefaultScopeUsesSettingsTableTest()
        {
            var dbPath = GetDbPath();
            var defaultProvider = CreateProvider(dbPath, SettingsScope.Default, userId: null, isReadOnly: false);
            var globalProvider = CreateProvider(dbPath, SettingsScope.Global, userId: null, isReadOnly: false);

            defaultProvider.Write("General", new[]
            {
                new SettingsEntry { Group = "General", Key = "Name", Value = "default-value", ValueKind = "String" }
            });

            globalProvider.Write("General", new[]
            {
                new SettingsEntry { Group = "General", Key = "Name", Value = "global-value", ValueKind = "String" }
            });

            var defaultEntries = defaultProvider.Read("General");
            var globalEntries = globalProvider.Read("General");

            Assert.That(defaultEntries[0].Value, Is.EqualTo("default-value"));
            Assert.That(globalEntries[0].Value, Is.EqualTo("global-value"));
        }

        #endregion

        #region Global Scope Tests

        [Test]
        public void GlobalScopeReadWriteRoundTripTest()
        {
            var dbPath = GetDbPath();
            var provider = CreateProvider(dbPath, SettingsScope.Global, userId: null);

            provider.Write("General", new[]
            {
                new SettingsEntry { Group = "General", Key = "Port", Value = "8080", ValueKind = "Integer" }
            });

            var entries = provider.Read("General");
            Assert.That(entries, Has.Count.EqualTo(1));
            Assert.That(entries[0].Key, Is.EqualTo("Port"));
            Assert.That(entries[0].Value, Is.EqualTo("8080"));
        }

        [Test]
        public void GlobalScopeIsolatedFromDefaultTest()
        {
            var dbPath = GetDbPath();
            var defaultProvider = CreateProvider(dbPath, SettingsScope.Default, userId: null, isReadOnly: false);
            var globalProvider = CreateProvider(dbPath, SettingsScope.Global, userId: null);

            defaultProvider.Write("General", new[]
            {
                new SettingsEntry { Group = "General", Key = "Name", Value = "default", ValueKind = "String" }
            });

            Assert.That(globalProvider.Read("General"), Is.Empty);
        }

        #endregion

        #region User Scope Tests

        [Test]
        public void UserScopeReadWriteRoundTripTest()
        {
            var dbPath = GetDbPath();
            var provider = CreateProvider(dbPath, SettingsScope.User, userId: "alice");

            provider.Write("General", new[]
            {
                new SettingsEntry { Group = "General", Key = "Theme", Value = "Dark", ValueKind = "String" }
            });

            var entries = provider.Read("General");
            Assert.That(entries, Has.Count.EqualTo(1));
            Assert.That(entries[0].Key, Is.EqualTo("Theme"));
            Assert.That(entries[0].Value, Is.EqualTo("Dark"));
        }

        [Test]
        public void UserScopeIsolatesUsersTest()
        {
            var dbPath = GetDbPath();
            var alice = CreateProvider(dbPath, SettingsScope.User, userId: "alice");
            var bob = CreateProvider(dbPath, SettingsScope.User, userId: "bob");

            alice.Write("General", new[]
            {
                new SettingsEntry { Group = "General", Key = "Theme", Value = "Dark", ValueKind = "String" }
            });

            bob.Write("General", new[]
            {
                new SettingsEntry { Group = "General", Key = "Theme", Value = "Light", ValueKind = "String" }
            });

            var aliceEntries = alice.Read("General");
            var bobEntries = bob.Read("General");

            Assert.That(aliceEntries[0].Value, Is.EqualTo("Dark"));
            Assert.That(bobEntries[0].Value, Is.EqualTo("Light"));
        }

        [Test]
        public void UserScopeIsolatedFromGlobalTest()
        {
            var dbPath = GetDbPath();
            var globalProvider = CreateProvider(dbPath, SettingsScope.Global, userId: null);
            var userProvider = CreateProvider(dbPath, SettingsScope.User, userId: "alice");

            globalProvider.Write("General", new[]
            {
                new SettingsEntry { Group = "General", Key = "Port", Value = "8080", ValueKind = "Integer" }
            });

            Assert.That(userProvider.Read("General"), Is.Empty);
        }

        [Test]
        public void UserScopeGetGroupsReturnsOnlyUserGroupsTest()
        {
            var dbPath = GetDbPath();
            var alice = CreateProvider(dbPath, SettingsScope.User, userId: "alice");
            var bob = CreateProvider(dbPath, SettingsScope.User, userId: "bob");

            alice.Write("General", new[]
            {
                new SettingsEntry { Group = "General", Key = "Name", Value = "Alice", ValueKind = "String" }
            });

            bob.Write("Advanced", new[]
            {
                new SettingsEntry { Group = "Advanced", Key = "Timeout", Value = "30", ValueKind = "Integer" }
            });

            Assert.That(alice.GetGroups(), Has.Count.EqualTo(1));
            Assert.That(alice.GetGroups()[0], Is.EqualTo("General"));

            Assert.That(bob.GetGroups(), Has.Count.EqualTo(1));
            Assert.That(bob.GetGroups()[0], Is.EqualTo("Advanced"));
        }

        [Test]
        public void UserScopeDeleteRemovesOnlyCurrentUserDataTest()
        {
            var dbPath = GetDbPath();
            var alice = CreateProvider(dbPath, SettingsScope.User, userId: "alice");
            var bob = CreateProvider(dbPath, SettingsScope.User, userId: "bob");

            alice.Write("General", new[]
            {
                new SettingsEntry { Group = "General", Key = "Name", Value = "Alice", ValueKind = "String" }
            });

            bob.Write("General", new[]
            {
                new SettingsEntry { Group = "General", Key = "Name", Value = "Bob", ValueKind = "String" }
            });

            alice.Delete();

            Assert.That(alice.Read("General"), Is.Empty);
            Assert.That(bob.Read("General"), Has.Count.EqualTo(1));
            Assert.That(bob.Read("General")[0].Value, Is.EqualTo("Bob"));
        }

        #endregion

        #region ReadOnly Tests

        [Test]
        public void ReadOnlyProviderIgnoresWritesTest()
        {
            var dbPath = GetDbPath();
            var provider = CreateProvider(dbPath, SettingsScope.Default, userId: null, isReadOnly: true);

            provider.Write("General", new[]
            {
                new SettingsEntry { Key = "Name", Value = "admin", ValueKind = "String" }
            });

            Assert.That(provider.Read("General"), Is.Empty);
        }

        [Test]
        public void ReadOnlyProviderIgnoresDeleteTest()
        {
            var dbPath = GetDbPath();
            var writable = CreateProvider(dbPath, SettingsScope.Default, userId: null, isReadOnly: false);

            writable.Write("General", new[]
            {
                new SettingsEntry { Group = "General", Key = "Name", Value = "admin", ValueKind = "String" }
            });

            var readOnly = CreateProvider(dbPath, SettingsScope.Default, userId: null, isReadOnly: true);
            readOnly.Delete();

            Assert.That(readOnly.Read("General"), Has.Count.EqualTo(1));
        }

        #endregion

        #region Group Metadata Tests

        [Test]
        public void GlobalScopeGroupMetadataRoundTripTest()
        {
            var dbPath = GetDbPath();
            var provider = CreateProvider(dbPath, SettingsScope.Global, userId: null);

            provider.WriteGroupInfo(new[]
            {
                new SettingsGroupInfo { Group = "General", DisplayName = "Main", Priority = 1 }
            });

            var readBack = provider.ReadGroupInfo();
            Assert.That(readBack, Has.Count.EqualTo(1));
            Assert.That(readBack[0].Group, Is.EqualTo("General"));
            Assert.That(readBack[0].DisplayName, Is.EqualTo("Main"));
            Assert.That(readBack[0].Priority, Is.EqualTo(1));
        }

        [Test]
        public void UserScopeGroupMetadataIsolatedByUserTest()
        {
            var dbPath = GetDbPath();
            var alice = CreateProvider(dbPath, SettingsScope.User, userId: "alice");
            var bob = CreateProvider(dbPath, SettingsScope.User, userId: "bob");

            alice.WriteGroupInfo(new[]
            {
                new SettingsGroupInfo { Group = "General", DisplayName = "Alice Settings", Priority = 1 }
            });

            bob.WriteGroupInfo(new[]
            {
                new SettingsGroupInfo { Group = "General", DisplayName = "Bob Settings", Priority = 2 }
            });

            var aliceInfo = alice.ReadGroupInfo();
            var bobInfo = bob.ReadGroupInfo();

            Assert.That(aliceInfo[0].DisplayName, Is.EqualTo("Alice Settings"));
            Assert.That(bobInfo[0].DisplayName, Is.EqualTo("Bob Settings"));
        }

        #endregion

        #region Tag and Hidden Tests

        [Test]
        public void WritePreservesTagAndHiddenTest()
        {
            var dbPath = GetDbPath();
            var provider = CreateProvider(dbPath, SettingsScope.User, userId: "alice");

            provider.Write("General", new[]
            {
                new SettingsEntry
                {
                    Group = "General", Key = "Mode", Value = "Alpha",
                    ValueKind = "Enum", Tag = "MyApp.TestEnum, MyApp", Hidden = true
                }
            });

            var entries = provider.Read("General");
            Assert.That(entries[0].Tag, Is.EqualTo("MyApp.TestEnum, MyApp"));
            Assert.That(entries[0].Hidden, Is.True);
        }

        #endregion

        #region Write Replaces Tests

        [Test]
        public void WriteReplacesExistingGroupTest()
        {
            var dbPath = GetDbPath();
            var provider = CreateProvider(dbPath, SettingsScope.User, userId: "alice");

            provider.Write("General", new[]
            {
                new SettingsEntry { Group = "General", Key = "Name", Value = "old", ValueKind = "String" },
                new SettingsEntry { Group = "General", Key = "Port", Value = "80", ValueKind = "Integer" }
            });

            provider.Write("General", new[]
            {
                new SettingsEntry { Group = "General", Key = "Name", Value = "new", ValueKind = "String" }
            });

            var entries = provider.Read("General");
            Assert.That(entries, Has.Count.EqualTo(1));
            Assert.That(entries[0].Value, Is.EqualTo("new"));
        }

        [Test]
        public void WritePreservesOtherGroupsTest()
        {
            var dbPath = GetDbPath();
            var provider = CreateProvider(dbPath, SettingsScope.Global, userId: null);

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

            var advanced = provider.Read("Advanced");
            Assert.That(advanced, Has.Count.EqualTo(1));
            Assert.That(advanced[0].Value, Is.EqualTo("30"));
        }

        #endregion

        #region All Scopes In One Database Tests

        [Test]
        public void AllThreeScopesCoexistInOneDatabaseTest()
        {
            var dbPath = GetDbPath();
            var defaultProvider = CreateProvider(dbPath, SettingsScope.Default, userId: null, isReadOnly: false);
            var globalProvider = CreateProvider(dbPath, SettingsScope.Global, userId: null);
            var userProvider = CreateProvider(dbPath, SettingsScope.User, userId: "alice");

            defaultProvider.Write("General", new[]
            {
                new SettingsEntry { Group = "General", Key = "Name", Value = "default-val", ValueKind = "String" }
            });

            globalProvider.Write("General", new[]
            {
                new SettingsEntry { Group = "General", Key = "Name", Value = "global-val", ValueKind = "String" }
            });

            userProvider.Write("General", new[]
            {
                new SettingsEntry { Group = "General", Key = "Name", Value = "user-val", ValueKind = "String" }
            });

            Assert.That(defaultProvider.Read("General")[0].Value, Is.EqualTo("default-val"));
            Assert.That(globalProvider.Read("General")[0].Value, Is.EqualTo("global-val"));
            Assert.That(userProvider.Read("General")[0].Value, Is.EqualTo("user-val"));
        }

        [Test]
        public void MultipleUsersAndGlobalCoexistTest()
        {
            var dbPath = GetDbPath();
            var global = CreateProvider(dbPath, SettingsScope.Global, userId: null);
            var alice = CreateProvider(dbPath, SettingsScope.User, userId: "alice");
            var bob = CreateProvider(dbPath, SettingsScope.User, userId: "bob");

            global.Write("App", new[]
            {
                new SettingsEntry { Group = "App", Key = "Port", Value = "8080", ValueKind = "Integer" }
            });

            alice.Write("App", new[]
            {
                new SettingsEntry { Group = "App", Key = "Theme", Value = "Dark", ValueKind = "String" }
            });

            bob.Write("App", new[]
            {
                new SettingsEntry { Group = "App", Key = "Theme", Value = "Light", ValueKind = "String" }
            });

            Assert.That(global.GetGroups(), Has.Count.EqualTo(1));
            Assert.That(alice.GetGroups(), Has.Count.EqualTo(1));
            Assert.That(bob.GetGroups(), Has.Count.EqualTo(1));

            Assert.That(global.Read("App")[0].Value, Is.EqualTo("8080"));
            Assert.That(alice.Read("App")[0].Value, Is.EqualTo("Dark"));
            Assert.That(bob.Read("App")[0].Value, Is.EqualTo("Light"));
        }

        #endregion

        #region Helpers

        private string GetDbPath()
        {
            return Path.Combine(m_testDir, $"{Guid.NewGuid()}.witdb");
        }

        private static DatabaseScopedSettingsProvider CreateProvider(
            string dbPath, SettingsScope scope, string? userId, bool isReadOnly = false)
        {
            return new DatabaseScopedSettingsProvider(
                options => options.UseWitDb($"Data Source={dbPath}"),
                scope, userId, isReadOnly);
        }

        #endregion
    }
}
