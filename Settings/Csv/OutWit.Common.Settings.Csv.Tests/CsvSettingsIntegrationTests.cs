using System;
using System.IO;
using System.Linq;
using OutWit.Common.Settings.Configuration;
using OutWit.Common.Settings.Csv;
using OutWit.Common.Settings.Providers;

namespace OutWit.Common.Settings.Csv.Tests
{
    [TestFixture]
    public class CsvSettingsIntegrationTests
    {
        #region Fields

        private string m_testDir = null!;

        #endregion

        #region Setup

        [SetUp]
        public void Setup()
        {
            m_testDir = Path.Combine(Path.GetTempPath(), "OutWit.Settings.Csv.Integration", Guid.NewGuid().ToString());
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
        public void LoadCreatesCollectionsFromCsvFileTest()
        {
            WriteCsv("defaults.csv",
                "Group,Key,Value,ValueKind,Tag,Hidden",
                "General,UserName,admin,String,,False",
                "General,DarkMode,True,Boolean,,False",
                "General,MaxRetries,5,Integer,,False");

            var manager = new SettingsBuilder()
                .UseCsvFile(Path.Combine(m_testDir, "defaults.csv"), SettingsScope.Default)
                .Build();

            manager.Load();

            var collection = manager["General"];
            Assert.That(collection, Has.Count.EqualTo(3));

            Assert.That(collection["UserName"].Value, Is.EqualTo("admin"));
            Assert.That(collection["DarkMode"].Value, Is.EqualTo(true));
            Assert.That(collection["MaxRetries"].Value, Is.EqualTo(5));
        }

        [Test]
        public void LoadMultipleGroupsFromCsvFileTest()
        {
            WriteCsv("defaults.csv",
                "Group,Key,Value,ValueKind,Tag,Hidden",
                "General,AppName,TestApp,String,,False",
                "Advanced,Timeout,30,Integer,,False",
                "Advanced,Verbose,False,Boolean,,False");

            var manager = new SettingsBuilder()
                .UseCsvFile(Path.Combine(m_testDir, "defaults.csv"), SettingsScope.Default)
                .Build();

            manager.Load();

            Assert.That(manager.Collections, Has.Count.EqualTo(2));
            Assert.That(manager["General"]["AppName"].Value, Is.EqualTo("TestApp"));
            Assert.That(manager["Advanced"]["Timeout"].Value, Is.EqualTo(30));
            Assert.That(manager["Advanced"]["Verbose"].Value, Is.EqualTo(false));
        }

        [Test]
        public void LoadOverridesDefaultWithUserCsvFileTest()
        {
            WriteCsv("defaults.csv",
                "Group,Key,Value,ValueKind,Tag,Hidden",
                "General,UserName,admin,String,,False",
                "General,DarkMode,False,Boolean,,False");

            WriteCsv("user.csv",
                "Group,Key,Value,ValueKind,Tag,Hidden",
                "General,UserName,john,String,,False");

            var manager = new SettingsBuilder()
                .UseCsvFile(Path.Combine(m_testDir, "defaults.csv"), SettingsScope.Default)
                .UseCsvFile(Path.Combine(m_testDir, "user.csv"), SettingsScope.User)
                .Build();

            manager.Load();

            var collection = manager["General"];
            Assert.That(collection["UserName"].Value, Is.EqualTo("john"));
            Assert.That(collection["UserName"].IsDefault, Is.False);
            Assert.That(collection["DarkMode"].Value, Is.EqualTo(false));
            Assert.That(collection["DarkMode"].IsDefault, Is.True);
        }

        [Test]
        public void LoadHandlesEnumWithTagTest()
        {
            WriteCsv("defaults.csv",
                "Group,Key,Value,ValueKind,Tag,Hidden",
                "General,Day,Tuesday,Enum,\"System.DayOfWeek, System.Runtime\",False");

            var manager = new SettingsBuilder()
                .UseCsvFile(Path.Combine(m_testDir, "defaults.csv"), SettingsScope.Default)
                .Build();

            manager.Load();

            Assert.That(manager["General"]["Day"].Value, Is.EqualTo(DayOfWeek.Tuesday));
            Assert.That(manager["General"]["Day"].Tag, Is.EqualTo("System.DayOfWeek, System.Runtime"));
        }

        [Test]
        public void LoadHandlesHiddenSettingsTest()
        {
            WriteCsv("defaults.csv",
                "Group,Key,Value,ValueKind,Tag,Hidden",
                "General,ApiKey,secret123,Password,,True",
                "General,Name,admin,String,,False");

            var manager = new SettingsBuilder()
                .UseCsvFile(Path.Combine(m_testDir, "defaults.csv"), SettingsScope.Default)
                .Build();

            manager.Load();

            Assert.That(manager["General"]["ApiKey"].Hidden, Is.True);
            Assert.That(manager["General"]["Name"].Hidden, Is.False);
        }

        [Test]
        public void LoadHandlesAllBuiltInTypesTest()
        {
            WriteCsv("defaults.csv",
                "Group,Key,Value,ValueKind,Tag,Hidden",
                "Types,S,hello,String,,False",
                "Types,I,42,Integer,,False",
                "Types,L,9999999999,Long,,False",
                "Types,D,3.14,Double,,False",
                "Types,Dec,99.99,Decimal,,False",
                "Types,B,True,Boolean,,False",
                "Types,TS,01:30:00,TimeSpan,,False",
                "Types,G,d3b07384-d113-4ec6-a7dc-e38b0d171b01,Guid,,False",
                "Types,DT,2025-06-15T10:30:00.0000000Z,DateTime,,False",
                "Types,SL,\"a, b, c\",StringList,,False",
                "Types,IL,\"1, 2, 3\",IntegerList,,False",
                "Types,DL,\"1.1, 2.2\",DoubleList,,False");

            var manager = new SettingsBuilder()
                .UseCsvFile(Path.Combine(m_testDir, "defaults.csv"), SettingsScope.Default)
                .Build();

            manager.Load();

            var c = manager["Types"];
            Assert.That(c["S"].Value, Is.EqualTo("hello"));
            Assert.That(c["I"].Value, Is.EqualTo(42));
            Assert.That(c["L"].Value, Is.EqualTo(9999999999L));
            Assert.That(c["D"].Value, Is.EqualTo(3.14));
            Assert.That(c["Dec"].Value, Is.EqualTo(99.99m));
            Assert.That(c["B"].Value, Is.EqualTo(true));
            Assert.That(c["TS"].Value, Is.EqualTo(TimeSpan.FromMinutes(90)));
            Assert.That(c["G"].Value, Is.EqualTo(Guid.Parse("d3b07384-d113-4ec6-a7dc-e38b0d171b01")));
            Assert.That(c["SL"].Value, Is.EquivalentTo(new[] { "a", "b", "c" }));
            Assert.That(c["IL"].Value, Is.EquivalentTo(new[] { 1, 2, 3 }));
            Assert.That(c["DL"].Value, Is.EquivalentTo(new[] { 1.1, 2.2 }));
        }

        #endregion

        #region Save Tests

        [Test]
        public void SaveWritesModifiedValuesToUserCsvFileTest()
        {
            WriteCsv("defaults.csv",
                "Group,Key,Value,ValueKind,Tag,Hidden",
                "General,UserName,admin,String,,False",
                "General,DarkMode,False,Boolean,,False");

            var userPath = Path.Combine(m_testDir, "user.csv");

            var manager = new SettingsBuilder()
                .UseCsvFile(Path.Combine(m_testDir, "defaults.csv"), SettingsScope.Default)
                .UseCsvFile(userPath, SettingsScope.User)
                .Build();

            manager.Load();

            manager["General"]["UserName"].Value = "john";
            manager["General"]["DarkMode"].Value = true;

            manager.Save();

            Assert.That(File.Exists(userPath), Is.True);

            var readBack = new CsvSettingsProvider(userPath);
            var entries = readBack.Read("General");
            Assert.That(entries, Has.Count.EqualTo(2));
            Assert.That(entries.First(e => e.Key == "UserName").Value, Is.EqualTo("john"));
            Assert.That(entries.First(e => e.Key == "DarkMode").Value, Is.EqualTo("True"));
        }

        [Test]
        public void SavePreservesTagAndHiddenInUserFileTest()
        {
            WriteCsv("defaults.csv",
                "Group,Key,Value,ValueKind,Tag,Hidden",
                "General,Mode,Monday,Enum,\"System.DayOfWeek, System.Runtime\",False",
                "General,ApiKey,secret,Password,,True");

            var userPath = Path.Combine(m_testDir, "user.csv");

            var manager = new SettingsBuilder()
                .UseCsvFile(Path.Combine(m_testDir, "defaults.csv"), SettingsScope.Default)
                .UseCsvFile(userPath, SettingsScope.User)
                .Build();

            manager.Load();
            manager.Save();

            var readBack = new CsvSettingsProvider(userPath);
            var entries = readBack.Read("General");

            var mode = entries.First(e => e.Key == "Mode");
            Assert.That(mode.Tag, Is.EqualTo("System.DayOfWeek, System.Runtime"));
            Assert.That(mode.ValueKind, Is.EqualTo("Enum"));

            var apiKey = entries.First(e => e.Key == "ApiKey");
            Assert.That(apiKey.Hidden, Is.True);
        }

        #endregion

        #region Full Round-Trip Tests

        [Test]
        public void FullRoundTripLoadModifySaveReloadTest()
        {
            WriteCsv("defaults.csv",
                "Group,Key,Value,ValueKind,Tag,Hidden",
                "General,UserName,admin,String,,False",
                "General,DarkMode,False,Boolean,,False",
                "General,Port,8080,Integer,,False");

            var defaultPath = Path.Combine(m_testDir, "defaults.csv");
            var userPath = Path.Combine(m_testDir, "user.csv");

            var manager1 = new SettingsBuilder()
                .UseCsvFile(defaultPath, SettingsScope.Default)
                .UseCsvFile(userPath, SettingsScope.User)
                .Build();

            manager1.Load();
            manager1["General"]["UserName"].Value = "john";
            manager1["General"]["Port"].Value = 9090;
            manager1.Save();

            var manager2 = new SettingsBuilder()
                .UseCsvFile(defaultPath, SettingsScope.Default)
                .UseCsvFile(userPath, SettingsScope.User)
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
            WriteCsv("defaults.csv",
                "Group,Key,Value,ValueKind,Tag,Hidden",
                "General,Name,app,String,,False",
                "Advanced,Timeout,30,Integer,,False",
                "Advanced,Verbose,False,Boolean,,False");

            var defaultPath = Path.Combine(m_testDir, "defaults.csv");
            var userPath = Path.Combine(m_testDir, "user.csv");

            var manager1 = new SettingsBuilder()
                .UseCsvFile(defaultPath, SettingsScope.Default)
                .UseCsvFile(userPath, SettingsScope.User)
                .Build();

            manager1.Load();
            manager1["General"]["Name"].Value = "myapp";
            manager1["Advanced"]["Timeout"].Value = 60;
            manager1.Save();

            var manager2 = new SettingsBuilder()
                .UseCsvFile(defaultPath, SettingsScope.Default)
                .UseCsvFile(userPath, SettingsScope.User)
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
            WriteCsv("defaults.csv",
                "Group,Key,Value,ValueKind,Tag,Hidden",
                "General,Day,Monday,Enum,\"System.DayOfWeek, System.Runtime\",False");

            var defaultPath = Path.Combine(m_testDir, "defaults.csv");
            var userPath = Path.Combine(m_testDir, "user.csv");

            var manager1 = new SettingsBuilder()
                .UseCsvFile(defaultPath, SettingsScope.Default)
                .UseCsvFile(userPath, SettingsScope.User)
                .Build();

            manager1.Load();
            manager1["General"]["Day"].Value = DayOfWeek.Friday;
            manager1.Save();

            var manager2 = new SettingsBuilder()
                .UseCsvFile(defaultPath, SettingsScope.Default)
                .UseCsvFile(userPath, SettingsScope.User)
                .Build();

            manager2.Load();

            Assert.That(manager2["General"]["Day"].Value, Is.EqualTo(DayOfWeek.Friday));
            Assert.That(manager2["General"]["Day"].DefaultValue, Is.EqualTo(DayOfWeek.Monday));
            Assert.That(manager2["General"]["Day"].IsDefault, Is.False);
        }

        #endregion

        #region Merge Tests

        [Test]
        public void MergeAddsNewSettingsToUserFileTest()
        {
            WriteCsv("defaults.csv",
                "Group,Key,Value,ValueKind,Tag,Hidden",
                "General,Name,admin,String,,False",
                "General,Theme,Light,String,,False");

            WriteCsv("user.csv",
                "Group,Key,Value,ValueKind,Tag,Hidden",
                "General,Name,john,String,,False");

            var defaultPath = Path.Combine(m_testDir, "defaults.csv");
            var userPath = Path.Combine(m_testDir, "user.csv");

            var manager = new SettingsBuilder()
                .UseCsvFile(defaultPath, SettingsScope.Default)
                .UseCsvFile(userPath, SettingsScope.User)
                .Build();

            manager.Merge();

            var readBack = new CsvSettingsProvider(userPath);
            var entries = readBack.Read("General");

            Assert.That(entries, Has.Count.EqualTo(2));
            Assert.That(entries.First(e => e.Key == "Name").Value, Is.EqualTo("john"));
            Assert.That(entries.First(e => e.Key == "Theme").Value, Is.EqualTo("Light"));
        }

        [Test]
        public void MergeRemovesObsoleteSettingsFromUserFileTest()
        {
            WriteCsv("defaults.csv",
                "Group,Key,Value,ValueKind,Tag,Hidden",
                "General,Name,admin,String,,False");

            WriteCsv("user.csv",
                "Group,Key,Value,ValueKind,Tag,Hidden",
                "General,Name,john,String,,False",
                "General,OldSetting,obsolete,String,,False");

            var defaultPath = Path.Combine(m_testDir, "defaults.csv");
            var userPath = Path.Combine(m_testDir, "user.csv");

            var manager = new SettingsBuilder()
                .UseCsvFile(defaultPath, SettingsScope.Default)
                .UseCsvFile(userPath, SettingsScope.User)
                .Build();

            manager.Merge();

            var readBack = new CsvSettingsProvider(userPath);
            var entries = readBack.Read("General");

            Assert.That(entries, Has.Count.EqualTo(1));
            Assert.That(entries[0].Key, Is.EqualTo("Name"));
            Assert.That(entries[0].Value, Is.EqualTo("john"));
        }

        [Test]
        public void MergeThenLoadProducesCorrectValuesTest()
        {
            WriteCsv("defaults.csv",
                "Group,Key,Value,ValueKind,Tag,Hidden",
                "General,Name,admin,String,,False",
                "General,NewSetting,default,String,,False");

            WriteCsv("user.csv",
                "Group,Key,Value,ValueKind,Tag,Hidden",
                "General,Name,john,String,,False",
                "General,OldSetting,obsolete,String,,False");

            var defaultPath = Path.Combine(m_testDir, "defaults.csv");
            var userPath = Path.Combine(m_testDir, "user.csv");

            var manager = new SettingsBuilder()
                .UseCsvFile(defaultPath, SettingsScope.Default)
                .UseCsvFile(userPath, SettingsScope.User)
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
            WriteCsv("defaults.csv",
                "Group,Key,Value,ValueKind,Tag,Hidden",
                "General,A,default_a,String,,False",
                "General,B,default_b,String,,False",
                "General,C,default_c,String,,False");

            WriteCsv("global.csv",
                "Group,Key,Value,ValueKind,Tag,Hidden",
                "General,A,global_a,String,,False",
                "General,B,global_b,String,,False");

            WriteCsv("user.csv",
                "Group,Key,Value,ValueKind,Tag,Hidden",
                "General,A,user_a,String,,False");

            var manager = new SettingsBuilder()
                .UseCsvFile(Path.Combine(m_testDir, "defaults.csv"), SettingsScope.Default)
                .AddProvider(SettingsScope.Global, new CsvSettingsProvider(Path.Combine(m_testDir, "global.csv")))
                .UseCsvFile(Path.Combine(m_testDir, "user.csv"), SettingsScope.User)
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
        public void LoadWithMissingUserFileUsesDefaultsTest()
        {
            WriteCsv("defaults.csv",
                "Group,Key,Value,ValueKind,Tag,Hidden",
                "General,Name,admin,String,,False");

            var manager = new SettingsBuilder()
                .UseCsvFile(Path.Combine(m_testDir, "defaults.csv"), SettingsScope.Default)
                .UseCsvFile(Path.Combine(m_testDir, "user.csv"), SettingsScope.User)
                .Build();

            manager.Load();

            Assert.That(manager["General"]["Name"].Value, Is.EqualTo("admin"));
            Assert.That(manager["General"]["Name"].IsDefault, Is.True);
        }

        [Test]
        public void SaveCreatesUserFileInSubdirectoryTest()
        {
            WriteCsv("defaults.csv",
                "Group,Key,Value,ValueKind,Tag,Hidden",
                "General,Name,admin,String,,False");

            var userPath = Path.Combine(m_testDir, "sub", "dir", "user.csv");

            var manager = new SettingsBuilder()
                .UseCsvFile(Path.Combine(m_testDir, "defaults.csv"), SettingsScope.Default)
                .UseCsvFile(userPath, SettingsScope.User)
                .Build();

            manager.Load();
            manager["General"]["Name"].Value = "john";
            manager.Save();

            Assert.That(File.Exists(userPath), Is.True);

            var readBack = new CsvSettingsProvider(userPath);
            var entries = readBack.Read("General");
            Assert.That(entries.First(e => e.Key == "Name").Value, Is.EqualTo("john"));
        }

        [Test]
        public void MergeWithEmptyUserFileCreatesSchemaTest()
        {
            WriteCsv("defaults.csv",
                "Group,Key,Value,ValueKind,Tag,Hidden",
                "General,Name,admin,String,,False",
                "General,Port,8080,Integer,,False");

            var userPath = Path.Combine(m_testDir, "user.csv");

            var manager = new SettingsBuilder()
                .UseCsvFile(Path.Combine(m_testDir, "defaults.csv"), SettingsScope.Default)
                .UseCsvFile(userPath, SettingsScope.User)
                .Build();

            manager.Merge();

            Assert.That(File.Exists(userPath), Is.True);

            var readBack = new CsvSettingsProvider(userPath);
            var entries = readBack.Read("General");
            Assert.That(entries, Has.Count.EqualTo(2));
            Assert.That(entries.First(e => e.Key == "Name").Value, Is.EqualTo("admin"));
            Assert.That(entries.First(e => e.Key == "Port").Value, Is.EqualTo("8080"));
        }

        [Test]
        public void UnknownValueKindIsSkippedDuringLoadTest()
        {
            WriteCsv("defaults.csv",
                "Group,Key,Value,ValueKind,Tag,Hidden",
                "General,Name,admin,String,,False",
                "General,Custom,data,UnknownType,,False");

            var manager = new SettingsBuilder()
                .UseCsvFile(Path.Combine(m_testDir, "defaults.csv"), SettingsScope.Default)
                .Build();

            manager.Load();

            var collection = manager["General"];
            Assert.That(collection, Has.Count.EqualTo(1));
            Assert.That(collection["Name"].Value, Is.EqualTo("admin"));
        }

        #endregion

        #region Group Metadata Integration Tests

        [Test]
        public void GroupMetadataRoundTripTest()
        {
            WriteCsv("defaults.csv",
                "Group,Key,Value,ValueKind,Tag,Hidden",
                "General,Name,admin,String,,False",
                "Advanced,Timeout,30,Integer,,False");

            var defaultGroupPath = Path.Combine(m_testDir, "defaults.groups.csv");
            File.WriteAllText(defaultGroupPath,
                "Group,DisplayName,Priority" + Environment.NewLine +
                "General,Main,1" + Environment.NewLine +
                "Advanced,Extra,2" + Environment.NewLine);

            var userPath = Path.Combine(m_testDir, "user.csv");

            var manager1 = new SettingsBuilder()
                .UseCsvFile(Path.Combine(m_testDir, "defaults.csv"), SettingsScope.Default)
                .UseCsvFile(userPath, SettingsScope.User)
                .Build();

            manager1.Load();

            Assert.That(manager1["General"].DisplayName, Is.EqualTo("Main"));
            Assert.That(manager1["General"].Priority, Is.EqualTo(1));

            manager1["General"].Priority = 10;
            manager1.Save();

            var userProvider = new CsvSettingsProvider(userPath);
            var infos = userProvider.ReadGroupInfo();
            Assert.That(infos.First(g => g.Group == "General").Priority, Is.EqualTo(10));
        }

        [Test]
        public void BackwardCompatibilityWithoutCompanionFileTest()
        {
            WriteCsv("defaults.csv",
                "Group,Key,Value,ValueKind,Tag,Hidden",
                "General,Name,admin,String,,False");

            var manager = new SettingsBuilder()
                .UseCsvFile(Path.Combine(m_testDir, "defaults.csv"), SettingsScope.Default)
                .Build();

            manager.Load();

            Assert.That(manager["General"].DisplayName, Is.EqualTo("General"));
            Assert.That(manager["General"].Priority, Is.EqualTo(0));
            Assert.That(manager["General"]["Name"].Value, Is.EqualTo("admin"));
        }

        [Test]
        public void ConfigureGroupOverridesCsvMetadataTest()
        {
            WriteCsv("defaults.csv",
                "Group,Key,Value,ValueKind,Tag,Hidden",
                "General,Name,admin,String,,False");

            var defaultGroupPath = Path.Combine(m_testDir, "defaults.groups.csv");
            File.WriteAllText(defaultGroupPath,
                "Group,DisplayName,Priority" + Environment.NewLine +
                "General,From File,5" + Environment.NewLine);

            var manager = new SettingsBuilder()
                .UseCsvFile(Path.Combine(m_testDir, "defaults.csv"), SettingsScope.Default)
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
            WriteCsv("defaults.csv",
                "Group,Key,Value,ValueKind,Tag,Hidden",
                "General,Name,admin,String,,False");

            var defaultGroupPath = Path.Combine(m_testDir, "defaults.groups.csv");
            File.WriteAllText(defaultGroupPath,
                "Group,DisplayName,Priority" + Environment.NewLine +
                "General,Main,1" + Environment.NewLine);

            WriteCsv("user.csv",
                "Group,Key,Value,ValueKind,Tag,Hidden",
                "General,Name,john,String,,False",
                "Legacy,Old,obsolete,String,,False");

            var userGroupPath = Path.Combine(m_testDir, "user.groups.csv");
            File.WriteAllText(userGroupPath,
                "Group,DisplayName,Priority" + Environment.NewLine +
                "General,User Main,5" + Environment.NewLine +
                "Legacy,Old,10" + Environment.NewLine);

            var manager = new SettingsBuilder()
                .UseCsvFile(Path.Combine(m_testDir, "defaults.csv"), SettingsScope.Default)
                .UseCsvFile(Path.Combine(m_testDir, "user.csv"), SettingsScope.User)
                .Build();

            manager.Merge();

            var userProvider = new CsvSettingsProvider(Path.Combine(m_testDir, "user.csv"));
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

        private void WriteCsv(string name, params string[] lines)
        {
            File.WriteAllText(Path.Combine(m_testDir, name), string.Join(Environment.NewLine, lines) + Environment.NewLine);
        }

        #endregion
    }
}
