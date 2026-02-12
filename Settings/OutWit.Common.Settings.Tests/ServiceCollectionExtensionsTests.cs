using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.Settings.Configuration;
using OutWit.Common.Settings.Interfaces;
using OutWit.Common.Settings.Providers;
using OutWit.Common.Settings.Tests.Utils;

namespace OutWit.Common.Settings.Tests
{
    [TestFixture]
    public class ServiceCollectionExtensionsTests
    {
        #region Registration Tests

        [Test]
        public void AddSettingsRegistersManagerAsSingletonTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("General", new SettingsEntry
            {
                Key = "UserName", Value = "admin", ValueKind = "String"
            });

            var services = new ServiceCollection();

            services.AddSettings(builder => builder
                .AddProvider(SettingsScope.Default, defaultProvider)
                .RegisterContainer<TestSettings>()
            );

            var provider = services.BuildServiceProvider();

            var manager = provider.GetService<ISettingsManager>();
            Assert.That(manager, Is.Not.Null);
        }

        [Test]
        public void AddSettingsRegistersContainerAsSingletonTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("General", new SettingsEntry
            {
                Key = "UserName", Value = "admin", ValueKind = "String"
            });
            defaultProvider.AddEntry("General", new SettingsEntry
            {
                Key = "DarkMode", Value = "False", ValueKind = "Boolean"
            });
            defaultProvider.AddEntry("General", new SettingsEntry
            {
                Key = "MaxRetries", Value = "3", ValueKind = "Integer"
            });
            defaultProvider.AddEntry("General", new SettingsEntry
            {
                Key = "AppVersion", Value = "1.0", ValueKind = "String"
            });

            var services = new ServiceCollection();

            services.AddSettings(builder => builder
                .AddProvider(SettingsScope.Default, defaultProvider)
                .RegisterContainer<TestSettings>()
            );

            var provider = services.BuildServiceProvider();

            var settings = provider.GetService<TestSettings>();
            Assert.That(settings, Is.Not.Null);
            Assert.That(settings!.UserName, Is.EqualTo("admin"));
            Assert.That(settings.DarkMode, Is.False);
            Assert.That(settings.MaxRetries, Is.EqualTo(3));
        }

        [Test]
        public void AddSettingsReturnsSameContainerInstanceTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("General", new SettingsEntry
            {
                Key = "UserName", Value = "admin", ValueKind = "String"
            });

            var services = new ServiceCollection();

            services.AddSettings(builder => builder
                .AddProvider(SettingsScope.Default, defaultProvider)
                .RegisterContainer<TestSettings>()
            );

            var provider = services.BuildServiceProvider();

            var settings1 = provider.GetRequiredService<TestSettings>();
            var settings2 = provider.GetRequiredService<TestSettings>();
            Assert.That(settings1, Is.SameAs(settings2));
        }

        #endregion

        #region Interface Registration Tests

        [Test]
        public void AddSettingsRegistersContainerByInterfaceTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("General", new SettingsEntry
            {
                Key = "UserName", Value = "admin", ValueKind = "String"
            });
            defaultProvider.AddEntry("General", new SettingsEntry
            {
                Key = "DarkMode", Value = "False", ValueKind = "Boolean"
            });
            defaultProvider.AddEntry("General", new SettingsEntry
            {
                Key = "MaxRetries", Value = "3", ValueKind = "Integer"
            });
            defaultProvider.AddEntry("General", new SettingsEntry
            {
                Key = "AppVersion", Value = "1.0", ValueKind = "String"
            });

            var services = new ServiceCollection();

            services.AddSettings(builder => builder
                .AddProvider(SettingsScope.Default, defaultProvider)
                .RegisterContainer<ITestSettings, TestSettings>()
            );

            var provider = services.BuildServiceProvider();

            var settings = provider.GetService<ITestSettings>();
            Assert.That(settings, Is.Not.Null);
            Assert.That(settings!.UserName, Is.EqualTo("admin"));
            Assert.That(settings.MaxRetries, Is.EqualTo(3));
        }

        [Test]
        public void AddSettingsInterfaceRegistrationIsNotResolvedByConcreteTypeTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("General", new SettingsEntry
            {
                Key = "UserName", Value = "admin", ValueKind = "String"
            });

            var services = new ServiceCollection();

            services.AddSettings(builder => builder
                .AddProvider(SettingsScope.Default, defaultProvider)
                .RegisterContainer<ITestSettings, TestSettings>()
            );

            var provider = services.BuildServiceProvider();

            var byInterface = provider.GetService<ITestSettings>();
            var byConcrete = provider.GetService<TestSettings>();
            Assert.That(byInterface, Is.Not.Null);
            Assert.That(byConcrete, Is.Null);
        }

        [Test]
        public void AddSettingsMixedRegistrationTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("General", new SettingsEntry
            {
                Key = "UserName", Value = "admin", ValueKind = "String"
            });
            defaultProvider.AddEntry("General", new SettingsEntry
            {
                Key = "UserSetting", Value = "user_val", ValueKind = "String"
            });
            defaultProvider.AddEntry("General", new SettingsEntry
            {
                Key = "GlobalSetting", Value = "global_val", ValueKind = "String"
            });

            var services = new ServiceCollection();

            services.AddSettings(builder => builder
                .AddProvider(SettingsScope.Default, defaultProvider)
                .RegisterContainer<ITestSettings, TestSettings>()
                .RegisterContainer<ScopedTestSettings>()
            );

            var provider = services.BuildServiceProvider();

            var byInterface = provider.GetRequiredService<ITestSettings>();
            Assert.That(byInterface.UserName, Is.EqualTo("admin"));

            var byConcrete = provider.GetRequiredService<ScopedTestSettings>();
            Assert.That(byConcrete.UserSetting, Is.EqualTo("user_val"));
        }

        #endregion

        #region Merge and Load Tests

        [Test]
        public void AddSettingsAutoLoadsMergedValuesTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("General", new SettingsEntry
            {
                Key = "UserName", Value = "default_user", ValueKind = "String"
            });
            defaultProvider.AddEntry("General", new SettingsEntry
            {
                Key = "MaxRetries", Value = "5", ValueKind = "Integer"
            });

            var userProvider = new MemorySettingsProvider();
            userProvider.AddEntry("General", new SettingsEntry
            {
                Key = "UserName", Value = "custom_user", ValueKind = "String"
            });

            var services = new ServiceCollection();

            services.AddSettings(builder => builder
                .AddProvider(SettingsScope.Default, defaultProvider)
                .AddProvider(SettingsScope.User, userProvider)
                .RegisterContainer<TestSettings>()
            );

            var provider = services.BuildServiceProvider();

            var settings = provider.GetRequiredService<TestSettings>();
            Assert.That(settings.UserName, Is.EqualTo("custom_user"));
            Assert.That(settings.MaxRetries, Is.EqualTo(5));
        }

        [Test]
        public void AddSettingsMultipleCallsRegisterMultipleManagersTest()
        {
            var provider1 = new MemorySettingsProvider(isReadOnly: true);
            provider1.AddEntry("General", new SettingsEntry
            {
                Key = "UserName", Value = "admin", ValueKind = "String"
            });

            var provider2 = new MemorySettingsProvider(isReadOnly: true);
            provider2.AddEntry("General", new SettingsEntry
            {
                Key = "UserName", Value = "other", ValueKind = "String"
            });
            provider2.AddEntry("General", new SettingsEntry
            {
                Key = "GlobalSetting", Value = "global", ValueKind = "String"
            });

            var services = new ServiceCollection();

            services.AddSettings(builder => builder
                .AddProvider(SettingsScope.Default, provider1)
                .RegisterContainer<TestSettings>()
            );

            services.AddSettings(builder => builder
                .AddProvider(SettingsScope.Default, provider2)
                .RegisterContainer<ScopedTestSettings>()
            );

            var sp = services.BuildServiceProvider();

            var managers = sp.GetServices<ISettingsManager>().ToList();
            Assert.That(managers, Has.Count.EqualTo(2));

            var testSettings = sp.GetRequiredService<TestSettings>();
            Assert.That(testSettings.UserName, Is.EqualTo("admin"));

            var scopedSettings = sp.GetRequiredService<ScopedTestSettings>();
            Assert.That(scopedSettings.GlobalSetting, Is.EqualTo("global"));
        }

        #endregion
    }
}
