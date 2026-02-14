using OutWit.Common.Settings.Configuration;
using OutWit.Common.Settings.Providers;
using OutWit.Common.Settings.Tests.Utils;

namespace OutWit.Common.Settings.Tests
{
    [TestFixture]
    public class SettingAspectTests
    {
        #region Getter Tests

        [Test]
        public void AspectGetterReturnsUserValueTest()
        {
            var settings = CreateTestSettings();

            Assert.That(settings.UserName, Is.EqualTo("admin"));
            Assert.That(settings.DarkMode, Is.False);
            Assert.That(settings.MaxRetries, Is.EqualTo(3));
        }

        [Test]
        public void AspectGetterReturnsOverriddenUserValueTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            PopulateDefaults(defaultProvider);

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

            var settings = new TestSettings(manager);

            Assert.That(settings.UserName, Is.EqualTo("john"));
        }

        [Test]
        public void AspectGetterReturnsDefaultValueForDefaultScopeTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            PopulateDefaults(defaultProvider);

            var userProvider = new MemorySettingsProvider();
            userProvider.AddEntry("General", new SettingsEntry
            {
                Key = "AppVersion",
                Value = "2.0",
                ValueKind = "String"
            });

            var manager = new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .AddProvider(SettingsScope.User, userProvider)
                .Build();

            manager.Load();

            var settings = new TestSettings(manager);

            Assert.That(settings.AppVersion, Is.EqualTo("1.0.0"));
        }

        #endregion

        #region Setter Tests

        [Test]
        public void AspectSetterUpdatesUserValueTest()
        {
            var settings = CreateTestSettings();

            settings.UserName = "newuser";

            Assert.That(settings.UserName, Is.EqualTo("newuser"));
            Assert.That(settings.SettingsManager["General"]["UserName"].Value,
                Is.EqualTo("newuser"));
        }

        [Test]
        public void AspectSetterUpdatesIsDefaultTest()
        {
            var settings = CreateTestSettings();

            var value = settings.SettingsManager["General"]["MaxRetries"];
            Assert.That(value.IsDefault, Is.True);

            settings.MaxRetries = 10;
            Assert.That(value.IsDefault, Is.False);

            settings.MaxRetries = 3;
            Assert.That(value.IsDefault, Is.True);
        }

        #endregion

        #region Integration Tests

        [Test]
        public void FullRoundTripLoadModifySaveReloadTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            PopulateDefaults(defaultProvider);

            var userProvider = new MemorySettingsProvider();

            var manager = new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .AddProvider(SettingsScope.User, userProvider)
                .Build();

            manager.Load();
            var settings = new TestSettings(manager);

            settings.UserName = "modified";
            settings.DarkMode = true;

            manager.Save();

            var manager2 = new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .AddProvider(SettingsScope.User, userProvider)
                .Build();

            manager2.Load();
            var settings2 = new TestSettings(manager2);

            Assert.That(settings2.UserName, Is.EqualTo("modified"));
            Assert.That(settings2.DarkMode, Is.True);
            Assert.That(settings2.MaxRetries, Is.EqualTo(3));
        }

        [Test]
        public void MergeAddsNewSettingsAndPreservesExistingTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            PopulateDefaults(defaultProvider);

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

            manager.Merge();
            manager.Load();

            var settings = new TestSettings(manager);

            Assert.That(settings.UserName, Is.EqualTo("john"));
            Assert.That(settings.DarkMode, Is.False);
            Assert.That(settings.MaxRetries, Is.EqualTo(3));
        }

        #endregion

        #region Implicit Group Tests

        [Test]
        public void AspectGetterResolvesImplicitGroupFromClassNameTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("ImplicitGroupSettings", new SettingsEntry
            {
                Key = "UserName",
                Value = "implicit_user",
                ValueKind = "String"
            });
            defaultProvider.AddEntry("ImplicitGroupSettings", new SettingsEntry
            {
                Key = "GlobalValue",
                Value = "global_val",
                ValueKind = "String"
            });
            defaultProvider.AddEntry("ImplicitGroupSettings", new SettingsEntry
            {
                Key = "DefaultValue",
                Value = "default_val",
                ValueKind = "String"
            });

            var manager = new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .RegisterContainer<ImplicitGroupSettings>()
                .Build();

            manager.Load();

            var settings = new ImplicitGroupSettings(manager);

            Assert.That(settings.UserName, Is.EqualTo("implicit_user"));
            Assert.That(settings.DefaultValue, Is.EqualTo("default_val"));
        }

        [Test]
        public void AspectSetterWorksWithImplicitGroupTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("ImplicitGroupSettings", new SettingsEntry
            {
                Key = "UserName",
                Value = "original",
                ValueKind = "String"
            });

            var userProvider = new MemorySettingsProvider();

            var manager = new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .AddProvider(SettingsScope.User, userProvider)
                .RegisterContainer<ImplicitGroupSettings>()
                .Build();

            manager.Merge();
            manager.Load();

            var settings = new ImplicitGroupSettings(manager);
            settings.UserName = "modified";

            Assert.That(settings.UserName, Is.EqualTo("modified"));
            Assert.That(settings.SettingsManager["ImplicitGroupSettings"]["UserName"].Value,
                Is.EqualTo("modified"));
        }

        [Test]
        public void ImplicitGroupMatchesExplicitGroupWithSameNameTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("ImplicitGroupSettings", new SettingsEntry
            {
                Key = "UserName",
                Value = "test_user",
                ValueKind = "String"
            });
            defaultProvider.AddEntry("ImplicitGroupSettings", new SettingsEntry
            {
                Key = "GlobalValue",
                Value = "gv",
                ValueKind = "String"
            });
            defaultProvider.AddEntry("ImplicitGroupSettings", new SettingsEntry
            {
                Key = "DefaultValue",
                Value = "dv",
                ValueKind = "String"
            });

            var manager = new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .RegisterContainer<ImplicitGroupSettings>()
                .Build();

            manager.Load();

            var collection = manager["ImplicitGroupSettings"];
            Assert.That(collection, Is.Not.Null);
            Assert.That(collection.ContainsKey("UserName"), Is.True);
            Assert.That(collection.ContainsKey("GlobalValue"), Is.True);
        }

        #endregion

        #region Helpers

        private static TestSettings CreateTestSettings()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            PopulateDefaults(defaultProvider);

            var manager = new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .Build();

            manager.Load();

            return new TestSettings(manager);
        }

        private static void PopulateDefaults(MemorySettingsProvider provider)
        {
            provider.AddEntry("General", new SettingsEntry
            {
                Key = "UserName",
                Value = "admin",
                ValueKind = "String"
            });
            provider.AddEntry("General", new SettingsEntry
            {
                Key = "DarkMode",
                Value = "False",
                ValueKind = "Boolean"
            });
            provider.AddEntry("General", new SettingsEntry
            {
                Key = "MaxRetries",
                Value = "3",
                ValueKind = "Integer"
            });
            provider.AddEntry("General", new SettingsEntry
            {
                Key = "AppVersion",
                Value = "1.0.0",
                ValueKind = "String"
            });
        }

        #endregion
    }
}
