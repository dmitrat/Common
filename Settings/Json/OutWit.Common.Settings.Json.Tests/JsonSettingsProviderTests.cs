using System;
using System.IO;
using System.Linq;
using OutWit.Common.Settings.Providers;

namespace OutWit.Common.Settings.Json.Tests
{
    [TestFixture]
    public class JsonSettingsProviderTests
    {
        #region Fields

        private string m_testDir = null!;

        #endregion

        #region Setup

        [SetUp]
        public void Setup()
        {
            m_testDir = Path.Combine(Path.GetTempPath(), "OutWit.Settings.Tests", Guid.NewGuid().ToString());
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
        public void ReadReturnsEntriesFromJsonFileTest()
        {
            var path = WriteJsonFile(@"{
  ""General"": [
    { ""key"": ""UserName"", ""value"": ""admin"", ""valueKind"": ""String"" },
    { ""key"": ""DarkMode"", ""value"": ""True"", ""valueKind"": ""Boolean"" }
  ]
}");

            var provider = new JsonSettingsProvider(path);
            var entries = provider.Read("General");

            Assert.That(entries, Has.Count.EqualTo(2));

            Assert.That(entries[0].Group, Is.EqualTo("General"));
            Assert.That(entries[0].Key, Is.EqualTo("UserName"));
            Assert.That(entries[0].Value, Is.EqualTo("admin"));
            Assert.That(entries[0].ValueKind, Is.EqualTo("String"));

            Assert.That(entries[1].Key, Is.EqualTo("DarkMode"));
            Assert.That(entries[1].Value, Is.EqualTo("True"));
            Assert.That(entries[1].ValueKind, Is.EqualTo("Boolean"));
        }

        [Test]
        public void ReadReturnsEmptyForMissingGroupTest()
        {
            var path = WriteJsonFile(@"{
  ""General"": [
    { ""key"": ""Name"", ""value"": ""admin"", ""valueKind"": ""String"" }
  ]
}");

            var provider = new JsonSettingsProvider(path);

            Assert.That(provider.Read("Advanced"), Is.Empty);
        }

        [Test]
        public void ReadReturnsEmptyForMissingFileTest()
        {
            var path = Path.Combine(m_testDir, "nonexistent.json");
            var provider = new JsonSettingsProvider(path);

            Assert.That(provider.Read("General"), Is.Empty);
        }

        [Test]
        public void ReadParsesTagAndHiddenTest()
        {
            var path = WriteJsonFile(@"{
  ""General"": [
    { ""key"": ""Mode"", ""value"": ""Alpha"", ""valueKind"": ""Enum"", ""tag"": ""MyApp.TestEnum, MyApp"", ""hidden"": true }
  ]
}");

            var provider = new JsonSettingsProvider(path);
            var entries = provider.Read("General");

            Assert.That(entries[0].Tag, Is.EqualTo("MyApp.TestEnum, MyApp"));
            Assert.That(entries[0].Hidden, Is.True);
        }

        [Test]
        public void ReadHandlesMissingOptionalFieldsTest()
        {
            var path = WriteJsonFile(@"{
  ""General"": [
    { ""key"": ""Name"", ""value"": ""admin"", ""valueKind"": ""String"" }
  ]
}");

            var provider = new JsonSettingsProvider(path);
            var entries = provider.Read("General");

            Assert.That(entries[0].Tag, Is.EqualTo(""));
            Assert.That(entries[0].Hidden, Is.False);
        }

        #endregion

        #region Write Tests

        [Test]
        public void WriteCreatesJsonFileTest()
        {
            var path = Path.Combine(m_testDir, "output.json");
            var provider = new JsonSettingsProvider(path);

            provider.Write("General", new[]
            {
                new SettingsEntry { Group = "General", Key = "Name", Value = "admin", ValueKind = "String" },
                new SettingsEntry { Group = "General", Key = "Port", Value = "8080", ValueKind = "Integer" }
            });

            Assert.That(File.Exists(path), Is.True);

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
            var path = WriteJsonFile(@"{
  ""General"": [
    { ""key"": ""Name"", ""value"": ""admin"", ""valueKind"": ""String"" }
  ],
  ""Advanced"": [
    { ""key"": ""Timeout"", ""value"": ""30"", ""valueKind"": ""Integer"" }
  ]
}");

            var provider = new JsonSettingsProvider(path);

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
            var path = Path.Combine(m_testDir, "readonly.json");
            var provider = new JsonSettingsProvider(path, isReadOnly: true);

            provider.Write("General", new[]
            {
                new SettingsEntry { Key = "Name", Value = "admin", ValueKind = "String" }
            });

            Assert.That(File.Exists(path), Is.False);
        }

        [Test]
        public void WriteOmitsEmptyTagAndFalseHiddenTest()
        {
            var path = Path.Combine(m_testDir, "compact.json");
            var provider = new JsonSettingsProvider(path);

            provider.Write("General", new[]
            {
                new SettingsEntry { Key = "Name", Value = "admin", ValueKind = "String", Tag = "", Hidden = false }
            });

            var json = File.ReadAllText(path);
            Assert.That(json, Does.Not.Contain("\"tag\""));
            Assert.That(json, Does.Not.Contain("\"hidden\""));
        }

        [Test]
        public void WriteIncludesTagWhenPresentTest()
        {
            var path = Path.Combine(m_testDir, "withtag.json");
            var provider = new JsonSettingsProvider(path);

            provider.Write("General", new[]
            {
                new SettingsEntry { Key = "Mode", Value = "Alpha", ValueKind = "Enum", Tag = "MyApp.TestEnum, MyApp" }
            });

            var json = File.ReadAllText(path);
            Assert.That(json, Does.Contain("\"tag\""));
            Assert.That(json, Does.Contain("MyApp.TestEnum, MyApp"));
        }

        [Test]
        public void WriteIncludesHiddenWhenTrueTest()
        {
            var path = Path.Combine(m_testDir, "withhidden.json");
            var provider = new JsonSettingsProvider(path);

            provider.Write("General", new[]
            {
                new SettingsEntry { Key = "Secret", Value = "123", ValueKind = "String", Hidden = true }
            });

            var json = File.ReadAllText(path);
            Assert.That(json, Does.Contain("\"hidden\": true"));
        }

        [Test]
        public void WriteCreatesDirectoryIfNeededTest()
        {
            var path = Path.Combine(m_testDir, "sub", "dir", "settings.json");
            var provider = new JsonSettingsProvider(path);

            provider.Write("General", new[]
            {
                new SettingsEntry { Key = "Name", Value = "admin", ValueKind = "String" }
            });

            Assert.That(File.Exists(path), Is.True);
        }

        #endregion

        #region GetGroups Tests

        [Test]
        public void GetGroupsReturnsAllGroupNamesTest()
        {
            var path = WriteJsonFile(@"{
  ""General"": [
    { ""key"": ""Name"", ""value"": ""admin"", ""valueKind"": ""String"" }
  ],
  ""Advanced"": [
    { ""key"": ""Timeout"", ""value"": ""30"", ""valueKind"": ""Integer"" }
  ]
}");

            var provider = new JsonSettingsProvider(path);
            var groups = provider.GetGroups();

            Assert.That(groups, Has.Count.EqualTo(2));
            Assert.That(groups, Does.Contain("General"));
            Assert.That(groups, Does.Contain("Advanced"));
        }

        [Test]
        public void GetGroupsReturnsEmptyForMissingFileTest()
        {
            var path = Path.Combine(m_testDir, "nonexistent.json");
            var provider = new JsonSettingsProvider(path);

            Assert.That(provider.GetGroups(), Is.Empty);
        }

        #endregion

        #region Properties Tests

        [Test]
        public void ConstructorSetsPropertiesTest()
        {
            var provider = new JsonSettingsProvider("/some/path.json", isReadOnly: true);

            Assert.That(provider.FilePath, Is.EqualTo("/some/path.json"));
            Assert.That(provider.IsReadOnly, Is.True);
        }

        [Test]
        public void DefaultIsReadOnlyIsFalseTest()
        {
            var provider = new JsonSettingsProvider("/some/path.json");

            Assert.That(provider.IsReadOnly, Is.False);
        }

        #endregion

        #region Group Metadata Tests

        [Test]
        public void ReadGroupInfoReturnsMetadataFromGroupsSectionTest()
        {
            var path = WriteJsonFile(@"{
  ""__groups__"": {
    ""General"": { ""displayName"": ""General Settings"", ""priority"": 1 },
    ""Advanced"": { ""displayName"": ""Advanced Settings"", ""priority"": 2 }
  },
  ""General"": [
    { ""key"": ""Name"", ""value"": ""admin"", ""valueKind"": ""String"" }
  ]
}");

            var provider = new JsonSettingsProvider(path);
            var infos = provider.ReadGroupInfo();

            Assert.That(infos, Has.Count.EqualTo(2));
            Assert.That(infos[0].Group, Is.EqualTo("General"));
            Assert.That(infos[0].DisplayName, Is.EqualTo("General Settings"));
            Assert.That(infos[0].Priority, Is.EqualTo(1));
            Assert.That(infos[1].Group, Is.EqualTo("Advanced"));
            Assert.That(infos[1].Priority, Is.EqualTo(2));
        }

        [Test]
        public void ReadGroupInfoReturnsEmptyWhenNoGroupsSectionTest()
        {
            var path = WriteJsonFile(@"{
  ""General"": [
    { ""key"": ""Name"", ""value"": ""admin"", ""valueKind"": ""String"" }
  ]
}");

            var provider = new JsonSettingsProvider(path);

            Assert.That(provider.ReadGroupInfo(), Is.Empty);
        }

        [Test]
        public void ReadGroupInfoReturnsEmptyForMissingFileTest()
        {
            var path = Path.Combine(m_testDir, "nonexistent.json");
            var provider = new JsonSettingsProvider(path);

            Assert.That(provider.ReadGroupInfo(), Is.Empty);
        }

        [Test]
        public void WriteGroupInfoCreatesGroupsSectionTest()
        {
            var path = Path.Combine(m_testDir, "groups.json");
            var provider = new JsonSettingsProvider(path);

            provider.Write("General", new[]
            {
                new SettingsEntry { Group = "General", Key = "Name", Value = "admin", ValueKind = "String" }
            });

            provider.WriteGroupInfo(new[]
            {
                new SettingsGroupInfo { Group = "General", DisplayName = "Main", Priority = 1 }
            });

            var readBack = provider.ReadGroupInfo();
            Assert.That(readBack, Has.Count.EqualTo(1));
            Assert.That(readBack[0].Group, Is.EqualTo("General"));
            Assert.That(readBack[0].DisplayName, Is.EqualTo("Main"));
            Assert.That(readBack[0].Priority, Is.EqualTo(1));

            var entries = provider.Read("General");
            Assert.That(entries, Has.Count.EqualTo(1));
            Assert.That(entries[0].Key, Is.EqualTo("Name"));
        }

        [Test]
        public void WriteGroupInfoDoesNothingWhenReadOnlyTest()
        {
            var path = Path.Combine(m_testDir, "readonly.json");
            var provider = new JsonSettingsProvider(path, isReadOnly: true);

            provider.WriteGroupInfo(new[]
            {
                new SettingsGroupInfo { Group = "General", Priority = 1 }
            });

            Assert.That(File.Exists(path), Is.False);
        }

        [Test]
        public void GetGroupsDoesNotIncludeGroupsSectionTest()
        {
            var path = WriteJsonFile(@"{
  ""__groups__"": {
    ""General"": { ""displayName"": ""Main"", ""priority"": 1 }
  },
  ""General"": [
    { ""key"": ""Name"", ""value"": ""admin"", ""valueKind"": ""String"" }
  ]
}");

            var provider = new JsonSettingsProvider(path);
            var groups = provider.GetGroups();

            Assert.That(groups, Has.Count.EqualTo(1));
            Assert.That(groups[0], Is.EqualTo("General"));
        }

        [Test]
        public void GetGroupsReturnsSortedNamesTest()
        {
            var path = WriteJsonFile(@"{
  ""Zebra"": [
    { ""key"": ""Z"", ""value"": ""1"", ""valueKind"": ""String"" }
  ],
  ""Alpha"": [
    { ""key"": ""A"", ""value"": ""2"", ""valueKind"": ""String"" }
  ]
}");

            var provider = new JsonSettingsProvider(path);
            var groups = provider.GetGroups();

            Assert.That(groups[0], Is.EqualTo("Alpha"));
            Assert.That(groups[1], Is.EqualTo("Zebra"));
        }

        #endregion

        #region Helpers

        private string WriteJsonFile(string json)
        {
            var path = Path.Combine(m_testDir, $"{Guid.NewGuid()}.json");
            File.WriteAllText(path, json);
            return path;
        }

        #endregion
    }
}
