using System.Collections.Generic;
using System.Linq;
using OutWit.Common.Settings.Configuration;
using OutWit.Common.Settings.Providers;
using OutWit.Common.Settings.Tests.Utils;

namespace OutWit.Common.Settings.Tests
{
    [TestFixture]
    public class SettingsMergerTests
    {
        #region Merge Tests

        [Test]
        public void MergePreservesExistingUserValuesTest()
        {
            var defaultProvider = CreateProvider(
                ("General", "Name", "admin", "String"),
                ("General", "Mode", "light", "String"));

            var userProvider = CreateProvider(
                ("General", "Name", "john", "String"),
                ("General", "Mode", "dark", "String"));

            SettingsMerger.Merge(defaultProvider, userProvider);

            var result = userProvider.Read("General");
            Assert.That(result.First(e => e.Key == "Name").Value, Is.EqualTo("john"));
            Assert.That(result.First(e => e.Key == "Mode").Value, Is.EqualTo("dark"));
        }

        [Test]
        public void MergeAddsNewSettingsWithDefaultValuesTest()
        {
            var defaultProvider = CreateProvider(
                ("General", "Name", "admin", "String"),
                ("General", "NewFeature", "enabled", "String"));

            var userProvider = CreateProvider(
                ("General", "Name", "john", "String"));

            SettingsMerger.Merge(defaultProvider, userProvider);

            var result = userProvider.Read("General");
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result.First(e => e.Key == "NewFeature").Value, Is.EqualTo("enabled"));
        }

        [Test]
        public void MergeRemovesDeletedSettingsTest()
        {
            var defaultProvider = CreateProvider(
                ("General", "Name", "admin", "String"));

            var userProvider = CreateProvider(
                ("General", "Name", "john", "String"),
                ("General", "Obsolete", "old", "String"));

            SettingsMerger.Merge(defaultProvider, userProvider);

            var result = userProvider.Read("General");
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].Key, Is.EqualTo("Name"));
        }

        [Test]
        public void MergeUpdatesMetadataFromDefaultTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("General", new SettingsEntry
            {
                Key = "Setting",
                Value = "default",
                ValueKind = "String",
                Hidden = true
            });

            var userProvider = new MemorySettingsProvider();
            userProvider.AddEntry("General", new SettingsEntry
            {
                Key = "Setting",
                Value = "custom",
                ValueKind = "Integer",
                Hidden = false
            });

            SettingsMerger.Merge(defaultProvider, userProvider);

            var result = userProvider.Read("General");
            Assert.That(result[0].Value, Is.EqualTo("custom"));
            Assert.That(result[0].ValueKind, Is.EqualTo("String"));
            Assert.That(result[0].Hidden, Is.True);
        }

        [Test]
        public void MergeHandlesMultipleGroupsTest()
        {
            var defaultProvider = CreateProvider(
                ("General", "A", "1", "String"),
                ("Advanced", "B", "2", "String"));

            var userProvider = CreateProvider(
                ("General", "A", "10", "String"));

            SettingsMerger.Merge(defaultProvider, userProvider);

            var general = userProvider.Read("General");
            Assert.That(general, Has.Count.EqualTo(1));
            Assert.That(general[0].Value, Is.EqualTo("10"));

            var advanced = userProvider.Read("Advanced");
            Assert.That(advanced, Has.Count.EqualTo(1));
            Assert.That(advanced[0].Value, Is.EqualTo("2"));
        }

        [Test]
        public void MergeSkipsReadOnlyTargetTest()
        {
            var defaultProvider = CreateProvider(
                ("General", "A", "1", "String"));

            var readOnlyTarget = new MemorySettingsProvider(isReadOnly: true);

            SettingsMerger.Merge(defaultProvider, readOnlyTarget);

            Assert.That(readOnlyTarget.Read("General"), Is.Empty);
        }

        #endregion

        #region Stale Group Tests

        [Test]
        public void MergeRemovesStaleGroupsTest()
        {
            var defaultProvider = CreateProvider(true,
                ("General", "Name", "admin", "String"));

            var userProvider = new MemorySettingsProvider();
            userProvider.AddEntry("General", new SettingsEntry
            {
                Key = "Name",
                Value = "john",
                ValueKind = "String"
            });
            userProvider.AddEntry("Legacy", new SettingsEntry
            {
                Key = "Old",
                Value = "obsolete",
                ValueKind = "String"
            });

            SettingsMerger.Merge(defaultProvider, userProvider);

            Assert.That(userProvider.GetGroups(), Has.Count.EqualTo(1));
            Assert.That(userProvider.GetGroups()[0], Is.EqualTo("General"));
            Assert.That(userProvider.Read("Legacy"), Is.Empty);
        }

        [Test]
        public void MergeRemovesMultipleStaleGroupsTest()
        {
            var defaultProvider = CreateProvider(true,
                ("General", "Name", "admin", "String"));

            var userProvider = new MemorySettingsProvider();
            userProvider.AddEntry("General", new SettingsEntry
            {
                Key = "Name",
                Value = "john",
                ValueKind = "String"
            });
            userProvider.AddEntry("Legacy", new SettingsEntry
            {
                Key = "Old",
                Value = "1",
                ValueKind = "String"
            });
            userProvider.AddEntry("Removed", new SettingsEntry
            {
                Key = "Gone",
                Value = "2",
                ValueKind = "String"
            });

            SettingsMerger.Merge(defaultProvider, userProvider);

            Assert.That(userProvider.GetGroups(), Has.Count.EqualTo(1));
            Assert.That(userProvider.Read("Legacy"), Is.Empty);
            Assert.That(userProvider.Read("Removed"), Is.Empty);
        }

        #endregion

        #region Group Metadata Merge Tests

        [Test]
        public void MergeCopiesNewGroupMetadataFromDefaultTest()
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
                DisplayName = "Main",
                Priority = 1
            });

            var userProvider = new MemorySettingsProvider();
            userProvider.AddEntry("General", new SettingsEntry
            {
                Key = "Name",
                Value = "john",
                ValueKind = "String"
            });

            SettingsMerger.Merge(defaultProvider, userProvider);

            var infos = userProvider.ReadGroupInfo();
            Assert.That(infos, Has.Count.EqualTo(1));
            Assert.That(infos[0].Group, Is.EqualTo("General"));
            Assert.That(infos[0].DisplayName, Is.EqualTo("Main"));
            Assert.That(infos[0].Priority, Is.EqualTo(1));
        }

        [Test]
        public void MergePreservesUserMetadataForExistingGroupsTest()
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
                DisplayName = "Default Name",
                Priority = 1
            });

            var userProvider = new MemorySettingsProvider();
            userProvider.AddEntry("General", new SettingsEntry
            {
                Key = "Name",
                Value = "john",
                ValueKind = "String"
            });
            userProvider.AddGroupInfo(new SettingsGroupInfo
            {
                Group = "General",
                DisplayName = "Custom Name",
                Priority = 99
            });

            SettingsMerger.Merge(defaultProvider, userProvider);

            var infos = userProvider.ReadGroupInfo();
            Assert.That(infos, Has.Count.EqualTo(1));
            Assert.That(infos[0].Group, Is.EqualTo("General"));
            Assert.That(infos[0].DisplayName, Is.EqualTo("Custom Name"));
            Assert.That(infos[0].Priority, Is.EqualTo(99));
        }

        [Test]
        public void MergeRemovesStaleGroupMetadataTest()
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
                DisplayName = "Main",
                Priority = 1
            });

            var userProvider = new MemorySettingsProvider();
            userProvider.AddEntry("General", new SettingsEntry
            {
                Key = "Name",
                Value = "john",
                ValueKind = "String"
            });
            userProvider.AddEntry("Legacy", new SettingsEntry
            {
                Key = "Old",
                Value = "1",
                ValueKind = "String"
            });
            userProvider.AddGroupInfo(new SettingsGroupInfo
            {
                Group = "General",
                DisplayName = "Custom Main",
                Priority = 5
            });
            userProvider.AddGroupInfo(new SettingsGroupInfo
            {
                Group = "Legacy",
                DisplayName = "Old Legacy",
                Priority = 10
            });

            SettingsMerger.Merge(defaultProvider, userProvider);

            var infos = userProvider.ReadGroupInfo();
            Assert.That(infos, Has.Count.EqualTo(1));
            Assert.That(infos[0].Group, Is.EqualTo("General"));
            Assert.That(infos[0].DisplayName, Is.EqualTo("Custom Main"));
            Assert.That(infos[0].Priority, Is.EqualTo(5));
        }

        [Test]
        public void MergePreservesUserMetadataWhenDefaultHasNoneTest()
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
            userProvider.AddGroupInfo(new SettingsGroupInfo
            {
                Group = "General",
                DisplayName = "My Custom Name",
                Priority = 42
            });

            SettingsMerger.Merge(defaultProvider, userProvider);

            var infos = userProvider.ReadGroupInfo();
            Assert.That(infos, Has.Count.EqualTo(1));
            Assert.That(infos[0].DisplayName, Is.EqualTo("My Custom Name"));
            Assert.That(infos[0].Priority, Is.EqualTo(42));
        }

        [Test]
        public void MergeNewGroupGetsDefaultMetadataExistingGroupKeepsUserMetadataTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("General", new SettingsEntry
            {
                Key = "Name",
                Value = "admin",
                ValueKind = "String"
            });
            defaultProvider.AddEntry("NewFeatures", new SettingsEntry
            {
                Key = "Beta",
                Value = "false",
                ValueKind = "Boolean"
            });
            defaultProvider.AddGroupInfo(new SettingsGroupInfo
            {
                Group = "General",
                DisplayName = "Default General",
                Priority = 1
            });
            defaultProvider.AddGroupInfo(new SettingsGroupInfo
            {
                Group = "NewFeatures",
                DisplayName = "New in V2",
                Priority = 3
            });

            var userProvider = new MemorySettingsProvider();
            userProvider.AddEntry("General", new SettingsEntry
            {
                Key = "Name",
                Value = "john",
                ValueKind = "String"
            });
            userProvider.AddGroupInfo(new SettingsGroupInfo
            {
                Group = "General",
                DisplayName = "My General",
                Priority = 10
            });

            SettingsMerger.Merge(defaultProvider, userProvider);

            var infos = userProvider.ReadGroupInfo();
            Assert.That(infos, Has.Count.EqualTo(2));

            var general = infos.First(i => i.Group == "General");
            Assert.That(general.DisplayName, Is.EqualTo("My General"));
            Assert.That(general.Priority, Is.EqualTo(10));

            var newFeatures = infos.First(i => i.Group == "NewFeatures");
            Assert.That(newFeatures.DisplayName, Is.EqualTo("New in V2"));
            Assert.That(newFeatures.Priority, Is.EqualTo(3));
        }

        #endregion

        #region Scope Map Tests

        [Test]
        public void MergeWithScopeMapIncludesOnlyMatchingScopeTest()
        {
            var defaultProvider = CreateProvider(true,
                ("General", "UserSetting", "a", "String"),
                ("General", "GlobalSetting", "b", "String"),
                ("General", "DefaultSetting", "c", "String"));

            var scopeMap = new Dictionary<(string, string), SettingsScope>
            {
                { ("General", "UserSetting"), SettingsScope.User },
                { ("General", "GlobalSetting"), SettingsScope.Global },
                { ("General", "DefaultSetting"), SettingsScope.Default }
            };

            var userProvider = new MemorySettingsProvider();

            SettingsMerger.Merge(defaultProvider, userProvider, SettingsScope.User, scopeMap);

            var result = userProvider.Read("General");
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].Key, Is.EqualTo("UserSetting"));
        }

        [Test]
        public void MergeWithScopeMapGlobalTargetTest()
        {
            var defaultProvider = CreateProvider(true,
                ("General", "UserSetting", "a", "String"),
                ("General", "GlobalSetting", "b", "String"),
                ("General", "DefaultSetting", "c", "String"));

            var scopeMap = new Dictionary<(string, string), SettingsScope>
            {
                { ("General", "UserSetting"), SettingsScope.User },
                { ("General", "GlobalSetting"), SettingsScope.Global },
                { ("General", "DefaultSetting"), SettingsScope.Default }
            };

            var globalProvider = new MemorySettingsProvider();

            SettingsMerger.Merge(defaultProvider, globalProvider, SettingsScope.Global, scopeMap);

            var result = globalProvider.Read("General");
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].Key, Is.EqualTo("GlobalSetting"));
        }

        [Test]
        public void MergeWithScopeMapDefaultScopeNeverMergedTest()
        {
            var defaultProvider = CreateProvider(true,
                ("General", "DefaultOnly", "x", "String"));

            var scopeMap = new Dictionary<(string, string), SettingsScope>
            {
                { ("General", "DefaultOnly"), SettingsScope.Default }
            };

            var userProvider = new MemorySettingsProvider();

            SettingsMerger.Merge(defaultProvider, userProvider, SettingsScope.User, scopeMap);

            var result = userProvider.Read("General");
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void MergeWithNullScopeMapIncludesAllEntriesTest()
        {
            var defaultProvider = CreateProvider(true,
                ("General", "A", "1", "String"),
                ("General", "B", "2", "String"));

            var userProvider = new MemorySettingsProvider();

            SettingsMerger.Merge(defaultProvider, userProvider, SettingsScope.User, null);

            var result = userProvider.Read("General");
            Assert.That(result, Has.Count.EqualTo(2));
        }

        #endregion

        #region Helpers

        private static MemorySettingsProvider CreateProvider(
            params (string Group, string Key, string Value, string ValueKind)[] entries)
        {
            var provider = entries.Any(e => IsDefaultGroup(e.Group))
                ? new MemorySettingsProvider(isReadOnly: true)
                : new MemorySettingsProvider();

            foreach (var (group, key, value, valueKind) in entries)
            {
                provider.AddEntry(group, new SettingsEntry
                {
                    Key = key,
                    Value = value,
                    ValueKind = valueKind
                });
            }

            return provider;

            static bool IsDefaultGroup(string _) => false;
        }

        private static MemorySettingsProvider CreateProvider(bool isReadOnly,
            params (string Group, string Key, string Value, string ValueKind)[] entries)
        {
            var provider = new MemorySettingsProvider(isReadOnly);

            foreach (var (group, key, value, valueKind) in entries)
            {
                provider.AddEntry(group, new SettingsEntry
                {
                    Key = key,
                    Value = value,
                    ValueKind = valueKind
                });
            }

            return provider;
        }

        #endregion
    }
}
