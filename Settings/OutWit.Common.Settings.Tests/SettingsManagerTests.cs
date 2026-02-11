using System.Linq;
using OutWit.Common.Settings.Configuration;
using OutWit.Common.Settings.Providers;
using OutWit.Common.Settings.Serialization;
using OutWit.Common.Settings.Tests.Utils;

namespace OutWit.Common.Settings.Tests
{
    [TestFixture]
    public class SettingsManagerTests
    {
        #region Load Tests

        [Test]
        public void LoadCreatesCollectionsFromDefaultProviderTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("General", new SettingsEntry
            {
                Key = "UserName",
                Value = "admin",
                ValueKind = "String"
            });
            defaultProvider.AddEntry("General", new SettingsEntry
            {
                Key = "DarkMode",
                Value = "False",
                ValueKind = "Boolean"
            });

            var manager = new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .Build();

            manager.Load();

            Assert.That(manager.Collections, Has.Count.EqualTo(1));
            Assert.That(manager["General"].Count, Is.EqualTo(2));

            Assert.That(manager["General"]["UserName"].Value, Is.EqualTo("admin"));
            Assert.That(manager["General"]["DarkMode"].Value, Is.EqualTo(false));
        }

        [Test]
        public void LoadOverridesWithUserProviderValuesTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("General", new SettingsEntry
            {
                Key = "UserName",
                Value = "admin",
                ValueKind = "String"
            });
            defaultProvider.AddEntry("General", new SettingsEntry
            {
                Key = "MaxRetries",
                Value = "3",
                ValueKind = "Integer"
            });

            var userProvider = new MemorySettingsProvider();
            userProvider.AddEntry("General", new SettingsEntry
            {
                Key = "UserName",
                Value = "john",
                ValueKind = "String"
            });

            var manager = new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .AddProvider(SettingsScope.User, userProvider)
                .Build();

            manager.Load();

            Assert.That(manager["General"]["UserName"].DefaultValue, Is.EqualTo("admin"));
            Assert.That(manager["General"]["UserName"].Value, Is.EqualTo("john"));
            Assert.That(manager["General"]["MaxRetries"].Value, Is.EqualTo(3));
        }

        [Test]
        public void LoadSetsIsDefaultCorrectlyTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("General", new SettingsEntry
            {
                Key = "Name",
                Value = "admin",
                ValueKind = "String"
            });

            var userProvider = new MemorySettingsProvider();
            userProvider.AddEntry("General", new SettingsEntry
            {
                Key = "Name",
                Value = "admin",
                ValueKind = "String"
            });

            var manager = new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .AddProvider(SettingsScope.User, userProvider)
                .Build();

            manager.Load();

            Assert.That(manager["General"]["Name"].IsDefault, Is.True);
        }

        [Test]
        public void LoadHandlesMultipleGroupsTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("General", new SettingsEntry
            {
                Key = "Name",
                Value = "admin",
                ValueKind = "String"
            });
            defaultProvider.AddEntry("Advanced", new SettingsEntry
            {
                Key = "Timeout",
                Value = "30",
                ValueKind = "Integer"
            });

            var manager = new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .Build();

            manager.Load();

            Assert.That(manager.Collections, Has.Count.EqualTo(2));
            Assert.That(manager["General"]["Name"].Value, Is.EqualTo("admin"));
            Assert.That(manager["Advanced"]["Timeout"].Value, Is.EqualTo(30));
        }

        [Test]
        public void LoadHandlesEnumWithTagTest()
        {
            var tag = typeof(TestEnum).AssemblyQualifiedName!;

            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("General", new SettingsEntry
            {
                Key = "Mode",
                Value = "Alpha",
                ValueKind = "Enum",
                Tag = tag
            });

            var manager = new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .Build();

            manager.Load();

            Assert.That(manager["General"]["Mode"].Value, Is.EqualTo(TestEnum.Alpha));
        }

        #endregion

        #region Save Tests

        [Test]
        public void SaveWritesUserValuesToUserProviderTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("General", new SettingsEntry
            {
                Key = "Name",
                Value = "admin",
                ValueKind = "String"
            });

            var userProvider = new MemorySettingsProvider();

            var manager = new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .AddProvider(SettingsScope.User, userProvider)
                .Build();

            manager.Load();

            manager["General"]["Name"].Value = "john";
            manager.Save();

            var saved = userProvider.Read("General");
            Assert.That(saved, Has.Count.EqualTo(1));
            Assert.That(saved[0].Group, Is.EqualTo("General"));
            Assert.That(saved[0].Key, Is.EqualTo("Name"));
            Assert.That(saved[0].Value, Is.EqualTo("john"));
        }

        [Test]
        public void SaveDoesNothingWhenNoUserProviderTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("General", new SettingsEntry
            {
                Key = "Name",
                Value = "admin",
                ValueKind = "String"
            });

            var manager = new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .Build();

            manager.Load();
            manager["General"]["Name"].Value = "john";

            Assert.DoesNotThrow(() => manager.Save());
        }

        #endregion

        #region Merge Tests

        [Test]
        public void MergeAddsNewKeysToUserProviderTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("General", new SettingsEntry
            {
                Key = "Name",
                Value = "admin",
                ValueKind = "String"
            });
            defaultProvider.AddEntry("General", new SettingsEntry
            {
                Key = "NewSetting",
                Value = "default",
                ValueKind = "String"
            });

            var userProvider = new MemorySettingsProvider();
            userProvider.AddEntry("General", new SettingsEntry
            {
                Key = "Name",
                Value = "john",
                ValueKind = "String"
            });

            var manager = new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .AddProvider(SettingsScope.User, userProvider)
                .Build();

            manager.Merge();

            var merged = userProvider.Read("General");
            Assert.That(merged, Has.Count.EqualTo(2));

            var nameEntry = merged.First(e => e.Key == "Name");
            Assert.That(nameEntry.Value, Is.EqualTo("john"));

            var newEntry = merged.First(e => e.Key == "NewSetting");
            Assert.That(newEntry.Value, Is.EqualTo("default"));
        }

        [Test]
        public void MergeRemovesObsoleteKeysFromUserProviderTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("General", new SettingsEntry
            {
                Key = "Name",
                Value = "admin",
                ValueKind = "String"
            });

            var userProvider = new MemorySettingsProvider();
            userProvider.AddEntry("General", new SettingsEntry
            {
                Key = "Name",
                Value = "john",
                ValueKind = "String"
            });
            userProvider.AddEntry("General", new SettingsEntry
            {
                Key = "Obsolete",
                Value = "old",
                ValueKind = "String"
            });

            var manager = new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .AddProvider(SettingsScope.User, userProvider)
                .Build();

            manager.Merge();

            var merged = userProvider.Read("General");
            Assert.That(merged, Has.Count.EqualTo(1));
            Assert.That(merged[0].Key, Is.EqualTo("Name"));
        }

        #endregion

        #region Builder Tests

        [Test]
        public void BuilderRegistersCustomSerializerTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("General", new SettingsEntry
            {
                Key = "Theme",
                Value = "dark",
                ValueKind = "Theme"
            });

            var manager = new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .AddSerializer(new ThemeSerializer())
                .Build();

            manager.Load();

            Assert.That(manager["General"]["Theme"].Value, Is.EqualTo("DARK"));
        }

        #endregion

        #region Group Metadata Tests

        [Test]
        public void LoadReadsGroupMetadataFromDefaultProviderTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("General", new SettingsEntry
            {
                Key = "Name",
                Value = "admin",
                ValueKind = "String"
            });
            defaultProvider.AddEntry("Advanced", new SettingsEntry
            {
                Key = "Timeout",
                Value = "30",
                ValueKind = "Integer"
            });
            defaultProvider.AddGroupInfo(new SettingsGroupInfo
            {
                Group = "General",
                DisplayName = "General Settings",
                Priority = 1
            });
            defaultProvider.AddGroupInfo(new SettingsGroupInfo
            {
                Group = "Advanced",
                DisplayName = "Advanced Settings",
                Priority = 2
            });

            var manager = new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .Build();

            manager.Load();

            Assert.That(manager["General"].DisplayName, Is.EqualTo("General Settings"));
            Assert.That(manager["General"].Priority, Is.EqualTo(1));
            Assert.That(manager["Advanced"].DisplayName, Is.EqualTo("Advanced Settings"));
            Assert.That(manager["Advanced"].Priority, Is.EqualTo(2));
        }

        [Test]
        public void LoadDefaultsMetadataWhenNoGroupInfoTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("General", new SettingsEntry
            {
                Key = "Name",
                Value = "admin",
                ValueKind = "String"
            });

            var manager = new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .Build();

            manager.Load();

            Assert.That(manager["General"].DisplayName, Is.EqualTo("General"));
            Assert.That(manager["General"].Priority, Is.EqualTo(0));
        }

        [Test]
        public void ConfigureGroupOverridesProviderMetadataTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("General", new SettingsEntry
            {
                Key = "Name",
                Value = "admin",
                ValueKind = "String"
            });
            defaultProvider.AddGroupInfo(new SettingsGroupInfo
            {
                Group = "General",
                DisplayName = "From Provider",
                Priority = 5
            });

            var manager = new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .ConfigureGroup("General", priority: 1, displayName: "Override Name")
                .Build();

            manager.Load();

            Assert.That(manager["General"].DisplayName, Is.EqualTo("Override Name"));
            Assert.That(manager["General"].Priority, Is.EqualTo(1));
        }

        [Test]
        public void SaveWritesGroupMetadataToUserProviderTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("General", new SettingsEntry
            {
                Key = "Name",
                Value = "admin",
                ValueKind = "String"
            });

            var userProvider = new MemorySettingsProvider();

            var manager = new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .AddProvider(SettingsScope.User, userProvider)
                .Build();

            manager.Load();
            manager["General"].Priority = 3;
            manager["General"].DisplayName = "Main Settings";
            manager.Save();

            var savedGroupInfos = userProvider.ReadGroupInfo();
            Assert.That(savedGroupInfos, Has.Count.EqualTo(1));
            Assert.That(savedGroupInfos[0].Group, Is.EqualTo("General"));
            Assert.That(savedGroupInfos[0].Priority, Is.EqualTo(3));
            Assert.That(savedGroupInfos[0].DisplayName, Is.EqualTo("Main Settings"));
        }

        [Test]
        public void CollectionsAreSortedByPriorityTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("Zebra", new SettingsEntry
            {
                Key = "Z",
                Value = "1",
                ValueKind = "Integer"
            });
            defaultProvider.AddEntry("Alpha", new SettingsEntry
            {
                Key = "A",
                Value = "2",
                ValueKind = "Integer"
            });
            defaultProvider.AddGroupInfo(new SettingsGroupInfo
            {
                Group = "Zebra",
                Priority = 1
            });
            defaultProvider.AddGroupInfo(new SettingsGroupInfo
            {
                Group = "Alpha",
                Priority = 2
            });

            var manager = new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .Build();

            manager.Load();

            Assert.That(manager.Collections[0].Group, Is.EqualTo("Zebra"));
            Assert.That(manager.Collections[1].Group, Is.EqualTo("Alpha"));
        }

        #endregion

        #region Container-Driven Scope Tests

        [Test]
        public void ContainerDrivenLoadIgnoresUnregisteredEntriesTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("General", new SettingsEntry { Key = "UserSetting", Value = "a", ValueKind = "String" });
            defaultProvider.AddEntry("General", new SettingsEntry { Key = "GlobalSetting", Value = "b", ValueKind = "String" });
            defaultProvider.AddEntry("General", new SettingsEntry { Key = "DefaultSetting", Value = "c", ValueKind = "String" });
            defaultProvider.AddEntry("General", new SettingsEntry { Key = "UnknownSetting", Value = "d", ValueKind = "String" });

            var manager = new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .RegisterContainer<ScopedTestSettings>()
                .Build();

            manager.Load();

            Assert.That(manager["General"].Count, Is.EqualTo(3));
            Assert.That(manager["General"].ContainsKey("UserSetting"), Is.True);
            Assert.That(manager["General"].ContainsKey("GlobalSetting"), Is.True);
            Assert.That(manager["General"].ContainsKey("DefaultSetting"), Is.True);
            Assert.That(manager["General"].ContainsKey("UnknownSetting"), Is.False);
        }

        [Test]
        public void ContainerDrivenLoadResolvesDefaultScopeTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("General", new SettingsEntry { Key = "UserSetting", Value = "def_u", ValueKind = "String" });
            defaultProvider.AddEntry("General", new SettingsEntry { Key = "GlobalSetting", Value = "def_g", ValueKind = "String" });
            defaultProvider.AddEntry("General", new SettingsEntry { Key = "DefaultSetting", Value = "def_d", ValueKind = "String" });

            var userProvider = new MemorySettingsProvider();
            userProvider.AddEntry("General", new SettingsEntry { Key = "DefaultSetting", Value = "user_d", ValueKind = "String" });

            var manager = new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .AddProvider(SettingsScope.User, userProvider)
                .RegisterContainer<ScopedTestSettings>()
                .Build();

            manager.Load();

            Assert.That(manager["General"]["DefaultSetting"].Value, Is.EqualTo("def_d"));
            Assert.That(manager["General"]["DefaultSetting"].Scope, Is.EqualTo(SettingsScope.Default));
        }

        [Test]
        public void ContainerDrivenLoadResolvesGlobalScopeTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("General", new SettingsEntry { Key = "UserSetting", Value = "def_u", ValueKind = "String" });
            defaultProvider.AddEntry("General", new SettingsEntry { Key = "GlobalSetting", Value = "def_g", ValueKind = "String" });
            defaultProvider.AddEntry("General", new SettingsEntry { Key = "DefaultSetting", Value = "def_d", ValueKind = "String" });

            var globalProvider = new MemorySettingsProvider();
            globalProvider.AddEntry("General", new SettingsEntry { Key = "GlobalSetting", Value = "glob_g", ValueKind = "String" });

            var manager = new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .AddProvider(SettingsScope.Global, globalProvider)
                .RegisterContainer<ScopedTestSettings>()
                .Build();

            manager.Load();

            Assert.That(manager["General"]["GlobalSetting"].Value, Is.EqualTo("glob_g"));
            Assert.That(manager["General"]["GlobalSetting"].Scope, Is.EqualTo(SettingsScope.Global));
        }

        [Test]
        public void ContainerDrivenLoadResolvesUserScopeTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("General", new SettingsEntry { Key = "UserSetting", Value = "def_u", ValueKind = "String" });
            defaultProvider.AddEntry("General", new SettingsEntry { Key = "GlobalSetting", Value = "def_g", ValueKind = "String" });
            defaultProvider.AddEntry("General", new SettingsEntry { Key = "DefaultSetting", Value = "def_d", ValueKind = "String" });

            var userProvider = new MemorySettingsProvider();
            userProvider.AddEntry("General", new SettingsEntry { Key = "UserSetting", Value = "usr_u", ValueKind = "String" });

            var manager = new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .AddProvider(SettingsScope.User, userProvider)
                .RegisterContainer<ScopedTestSettings>()
                .Build();

            manager.Load();

            Assert.That(manager["General"]["UserSetting"].Value, Is.EqualTo("usr_u"));
            Assert.That(manager["General"]["UserSetting"].Scope, Is.EqualTo(SettingsScope.User));
        }

        [Test]
        public void ContainerDrivenSaveRoutesToCorrectProviderTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("General", new SettingsEntry { Key = "UserSetting", Value = "def_u", ValueKind = "String" });
            defaultProvider.AddEntry("General", new SettingsEntry { Key = "GlobalSetting", Value = "def_g", ValueKind = "String" });
            defaultProvider.AddEntry("General", new SettingsEntry { Key = "DefaultSetting", Value = "def_d", ValueKind = "String" });

            var userProvider = new MemorySettingsProvider();
            var globalProvider = new MemorySettingsProvider();

            var manager = new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .AddProvider(SettingsScope.User, userProvider)
                .AddProvider(SettingsScope.Global, globalProvider)
                .RegisterContainer<ScopedTestSettings>()
                .Build();

            manager.Load();

            manager["General"]["UserSetting"].Value = "modified_u";
            manager["General"]["GlobalSetting"].Value = "modified_g";
            manager.Save();

            var userEntries = userProvider.Read("General");
            Assert.That(userEntries.Any(e => e.Key == "UserSetting" && e.Value == "modified_u"), Is.True);
            Assert.That(userEntries.Any(e => e.Key == "GlobalSetting"), Is.False);
            Assert.That(userEntries.Any(e => e.Key == "DefaultSetting"), Is.False);

            var globalEntries = globalProvider.Read("General");
            Assert.That(globalEntries.Any(e => e.Key == "GlobalSetting" && e.Value == "modified_g"), Is.True);
            Assert.That(globalEntries.Any(e => e.Key == "UserSetting"), Is.False);
        }

        [Test]
        public void ContainerDrivenSaveSkipsDefaultScopeTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("General", new SettingsEntry { Key = "UserSetting", Value = "a", ValueKind = "String" });
            defaultProvider.AddEntry("General", new SettingsEntry { Key = "GlobalSetting", Value = "b", ValueKind = "String" });
            defaultProvider.AddEntry("General", new SettingsEntry { Key = "DefaultSetting", Value = "c", ValueKind = "String" });

            var userProvider = new MemorySettingsProvider();

            var manager = new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .AddProvider(SettingsScope.User, userProvider)
                .RegisterContainer<ScopedTestSettings>()
                .Build();

            manager.Load();
            manager.Save();

            var userEntries = userProvider.Read("General");
            Assert.That(userEntries.Any(e => e.Key == "DefaultSetting"), Is.False);
        }

        #endregion

        #region Scope-Aware Merge Tests

        [Test]
        public void MergeDeletesProviderWhenNoSettingsForScopeTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("General", new SettingsEntry { Key = "UserSetting", Value = "a", ValueKind = "String" });
            defaultProvider.AddEntry("General", new SettingsEntry { Key = "DefaultSetting", Value = "c", ValueKind = "String" });

            var globalProvider = new MemorySettingsProvider();
            globalProvider.AddEntry("General", new SettingsEntry { Key = "OldSetting", Value = "stale", ValueKind = "String" });

            var manager = new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .AddProvider(SettingsScope.Global, globalProvider)
                .RegisterContainer<TestSettings>()
                .Build();

            manager.Merge();

            var globalEntries = globalProvider.Read("General");
            Assert.That(globalEntries, Has.Count.EqualTo(0));
        }

        [Test]
        public void MergeKeepsProviderWhenSettingsExistForScopeTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("General", new SettingsEntry { Key = "UserSetting", Value = "a", ValueKind = "String" });
            defaultProvider.AddEntry("General", new SettingsEntry { Key = "GlobalSetting", Value = "b", ValueKind = "String" });
            defaultProvider.AddEntry("General", new SettingsEntry { Key = "DefaultSetting", Value = "c", ValueKind = "String" });

            var userProvider = new MemorySettingsProvider();
            var globalProvider = new MemorySettingsProvider();

            var manager = new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .AddProvider(SettingsScope.User, userProvider)
                .AddProvider(SettingsScope.Global, globalProvider)
                .RegisterContainer<ScopedTestSettings>()
                .Build();

            manager.Merge();

            var userEntries = userProvider.Read("General");
            Assert.That(userEntries, Has.Count.EqualTo(1));
            Assert.That(userEntries[0].Key, Is.EqualTo("UserSetting"));

            var globalEntries = globalProvider.Read("General");
            Assert.That(globalEntries, Has.Count.EqualTo(1));
            Assert.That(globalEntries[0].Key, Is.EqualTo("GlobalSetting"));
        }

        #endregion

        #region Nested Types

        private sealed class ThemeSerializer : SettingsSerializerBase<string>
        {
            public override string ValueKind => "Theme";

            public override string Parse(string value, string tag)
            {
                return value.ToUpperInvariant();
            }

            public override string Format(string value)
            {
                return value.ToLowerInvariant();
            }
        }

        #endregion
    }
}
