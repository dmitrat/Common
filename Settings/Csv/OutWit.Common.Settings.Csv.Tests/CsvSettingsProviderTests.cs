using System;
using System.IO;
using System.Linq;
using OutWit.Common.Settings.Providers;

namespace OutWit.Common.Settings.Csv.Tests
{
    [TestFixture]
    public class CsvSettingsProviderTests
    {
        #region Fields

        private string m_testDir = null!;

        #endregion

        #region Setup

        [SetUp]
        public void Setup()
        {
            m_testDir = Path.Combine(Path.GetTempPath(), "OutWit.Settings.Csv.Tests", Guid.NewGuid().ToString());
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
        public void ReadReturnsEntriesFromCsvFileTest()
        {
            var path = WriteCsvFile(
                "Group,Key,Value,ValueKind,Tag,Hidden",
                "General,UserName,admin,String,,False",
                "General,DarkMode,True,Boolean,,False");

            var provider = new CsvSettingsProvider(path);
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
            var path = WriteCsvFile(
                "Group,Key,Value,ValueKind,Tag,Hidden",
                "General,Name,admin,String,,False");

            var provider = new CsvSettingsProvider(path);

            Assert.That(provider.Read("Advanced"), Is.Empty);
        }

        [Test]
        public void ReadReturnsEmptyForMissingFileTest()
        {
            var path = Path.Combine(m_testDir, "nonexistent.csv");
            var provider = new CsvSettingsProvider(path);

            Assert.That(provider.Read("General"), Is.Empty);
        }

        [Test]
        public void ReadParsesTagAndHiddenTest()
        {
            var path = WriteCsvFile(
                "Group,Key,Value,ValueKind,Tag,Hidden",
                "General,Mode,Alpha,Enum,\"MyApp.TestEnum, MyApp\",True");

            var provider = new CsvSettingsProvider(path);
            var entries = provider.Read("General");

            Assert.That(entries[0].Tag, Is.EqualTo("MyApp.TestEnum, MyApp"));
            Assert.That(entries[0].Hidden, Is.True);
        }

        [Test]
        public void ReadHandlesEmptyTagTest()
        {
            var path = WriteCsvFile(
                "Group,Key,Value,ValueKind,Tag,Hidden",
                "General,Name,admin,String,,False");

            var provider = new CsvSettingsProvider(path);
            var entries = provider.Read("General");

            Assert.That(entries[0].Tag, Is.EqualTo(""));
            Assert.That(entries[0].Hidden, Is.False);
        }

        [Test]
        public void ReadHandlesValueWithCommaTest()
        {
            var path = WriteCsvFile(
                "Group,Key,Value,ValueKind,Tag,Hidden",
                "General,Items,\"a, b, c\",StringList,,False");

            var provider = new CsvSettingsProvider(path);
            var entries = provider.Read("General");

            Assert.That(entries[0].Value, Is.EqualTo("a, b, c"));
        }

        [Test]
        public void ReadHandlesValueWithQuotesTest()
        {
            var path = WriteCsvFile(
                "Group,Key,Value,ValueKind,Tag,Hidden",
                "General,Desc,\"He said \"\"hello\"\"\",String,,False");

            var provider = new CsvSettingsProvider(path);
            var entries = provider.Read("General");

            Assert.That(entries[0].Value, Is.EqualTo("He said \"hello\""));
        }

        [Test]
        public void ReadFiltersOnlyRequestedGroupTest()
        {
            var path = WriteCsvFile(
                "Group,Key,Value,ValueKind,Tag,Hidden",
                "General,Name,admin,String,,False",
                "Advanced,Timeout,30,Integer,,False",
                "General,Port,8080,Integer,,False");

            var provider = new CsvSettingsProvider(path);

            var general = provider.Read("General");
            Assert.That(general, Has.Count.EqualTo(2));
            Assert.That(general[0].Key, Is.EqualTo("Name"));
            Assert.That(general[1].Key, Is.EqualTo("Port"));

            var advanced = provider.Read("Advanced");
            Assert.That(advanced, Has.Count.EqualTo(1));
            Assert.That(advanced[0].Key, Is.EqualTo("Timeout"));
        }

        #endregion

        #region Write Tests

        [Test]
        public void WriteCreatesCsvFileTest()
        {
            var path = Path.Combine(m_testDir, "output.csv");
            var provider = new CsvSettingsProvider(path);

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
            var path = WriteCsvFile(
                "Group,Key,Value,ValueKind,Tag,Hidden",
                "General,Name,admin,String,,False",
                "Advanced,Timeout,30,Integer,,False");

            var provider = new CsvSettingsProvider(path);

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
            var path = Path.Combine(m_testDir, "readonly.csv");
            var provider = new CsvSettingsProvider(path, isReadOnly: true);

            provider.Write("General", new[]
            {
                new SettingsEntry { Key = "Name", Value = "admin", ValueKind = "String" }
            });

            Assert.That(File.Exists(path), Is.False);
        }

        [Test]
        public void WriteEscapesCommasInValuesTest()
        {
            var path = Path.Combine(m_testDir, "escaped.csv");
            var provider = new CsvSettingsProvider(path);

            provider.Write("General", new[]
            {
                new SettingsEntry { Group = "General", Key = "Items", Value = "a, b, c", ValueKind = "StringList" }
            });

            var readBack = provider.Read("General");
            Assert.That(readBack[0].Value, Is.EqualTo("a, b, c"));

            var raw = File.ReadAllText(path);
            Assert.That(raw, Does.Contain("\"a, b, c\""));
        }

        [Test]
        public void WriteEscapesQuotesInValuesTest()
        {
            var path = Path.Combine(m_testDir, "quotes.csv");
            var provider = new CsvSettingsProvider(path);

            provider.Write("General", new[]
            {
                new SettingsEntry { Group = "General", Key = "Desc", Value = "He said \"hello\"", ValueKind = "String" }
            });

            var readBack = provider.Read("General");
            Assert.That(readBack[0].Value, Is.EqualTo("He said \"hello\""));
        }

        [Test]
        public void WriteEscapesTagWithCommaTest()
        {
            var path = Path.Combine(m_testDir, "tag.csv");
            var provider = new CsvSettingsProvider(path);

            provider.Write("General", new[]
            {
                new SettingsEntry { Group = "General", Key = "Mode", Value = "Alpha", ValueKind = "Enum", Tag = "MyApp.TestEnum, MyApp" }
            });

            var readBack = provider.Read("General");
            Assert.That(readBack[0].Tag, Is.EqualTo("MyApp.TestEnum, MyApp"));
        }

        [Test]
        public void WriteIncludesHiddenAsTrueOrFalseTest()
        {
            var path = Path.Combine(m_testDir, "hidden.csv");
            var provider = new CsvSettingsProvider(path);

            provider.Write("General", new[]
            {
                new SettingsEntry { Group = "General", Key = "Secret", Value = "123", ValueKind = "String", Hidden = true },
                new SettingsEntry { Group = "General", Key = "Name", Value = "admin", ValueKind = "String", Hidden = false }
            });

            var raw = File.ReadAllText(path);
            Assert.That(raw, Does.Contain(",True"));
            Assert.That(raw, Does.Contain(",False"));

            var readBack = provider.Read("General");
            Assert.That(readBack[0].Hidden, Is.True);
            Assert.That(readBack[1].Hidden, Is.False);
        }

        [Test]
        public void WriteCreatesDirectoryIfNeededTest()
        {
            var path = Path.Combine(m_testDir, "sub", "dir", "settings.csv");
            var provider = new CsvSettingsProvider(path);

            provider.Write("General", new[]
            {
                new SettingsEntry { Group = "General", Key = "Name", Value = "admin", ValueKind = "String" }
            });

            Assert.That(File.Exists(path), Is.True);
        }

        #endregion

        #region GetGroups Tests

        [Test]
        public void GetGroupsReturnsAllGroupNamesTest()
        {
            var path = WriteCsvFile(
                "Group,Key,Value,ValueKind,Tag,Hidden",
                "General,Name,admin,String,,False",
                "Advanced,Timeout,30,Integer,,False");

            var provider = new CsvSettingsProvider(path);
            var groups = provider.GetGroups();

            Assert.That(groups, Has.Count.EqualTo(2));
            Assert.That(groups, Does.Contain("General"));
            Assert.That(groups, Does.Contain("Advanced"));
        }

        [Test]
        public void GetGroupsReturnsEmptyForMissingFileTest()
        {
            var path = Path.Combine(m_testDir, "nonexistent.csv");
            var provider = new CsvSettingsProvider(path);

            Assert.That(provider.GetGroups(), Is.Empty);
        }

        #endregion

        #region Properties Tests

        [Test]
        public void ConstructorSetsPropertiesTest()
        {
            var provider = new CsvSettingsProvider("/some/path.csv", isReadOnly: true);

            Assert.That(provider.FilePath, Is.EqualTo("/some/path.csv"));
            Assert.That(provider.IsReadOnly, Is.True);
        }

        [Test]
        public void DefaultIsReadOnlyIsFalseTest()
        {
            var provider = new CsvSettingsProvider("/some/path.csv");

            Assert.That(provider.IsReadOnly, Is.False);
        }

        #endregion

        #region Group Metadata Tests

        [Test]
        public void ReadGroupInfoReturnsMetadataFromCompanionFileTest()
        {
            var path = Path.Combine(m_testDir, "settings.csv");
            var groupPath = Path.Combine(m_testDir, "settings.groups.csv");

            File.WriteAllText(path,
                "Group,Key,Value,ValueKind,Tag,Hidden" + Environment.NewLine +
                "General,Name,admin,String,,False" + Environment.NewLine);
            File.WriteAllText(groupPath,
                "Group,DisplayName,Priority" + Environment.NewLine +
                "General,Main,1" + Environment.NewLine +
                "Advanced,Extra,2" + Environment.NewLine);

            var provider = new CsvSettingsProvider(path);
            var infos = provider.ReadGroupInfo();

            Assert.That(infos, Has.Count.EqualTo(2));
            Assert.That(infos[0].Group, Is.EqualTo("General"));
            Assert.That(infos[0].DisplayName, Is.EqualTo("Main"));
            Assert.That(infos[0].Priority, Is.EqualTo(1));
            Assert.That(infos[1].Group, Is.EqualTo("Advanced"));
            Assert.That(infos[1].Priority, Is.EqualTo(2));
        }

        [Test]
        public void ReadGroupInfoReturnsEmptyWhenNoCompanionFileTest()
        {
            var path = WriteCsvFile(
                "Group,Key,Value,ValueKind,Tag,Hidden",
                "General,Name,admin,String,,False");

            var provider = new CsvSettingsProvider(path);

            Assert.That(provider.ReadGroupInfo(), Is.Empty);
        }

        [Test]
        public void WriteGroupInfoCreatesCompanionFileTest()
        {
            var path = Path.Combine(m_testDir, "settings.csv");
            var provider = new CsvSettingsProvider(path);

            provider.WriteGroupInfo(new[]
            {
                new SettingsGroupInfo { Group = "General", DisplayName = "Main", Priority = 1 },
                new SettingsGroupInfo { Group = "Advanced", DisplayName = "Extra", Priority = 2 }
            });

            Assert.That(File.Exists(provider.GroupFilePath), Is.True);

            var readBack = provider.ReadGroupInfo();
            Assert.That(readBack, Has.Count.EqualTo(2));
            Assert.That(readBack[0].Group, Is.EqualTo("General"));
            Assert.That(readBack[0].Priority, Is.EqualTo(1));
        }

        [Test]
        public void WriteGroupInfoDoesNothingWhenReadOnlyTest()
        {
            var path = Path.Combine(m_testDir, "readonly.csv");
            var provider = new CsvSettingsProvider(path, isReadOnly: true);

            provider.WriteGroupInfo(new[]
            {
                new SettingsGroupInfo { Group = "General", Priority = 1 }
            });

            Assert.That(File.Exists(provider.GroupFilePath), Is.False);
        }

        [Test]
        public void GroupFilePathIsCorrectlyComputedTest()
        {
            var provider = new CsvSettingsProvider(Path.Combine(m_testDir, "settings.csv"));
            var expected = Path.Combine(m_testDir, "settings.groups.csv");

            Assert.That(provider.GroupFilePath, Is.EqualTo(expected));
        }

        [Test]
        public void GetGroupsReturnsSortedNamesTest()
        {
            var path = WriteCsvFile(
                "Group,Key,Value,ValueKind,Tag,Hidden",
                "Zebra,Z,1,String,,False",
                "Alpha,A,2,String,,False");

            var provider = new CsvSettingsProvider(path);
            var groups = provider.GetGroups();

            Assert.That(groups[0], Is.EqualTo("Alpha"));
            Assert.That(groups[1], Is.EqualTo("Zebra"));
        }

        #endregion

        #region Helpers

        private string WriteCsvFile(params string[] lines)
        {
            var path = Path.Combine(m_testDir, $"{Guid.NewGuid()}.csv");
            File.WriteAllText(path, string.Join(Environment.NewLine, lines) + Environment.NewLine);
            return path;
        }

        #endregion
    }
}
