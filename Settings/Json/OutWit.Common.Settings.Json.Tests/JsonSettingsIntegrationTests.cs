using System;
using System.IO;
using System.Linq;
using OutWit.Common.Settings.Configuration;
using OutWit.Common.Settings.Json;
using OutWit.Common.Settings.Providers;

namespace OutWit.Common.Settings.Json.Tests
{
    [TestFixture]
    public class JsonSettingsIntegrationTests
    {
        #region Fields

        private string m_testDir = null!;

        #endregion

        #region Setup

        [SetUp]
        public void Setup()
        {
            m_testDir = Path.Combine(Path.GetTempPath(), "OutWit.Settings.Integration", Guid.NewGuid().ToString());
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
        public void LoadCreatesCollectionsFromJsonFileTest()
        {
            WriteFile("defaults.json", @"{
  ""General"": [
    { ""key"": ""UserName"", ""value"": ""admin"", ""valueKind"": ""String"" },
    { ""key"": ""DarkMode"", ""value"": ""True"", ""valueKind"": ""Boolean"" },
    { ""key"": ""MaxRetries"", ""value"": ""5"", ""valueKind"": ""Integer"" }
  ]
}");

            var manager = new SettingsBuilder()
                .UseJsonFile(Path.Combine(m_testDir, "defaults.json"), SettingsScope.Default)
                .Build();

            manager.Load();

            var collection = manager["General"];
            Assert.That(collection, Has.Count.EqualTo(3));

            Assert.That(collection["UserName"].Value, Is.EqualTo("admin"));
            Assert.That(collection["DarkMode"].Value, Is.EqualTo(true));
            Assert.That(collection["MaxRetries"].Value, Is.EqualTo(5));
        }

        [Test]
        public void LoadMultipleGroupsFromJsonFileTest()
        {
            WriteFile("defaults.json", @"{
  ""General"": [
    { ""key"": ""AppName"", ""value"": ""TestApp"", ""valueKind"": ""String"" }
  ],
  ""Advanced"": [
    { ""key"": ""Timeout"", ""value"": ""30"", ""valueKind"": ""Integer"" },
    { ""key"": ""Verbose"", ""value"": ""False"", ""valueKind"": ""Boolean"" }
  ]
}");

            var manager = new SettingsBuilder()
                .UseJsonFile(Path.Combine(m_testDir, "defaults.json"), SettingsScope.Default)
                .Build();

            manager.Load();

            Assert.That(manager.Collections, Has.Count.EqualTo(2));
            Assert.That(manager["General"]["AppName"].Value, Is.EqualTo("TestApp"));
            Assert.That(manager["Advanced"]["Timeout"].Value, Is.EqualTo(30));
            Assert.That(manager["Advanced"]["Verbose"].Value, Is.EqualTo(false));
        }

        [Test]
        public void LoadOverridesDefaultWithUserJsonFileTest()
        {
            WriteFile("defaults.json", @"{
  ""General"": [
    { ""key"": ""UserName"", ""value"": ""admin"", ""valueKind"": ""String"" },
    { ""key"": ""DarkMode"", ""value"": ""False"", ""valueKind"": ""Boolean"" }
  ]
}");

            WriteFile("user.json", @"{
  ""General"": [
    { ""key"": ""UserName"", ""value"": ""john"", ""valueKind"": ""String"" }
  ]
}");

            var manager = new SettingsBuilder()
                .UseJsonFile(Path.Combine(m_testDir, "defaults.json"), SettingsScope.Default)
                .UseJsonFile(Path.Combine(m_testDir, "user.json"), SettingsScope.User)
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
            WriteFile("defaults.json", @"{
  ""General"": [
    { ""key"": ""Mode"", ""value"": ""Tuesday"", ""valueKind"": ""Enum"", ""tag"": ""System.DayOfWeek, System.Runtime"" }
  ]
}");

            var manager = new SettingsBuilder()
                .UseJsonFile(Path.Combine(m_testDir, "defaults.json"), SettingsScope.Default)
                .Build();

            manager.Load();

            Assert.That(manager["General"]["Mode"].Value, Is.EqualTo(DayOfWeek.Tuesday));
            Assert.That(manager["General"]["Mode"].Tag, Is.EqualTo("System.DayOfWeek, System.Runtime"));
        }

        [Test]
        public void LoadHandlesHiddenSettingsTest()
        {
            WriteFile("defaults.json", @"{
  ""General"": [
    { ""key"": ""ApiKey"", ""value"": ""secret123"", ""valueKind"": ""Password"", ""hidden"": true },
    { ""key"": ""Name"", ""value"": ""admin"", ""valueKind"": ""String"" }
  ]
}");

            var manager = new SettingsBuilder()
                .UseJsonFile(Path.Combine(m_testDir, "defaults.json"), SettingsScope.Default)
                .Build();

            manager.Load();

            Assert.That(manager["General"]["ApiKey"].Hidden, Is.True);
            Assert.That(manager["General"]["Name"].Hidden, Is.False);
        }

        [Test]
        public void LoadHandlesAllBuiltInTypesTest()
        {
            WriteFile("defaults.json", @"{
  ""Types"": [
    { ""key"": ""S"", ""value"": ""hello"", ""valueKind"": ""String"" },
    { ""key"": ""I"", ""value"": ""42"", ""valueKind"": ""Integer"" },
    { ""key"": ""L"", ""value"": ""9999999999"", ""valueKind"": ""Long"" },
    { ""key"": ""D"", ""value"": ""3.14"", ""valueKind"": ""Double"" },
    { ""key"": ""Dec"", ""value"": ""99.99"", ""valueKind"": ""Decimal"" },
    { ""key"": ""B"", ""value"": ""True"", ""valueKind"": ""Boolean"" },
    { ""key"": ""TS"", ""value"": ""01:30:00"", ""valueKind"": ""TimeSpan"" },
    { ""key"": ""G"", ""value"": ""d3b07384-d113-4ec6-a7dc-e38b0d171b01"", ""valueKind"": ""Guid"" },
    { ""key"": ""DT"", ""value"": ""2025-06-15T10:30:00.0000000Z"", ""valueKind"": ""DateTime"" },
    { ""key"": ""SL"", ""value"": ""a, b, c"", ""valueKind"": ""StringList"" },
    { ""key"": ""IL"", ""value"": ""1, 2, 3"", ""valueKind"": ""IntegerList"" },
    { ""key"": ""DL"", ""value"": ""1.1, 2.2"", ""valueKind"": ""DoubleList"" }
  ]
}");

            var manager = new SettingsBuilder()
                .UseJsonFile(Path.Combine(m_testDir, "defaults.json"), SettingsScope.Default)
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
        public void SaveWritesModifiedValuesToUserJsonFileTest()
        {
            WriteFile("defaults.json", @"{
  ""General"": [
    { ""key"": ""UserName"", ""value"": ""admin"", ""valueKind"": ""String"" },
    { ""key"": ""DarkMode"", ""value"": ""False"", ""valueKind"": ""Boolean"" }
  ]
}");

            var userPath = Path.Combine(m_testDir, "user.json");

            var manager = new SettingsBuilder()
                .UseJsonFile(Path.Combine(m_testDir, "defaults.json"), SettingsScope.Default)
                .UseJsonFile(userPath, SettingsScope.User)
                .Build();

            manager.Load();

            manager["General"]["UserName"].Value = "john";
            manager["General"]["DarkMode"].Value = true;

            manager.Save();

            Assert.That(File.Exists(userPath), Is.True);

            var readBack = new JsonSettingsProvider(userPath);
            var entries = readBack.Read("General");
            Assert.That(entries, Has.Count.EqualTo(2));
            Assert.That(entries.First(e => e.Key == "UserName").Value, Is.EqualTo("john"));
            Assert.That(entries.First(e => e.Key == "DarkMode").Value, Is.EqualTo("True"));
        }

        [Test]
        public void SavePreservesTagAndHiddenInUserFileTest()
        {
            WriteFile("defaults.json", @"{
  ""General"": [
    { ""key"": ""Mode"", ""value"": ""Monday"", ""valueKind"": ""Enum"", ""tag"": ""System.DayOfWeek, System.Runtime"" },
    { ""key"": ""ApiKey"", ""value"": ""secret"", ""valueKind"": ""Password"", ""hidden"": true }
  ]
}");

            var userPath = Path.Combine(m_testDir, "user.json");

            var manager = new SettingsBuilder()
                .UseJsonFile(Path.Combine(m_testDir, "defaults.json"), SettingsScope.Default)
                .UseJsonFile(userPath, SettingsScope.User)
                .Build();

            manager.Load();
            manager.Save();

            var json = File.ReadAllText(userPath);
            Assert.That(json, Does.Contain("\"tag\""));
            Assert.That(json, Does.Contain("System.DayOfWeek, System.Runtime"));
            Assert.That(json, Does.Contain("\"hidden\": true"));
        }

        [Test]
        public void SaveDoesNotCreateFileWhenDefaultScopeOnlyTest()
        {
            WriteFile("defaults.json", @"{
  ""General"": [
    { ""key"": ""Name"", ""value"": ""admin"", ""valueKind"": ""String"" }
  ]
}");

            var manager = new SettingsBuilder()
                .UseJsonFile(Path.Combine(m_testDir, "defaults.json"), SettingsScope.Default)
                .Build();

            manager.Load();
            manager.Save();

            var files = Directory.GetFiles(m_testDir, "*.json");
            Assert.That(files, Has.Length.EqualTo(1));
        }

        #endregion

        #region Full Round-Trip Tests

        [Test]
        public void FullRoundTripLoadModifySaveReloadTest()
        {
            WriteFile("defaults.json", @"{
  ""General"": [
    { ""key"": ""UserName"", ""value"": ""admin"", ""valueKind"": ""String"" },
    { ""key"": ""DarkMode"", ""value"": ""False"", ""valueKind"": ""Boolean"" },
    { ""key"": ""Port"", ""value"": ""8080"", ""valueKind"": ""Integer"" }
  ]
}");

            var defaultPath = Path.Combine(m_testDir, "defaults.json");
            var userPath = Path.Combine(m_testDir, "user.json");

            // Load and modify
            var manager1 = new SettingsBuilder()
                .UseJsonFile(defaultPath, SettingsScope.Default)
                .UseJsonFile(userPath, SettingsScope.User)
                .Build();

            manager1.Load();
            manager1["General"]["UserName"].Value = "john";
            manager1["General"]["Port"].Value = 9090;
            manager1.Save();

            // Reload with a fresh manager
            var manager2 = new SettingsBuilder()
                .UseJsonFile(defaultPath, SettingsScope.Default)
                .UseJsonFile(userPath, SettingsScope.User)
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
            WriteFile("defaults.json", @"{
  ""General"": [
    { ""key"": ""Name"", ""value"": ""app"", ""valueKind"": ""String"" }
  ],
  ""Advanced"": [
    { ""key"": ""Timeout"", ""value"": ""30"", ""valueKind"": ""Integer"" },
    { ""key"": ""Verbose"", ""value"": ""False"", ""valueKind"": ""Boolean"" }
  ]
}");

            var defaultPath = Path.Combine(m_testDir, "defaults.json");
            var userPath = Path.Combine(m_testDir, "user.json");

            var manager1 = new SettingsBuilder()
                .UseJsonFile(defaultPath, SettingsScope.Default)
                .UseJsonFile(userPath, SettingsScope.User)
                .Build();

            manager1.Load();
            manager1["General"]["Name"].Value = "myapp";
            manager1["Advanced"]["Timeout"].Value = 60;
            manager1.Save();

            var manager2 = new SettingsBuilder()
                .UseJsonFile(defaultPath, SettingsScope.Default)
                .UseJsonFile(userPath, SettingsScope.User)
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
            WriteFile("defaults.json", @"{
  ""General"": [
    { ""key"": ""Day"", ""value"": ""Monday"", ""valueKind"": ""Enum"", ""tag"": ""System.DayOfWeek, System.Runtime"" }
  ]
}");

            var defaultPath = Path.Combine(m_testDir, "defaults.json");
            var userPath = Path.Combine(m_testDir, "user.json");

            var manager1 = new SettingsBuilder()
                .UseJsonFile(defaultPath, SettingsScope.Default)
                .UseJsonFile(userPath, SettingsScope.User)
                .Build();

            manager1.Load();
            manager1["General"]["Day"].Value = DayOfWeek.Friday;
            manager1.Save();

            var manager2 = new SettingsBuilder()
                .UseJsonFile(defaultPath, SettingsScope.Default)
                .UseJsonFile(userPath, SettingsScope.User)
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
            WriteFile("defaults.json", @"{
  ""General"": [
    { ""key"": ""Name"", ""value"": ""admin"", ""valueKind"": ""String"" },
    { ""key"": ""Theme"", ""value"": ""Light"", ""valueKind"": ""String"" }
  ]
}");

            WriteFile("user.json", @"{
  ""General"": [
    { ""key"": ""Name"", ""value"": ""john"", ""valueKind"": ""String"" }
  ]
}");

            var defaultPath = Path.Combine(m_testDir, "defaults.json");
            var userPath = Path.Combine(m_testDir, "user.json");

            var manager = new SettingsBuilder()
                .UseJsonFile(defaultPath, SettingsScope.Default)
                .UseJsonFile(userPath, SettingsScope.User)
                .Build();

            manager.Merge();

            var readBack = new JsonSettingsProvider(userPath);
            var entries = readBack.Read("General");

            Assert.That(entries, Has.Count.EqualTo(2));
            Assert.That(entries.First(e => e.Key == "Name").Value, Is.EqualTo("john"));
            Assert.That(entries.First(e => e.Key == "Theme").Value, Is.EqualTo("Light"));
        }

        [Test]
        public void MergeRemovesObsoleteSettingsFromUserFileTest()
        {
            WriteFile("defaults.json", @"{
  ""General"": [
    { ""key"": ""Name"", ""value"": ""admin"", ""valueKind"": ""String"" }
  ]
}");

            WriteFile("user.json", @"{
  ""General"": [
    { ""key"": ""Name"", ""value"": ""john"", ""valueKind"": ""String"" },
    { ""key"": ""OldSetting"", ""value"": ""obsolete"", ""valueKind"": ""String"" }
  ]
}");

            var defaultPath = Path.Combine(m_testDir, "defaults.json");
            var userPath = Path.Combine(m_testDir, "user.json");

            var manager = new SettingsBuilder()
                .UseJsonFile(defaultPath, SettingsScope.Default)
                .UseJsonFile(userPath, SettingsScope.User)
                .Build();

            manager.Merge();

            var readBack = new JsonSettingsProvider(userPath);
            var entries = readBack.Read("General");

            Assert.That(entries, Has.Count.EqualTo(1));
            Assert.That(entries[0].Key, Is.EqualTo("Name"));
            Assert.That(entries[0].Value, Is.EqualTo("john"));
        }

        [Test]
        public void MergeAddsNewGroupToUserFileTest()
        {
            WriteFile("defaults.json", @"{
  ""General"": [
    { ""key"": ""Name"", ""value"": ""admin"", ""valueKind"": ""String"" }
  ],
  ""Advanced"": [
    { ""key"": ""Timeout"", ""value"": ""30"", ""valueKind"": ""Integer"" }
  ]
}");

            WriteFile("user.json", @"{
  ""General"": [
    { ""key"": ""Name"", ""value"": ""john"", ""valueKind"": ""String"" }
  ]
}");

            var defaultPath = Path.Combine(m_testDir, "defaults.json");
            var userPath = Path.Combine(m_testDir, "user.json");

            var manager = new SettingsBuilder()
                .UseJsonFile(defaultPath, SettingsScope.Default)
                .UseJsonFile(userPath, SettingsScope.User)
                .Build();

            manager.Merge();

            var readBack = new JsonSettingsProvider(userPath);
            var groups = readBack.GetGroups();

            Assert.That(groups, Has.Count.EqualTo(2));
            Assert.That(groups, Does.Contain("General"));
            Assert.That(groups, Does.Contain("Advanced"));

            var advanced = readBack.Read("Advanced");
            Assert.That(advanced, Has.Count.EqualTo(1));
            Assert.That(advanced[0].Key, Is.EqualTo("Timeout"));
            Assert.That(advanced[0].Value, Is.EqualTo("30"));
        }

        [Test]
        public void MergeThenLoadProducesCorrectValuesTest()
        {
            WriteFile("defaults.json", @"{
  ""General"": [
    { ""key"": ""Name"", ""value"": ""admin"", ""valueKind"": ""String"" },
    { ""key"": ""NewSetting"", ""value"": ""default"", ""valueKind"": ""String"" }
  ]
}");

            WriteFile("user.json", @"{
  ""General"": [
    { ""key"": ""Name"", ""value"": ""john"", ""valueKind"": ""String"" },
    { ""key"": ""OldSetting"", ""value"": ""obsolete"", ""valueKind"": ""String"" }
  ]
}");

            var defaultPath = Path.Combine(m_testDir, "defaults.json");
            var userPath = Path.Combine(m_testDir, "user.json");

            var manager = new SettingsBuilder()
                .UseJsonFile(defaultPath, SettingsScope.Default)
                .UseJsonFile(userPath, SettingsScope.User)
                .Build();

            manager.Merge();
            manager.Load();

            Assert.That(manager["General"]["Name"].Value, Is.EqualTo("john"));
            Assert.That(manager["General"]["Name"].IsDefault, Is.False);

            Assert.That(manager["General"]["NewSetting"].Value, Is.EqualTo("default"));
            Assert.That(manager["General"]["NewSetting"].IsDefault, Is.True);
        }

        [Test]
        public void MergePreservesMetadataFromDefaultTest()
        {
            WriteFile("defaults.json", @"{
  ""General"": [
    { ""key"": ""Mode"", ""value"": ""Monday"", ""valueKind"": ""Enum"", ""tag"": ""System.DayOfWeek, System.Runtime"", ""hidden"": true }
  ]
}");

            WriteFile("user.json", @"{
  ""General"": [
    { ""key"": ""Mode"", ""value"": ""Friday"", ""valueKind"": ""String"" }
  ]
}");

            var defaultPath = Path.Combine(m_testDir, "defaults.json");
            var userPath = Path.Combine(m_testDir, "user.json");

            var manager = new SettingsBuilder()
                .UseJsonFile(defaultPath, SettingsScope.Default)
                .UseJsonFile(userPath, SettingsScope.User)
                .Build();

            manager.Merge();

            var readBack = new JsonSettingsProvider(userPath);
            var entries = readBack.Read("General");

            Assert.That(entries[0].ValueKind, Is.EqualTo("Enum"));
            Assert.That(entries[0].Tag, Is.EqualTo("System.DayOfWeek, System.Runtime"));
            Assert.That(entries[0].Hidden, Is.True);
            Assert.That(entries[0].Value, Is.EqualTo("Friday"));
        }

        #endregion

        #region Three-Scope Tests

        [Test]
        public void ThreeScopeResolutionTest()
        {
            WriteFile("defaults.json", @"{
  ""General"": [
    { ""key"": ""A"", ""value"": ""default_a"", ""valueKind"": ""String"" },
    { ""key"": ""B"", ""value"": ""default_b"", ""valueKind"": ""String"" },
    { ""key"": ""C"", ""value"": ""default_c"", ""valueKind"": ""String"" }
  ]
}");

            WriteFile("global.json", @"{
  ""General"": [
    { ""key"": ""A"", ""value"": ""global_a"", ""valueKind"": ""String"" },
    { ""key"": ""B"", ""value"": ""global_b"", ""valueKind"": ""String"" }
  ]
}");

            WriteFile("user.json", @"{
  ""General"": [
    { ""key"": ""A"", ""value"": ""user_a"", ""valueKind"": ""String"" }
  ]
}");

            var manager = new SettingsBuilder()
                .UseJsonFile(Path.Combine(m_testDir, "defaults.json"), SettingsScope.Default)
                .AddProvider(SettingsScope.Global, new JsonSettingsProvider(Path.Combine(m_testDir, "global.json")))
                .UseJsonFile(Path.Combine(m_testDir, "user.json"), SettingsScope.User)
                .Build();

            manager.Load();

            // A: user overrides global and default
            Assert.That(manager["General"]["A"].Value, Is.EqualTo("user_a"));
            // B: global overrides default (no user override)
            Assert.That(manager["General"]["B"].Value, Is.EqualTo("global_b"));
            // C: no overrides, falls back to default
            Assert.That(manager["General"]["C"].Value, Is.EqualTo("default_c"));
            Assert.That(manager["General"]["C"].IsDefault, Is.True);
        }

        #endregion

        #region Edge Cases

        [Test]
        public void LoadWithMissingUserFileUsesDefaultsTest()
        {
            WriteFile("defaults.json", @"{
  ""General"": [
    { ""key"": ""Name"", ""value"": ""admin"", ""valueKind"": ""String"" }
  ]
}");

            var manager = new SettingsBuilder()
                .UseJsonFile(Path.Combine(m_testDir, "defaults.json"), SettingsScope.Default)
                .UseJsonFile(Path.Combine(m_testDir, "user.json"), SettingsScope.User)
                .Build();

            manager.Load();

            Assert.That(manager["General"]["Name"].Value, Is.EqualTo("admin"));
            Assert.That(manager["General"]["Name"].IsDefault, Is.True);
        }

        [Test]
        public void SaveCreatesUserFileInSubdirectoryTest()
        {
            WriteFile("defaults.json", @"{
  ""General"": [
    { ""key"": ""Name"", ""value"": ""admin"", ""valueKind"": ""String"" }
  ]
}");

            var userPath = Path.Combine(m_testDir, "sub", "dir", "user.json");

            var manager = new SettingsBuilder()
                .UseJsonFile(Path.Combine(m_testDir, "defaults.json"), SettingsScope.Default)
                .UseJsonFile(userPath, SettingsScope.User)
                .Build();

            manager.Load();
            manager["General"]["Name"].Value = "john";
            manager.Save();

            Assert.That(File.Exists(userPath), Is.True);

            var readBack = new JsonSettingsProvider(userPath);
            var entries = readBack.Read("General");
            Assert.That(entries.First(e => e.Key == "Name").Value, Is.EqualTo("john"));
        }

        [Test]
        public void MergeWithEmptyUserFileCreatesSchemaTest()
        {
            WriteFile("defaults.json", @"{
  ""General"": [
    { ""key"": ""Name"", ""value"": ""admin"", ""valueKind"": ""String"" },
    { ""key"": ""Port"", ""value"": ""8080"", ""valueKind"": ""Integer"" }
  ]
}");

            var userPath = Path.Combine(m_testDir, "user.json");

            var manager = new SettingsBuilder()
                .UseJsonFile(Path.Combine(m_testDir, "defaults.json"), SettingsScope.Default)
                .UseJsonFile(userPath, SettingsScope.User)
                .Build();

            manager.Merge();

            Assert.That(File.Exists(userPath), Is.True);

            var readBack = new JsonSettingsProvider(userPath);
            var entries = readBack.Read("General");
            Assert.That(entries, Has.Count.EqualTo(2));
            Assert.That(entries.First(e => e.Key == "Name").Value, Is.EqualTo("admin"));
            Assert.That(entries.First(e => e.Key == "Port").Value, Is.EqualTo("8080"));
        }

        [Test]
        public void UnknownValueKindIsSkippedDuringLoadTest()
        {
            WriteFile("defaults.json", @"{
  ""General"": [
    { ""key"": ""Name"", ""value"": ""admin"", ""valueKind"": ""String"" },
    { ""key"": ""Custom"", ""value"": ""data"", ""valueKind"": ""UnknownType"" }
  ]
}");

            var manager = new SettingsBuilder()
                .UseJsonFile(Path.Combine(m_testDir, "defaults.json"), SettingsScope.Default)
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
            WriteFile("defaults.json", @"{
  ""__groups__"": {
    ""General"": { ""displayName"": ""Main"", ""priority"": 1 },
    ""Advanced"": { ""displayName"": ""Extra"", ""priority"": 2 }
  },
  ""General"": [
    { ""key"": ""Name"", ""value"": ""admin"", ""valueKind"": ""String"" }
  ],
  ""Advanced"": [
    { ""key"": ""Timeout"", ""value"": ""30"", ""valueKind"": ""Integer"" }
  ]
}");

            var userPath = Path.Combine(m_testDir, "user.json");

            var manager1 = new SettingsBuilder()
                .UseJsonFile(Path.Combine(m_testDir, "defaults.json"), SettingsScope.Default)
                .UseJsonFile(userPath, SettingsScope.User)
                .Build();

            manager1.Load();

            Assert.That(manager1["General"].DisplayName, Is.EqualTo("Main"));
            Assert.That(manager1["General"].Priority, Is.EqualTo(1));

            manager1["General"].Priority = 10;
            manager1.Save();

            var provider = new JsonSettingsProvider(userPath);
            var infos = provider.ReadGroupInfo();
            Assert.That(infos.First(g => g.Group == "General").Priority, Is.EqualTo(10));
        }

        [Test]
        public void BackwardCompatibilityWithoutGroupsSectionTest()
        {
            WriteFile("defaults.json", @"{
  ""General"": [
    { ""key"": ""Name"", ""value"": ""admin"", ""valueKind"": ""String"" }
  ]
}");

            var manager = new SettingsBuilder()
                .UseJsonFile(Path.Combine(m_testDir, "defaults.json"), SettingsScope.Default)
                .Build();

            manager.Load();

            Assert.That(manager["General"].DisplayName, Is.EqualTo("General"));
            Assert.That(manager["General"].Priority, Is.EqualTo(0));
            Assert.That(manager["General"]["Name"].Value, Is.EqualTo("admin"));
        }

        [Test]
        public void ConfigureGroupOverridesJsonMetadataTest()
        {
            WriteFile("defaults.json", @"{
  ""__groups__"": {
    ""General"": { ""displayName"": ""From File"", ""priority"": 5 }
  },
  ""General"": [
    { ""key"": ""Name"", ""value"": ""admin"", ""valueKind"": ""String"" }
  ]
}");

            var manager = new SettingsBuilder()
                .UseJsonFile(Path.Combine(m_testDir, "defaults.json"), SettingsScope.Default)
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
            WriteFile("defaults.json", @"{
  ""__groups__"": {
    ""General"": { ""displayName"": ""Main"", ""priority"": 1 }
  },
  ""General"": [
    { ""key"": ""Name"", ""value"": ""admin"", ""valueKind"": ""String"" }
  ]
}");

            WriteFile("user.json", @"{
  ""__groups__"": {
    ""General"": { ""displayName"": ""User Main"", ""priority"": 5 },
    ""Legacy"": { ""displayName"": ""Old"", ""priority"": 10 }
  },
  ""General"": [
    { ""key"": ""Name"", ""value"": ""john"", ""valueKind"": ""String"" }
  ],
  ""Legacy"": [
    { ""key"": ""Old"", ""value"": ""obsolete"", ""valueKind"": ""String"" }
  ]
}");

            var defaultPath = Path.Combine(m_testDir, "defaults.json");
            var userPath = Path.Combine(m_testDir, "user.json");

            var manager = new SettingsBuilder()
                .UseJsonFile(defaultPath, SettingsScope.Default)
                .UseJsonFile(userPath, SettingsScope.User)
                .Build();

            manager.Merge();

            var userProvider = new JsonSettingsProvider(userPath);
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

        private void WriteFile(string name, string content)
        {
            File.WriteAllText(Path.Combine(m_testDir, name), content);
        }

        #endregion
    }
}
