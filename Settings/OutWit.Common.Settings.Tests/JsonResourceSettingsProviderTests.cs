using System;
using System.Linq;
using System.Reflection;
using OutWit.Common.Settings.Configuration;
using OutWit.Common.Settings.Json;
using OutWit.Common.Settings.Providers;
using OutWit.Common.Settings.Tests.Utils;

namespace OutWit.Common.Settings.Tests
{
    /// <summary>
    /// Verifies the embedded-resource Default provider parses the same JSON shape as the file
    /// provider and — crucially — feeds <see cref="SettingsMerger"/> correctly so that a
    /// version upgrade (new defaults shipped in resources) adapts existing user settings:
    /// new keys appear, removed keys/groups are dropped, user overrides are preserved.
    /// </summary>
    [TestFixture]
    public class JsonResourceSettingsProviderTests
    {
        #region Constants

        // LogicalName of the embedded Resources/test-defaults.json (see the test csproj).
        private const string DEFAULTS_RESOURCE = "test-defaults.json";

        #endregion

        #region Fields

        private static Assembly TestAssembly => typeof(JsonResourceSettingsProviderTests).Assembly;

        #endregion

        #region Provider Tests

        [Test]
        public void ReadParsesGroupsAndEntriesFromResourceTest()
        {
            var provider = new JsonResourceSettingsProvider(TestAssembly, DEFAULTS_RESOURCE);

            Assert.That(provider.GetGroups(), Is.EqualTo(new[] { "Advanced", "General" }));

            var general = provider.Read("General");
            Assert.That(general.Select(e => e.Key), Is.EquivalentTo(new[] { "Name", "NewFeature" }));
            Assert.That(general.First(e => e.Key == "Name").Value, Is.EqualTo("admin"));
            Assert.That(general.First(e => e.Key == "NewFeature").Value, Is.EqualTo("enabled"));

            var advanced = provider.Read("Advanced");
            Assert.That(advanced[0].Key, Is.EqualTo("Level"));
            Assert.That(advanced[0].Value, Is.EqualTo("5"));
            Assert.That(advanced[0].ValueKind, Is.EqualTo("Integer"));
        }

        [Test]
        public void ReadGroupInfoParsesGroupsSectionTest()
        {
            var provider = new JsonResourceSettingsProvider(TestAssembly, DEFAULTS_RESOURCE);

            var infos = provider.ReadGroupInfo().ToDictionary(g => g.Group);
            Assert.That(infos["General"].DisplayName, Is.EqualTo("Main"));
            Assert.That(infos["General"].Priority, Is.EqualTo(1));
            Assert.That(infos["Advanced"].Priority, Is.EqualTo(2));
        }

        [Test]
        public void IsReadOnlyAndMutationsAreNoOpsTest()
        {
            var provider = new JsonResourceSettingsProvider(TestAssembly, DEFAULTS_RESOURCE);
            Assert.That(provider.IsReadOnly, Is.True);

            provider.Write("General", Array.Empty<SettingsEntry>());
            provider.Delete();

            // Defaults are immutable — still readable after no-op mutations.
            Assert.That(provider.Read("General"), Has.Count.EqualTo(2));
        }

        [Test]
        public void SuffixResourceNameResolvesTest()
        {
            // The full logical name is "test-defaults.json"; a suffix must still resolve.
            var provider = new JsonResourceSettingsProvider(TestAssembly, "defaults.json");
            Assert.That(provider.GetGroups(), Is.Not.Empty);
        }

        [Test]
        public void UnknownResourceThrowsTest()
        {
            Assert.Throws<InvalidOperationException>(() =>
                new JsonResourceSettingsProvider(TestAssembly, "no-such-resource.json"));
        }

        #endregion

        #region Upgrade Merge Test

        [Test]
        public void UpgradeMergeAdaptsUserSettingsToNewResourceDefaultsTest()
        {
            // New version's immutable defaults come from the embedded resource:
            //   General: Name, NewFeature (NewFeature is new)   Advanced: Level (new group)
            //   (OldSetting and the Legacy group no longer exist)
            var defaults = new JsonResourceSettingsProvider(TestAssembly, DEFAULTS_RESOURCE);

            // Existing user store from the previous version.
            var user = new MemorySettingsProvider();
            user.AddEntry("General", new SettingsEntry { Key = "Name", Value = "john", ValueKind = "String" });   // user override
            user.AddEntry("General", new SettingsEntry { Key = "OldSetting", Value = "obsolete", ValueKind = "String" }); // dropped key
            user.AddEntry("Legacy", new SettingsEntry { Key = "Foo", Value = "bar", ValueKind = "String" });       // dropped group

            SettingsMerger.Merge(defaults, user);

            var general = user.Read("General").ToDictionary(e => e.Key);
            Assert.Multiple(() =>
            {
                // user override preserved
                Assert.That(general.ContainsKey("Name"), Is.True);
                Assert.That(general["Name"].Value, Is.EqualTo("john"));
                // new default key added with its default value
                Assert.That(general.ContainsKey("NewFeature"), Is.True);
                Assert.That(general["NewFeature"].Value, Is.EqualTo("enabled"));
                // removed default key dropped from the user store
                Assert.That(general.ContainsKey("OldSetting"), Is.False);

                // new default group added with its default value
                var advanced = user.Read("Advanced").ToDictionary(e => e.Key);
                Assert.That(advanced.ContainsKey("Level"), Is.True);
                Assert.That(advanced["Level"].Value, Is.EqualTo("5"));

                // removed default group cleared
                Assert.That(user.Read("Legacy"), Is.Empty);
                Assert.That(user.GetGroups(), Is.EquivalentTo(new[] { "Advanced", "General" }));
            });
        }

        #endregion
    }
}
