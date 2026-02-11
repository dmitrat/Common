using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using OutWit.Common.Settings.Configuration;
using OutWit.Common.Settings.Interfaces;
using OutWit.Common.Settings.Providers;
using OutWit.Common.Settings.Tests.Utils;

namespace OutWit.Common.Settings.Tests
{
    [TestFixture]
    public class SettingsBuilderTests
    {
        #region AutoCreateScopeProviders Tests

        [Test]
        public void BuildAutoCreatesUserProviderWhenFactorySetTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("General", new SettingsEntry { Key = "UserName", Value = "admin", ValueKind = "String" });
            defaultProvider.AddEntry("General", new SettingsEntry { Key = "DarkMode", Value = "False", ValueKind = "Boolean" });
            defaultProvider.AddEntry("General", new SettingsEntry { Key = "MaxRetries", Value = "3", ValueKind = "Integer" });
            defaultProvider.AddEntry("General", new SettingsEntry { Key = "AppVersion", Value = "1.0", ValueKind = "String" });

            var createdProviders = new Dictionary<string, MemorySettingsProvider>();

            var manager = new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .SetScopeProviderFactory(".json", path =>
                {
                    var provider = new MemorySettingsProvider();
                    createdProviders[path] = provider;
                    return provider;
                })
                .RegisterContainer<TestSettings>()
                .Build();

            Assert.That(createdProviders, Has.Count.EqualTo(1));

            var createdPath = createdProviders.Keys.Single();
            Assert.That(createdPath, Does.EndWith(".json"));

            manager.Merge();
            manager.Load();

            Assert.That(manager["General"]["UserName"].Value, Is.EqualTo("admin"));
        }

        [Test]
        public void BuildAutoCreatesGlobalProviderWhenSettingsExistTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("General", new SettingsEntry { Key = "UserSetting", Value = "a", ValueKind = "String" });
            defaultProvider.AddEntry("General", new SettingsEntry { Key = "GlobalSetting", Value = "b", ValueKind = "String" });
            defaultProvider.AddEntry("General", new SettingsEntry { Key = "DefaultSetting", Value = "c", ValueKind = "String" });

            var createdPaths = new List<string>();

            var manager = new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .SetScopeProviderFactory(".json", path =>
                {
                    createdPaths.Add(path);
                    return new MemorySettingsProvider();
                })
                .RegisterContainer<ScopedTestSettings>()
                .Build();

            Assert.That(createdPaths, Has.Count.EqualTo(2));
        }

        [Test]
        public void BuildSkipsProviderWhenNoSettingsForScopeTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("General", new SettingsEntry { Key = "UserName", Value = "admin", ValueKind = "String" });
            defaultProvider.AddEntry("General", new SettingsEntry { Key = "AppVersion", Value = "1.0", ValueKind = "String" });

            var createdPaths = new List<string>();

            var manager = new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .SetScopeProviderFactory(".json", path =>
                {
                    createdPaths.Add(path);
                    return new MemorySettingsProvider();
                })
                .RegisterContainer<TestSettings>()
                .Build();

            Assert.That(createdPaths, Has.Count.EqualTo(1));
            Assert.That(createdPaths.Any(p => p.Contains("Global")), Is.False);
        }

        [Test]
        public void BuildDoesNotOverrideExplicitProviderTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("General", new SettingsEntry { Key = "UserName", Value = "admin", ValueKind = "String" });
            defaultProvider.AddEntry("General", new SettingsEntry { Key = "AppVersion", Value = "1.0", ValueKind = "String" });

            var explicitUserProvider = new MemorySettingsProvider();
            var createdPaths = new List<string>();

            var manager = new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .AddProvider(SettingsScope.User, explicitUserProvider)
                .SetScopeProviderFactory(".json", path =>
                {
                    createdPaths.Add(path);
                    return new MemorySettingsProvider();
                })
                .RegisterContainer<TestSettings>()
                .Build();

            Assert.That(createdPaths, Has.Count.EqualTo(0));

            manager.Merge();

            var entries = explicitUserProvider.Read("General");
            Assert.That(entries, Has.Count.GreaterThan(0));
        }

        [Test]
        public void BuildDoesNothingWithoutFactoryTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("General", new SettingsEntry { Key = "UserName", Value = "admin", ValueKind = "String" });

            var manager = new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .RegisterContainer<TestSettings>()
                .Build();

            manager.Load();

            Assert.That(manager["General"]["UserName"].Value, Is.EqualTo("admin"));
        }

        [Test]
        public void BuildDoesNothingWithoutContainerTypesTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("General", new SettingsEntry { Key = "UserName", Value = "admin", ValueKind = "String" });

            var createdPaths = new List<string>();

            var manager = new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .SetScopeProviderFactory(".json", path =>
                {
                    createdPaths.Add(path);
                    return new MemorySettingsProvider();
                })
                .Build();

            Assert.That(createdPaths, Has.Count.EqualTo(0));
        }

        #endregion

        #region WithLogger Tests

        [Test]
        public void WithLoggerDoesNotThrowTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("General", new SettingsEntry { Key = "UserName", Value = "admin", ValueKind = "String" });

            var manager = new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .WithLogger(NullLogger.Instance)
                .SetScopeProviderFactory(".json", path => new MemorySettingsProvider())
                .RegisterContainer<TestSettings>()
                .Build();

            Assert.DoesNotThrow(() =>
            {
                manager.Merge();
                manager.Load();
                manager.Save();
            });
        }

        [Test]
        public void WithoutLoggerDoesNotThrowTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("General", new SettingsEntry { Key = "UserName", Value = "admin", ValueKind = "String" });

            var manager = new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .SetScopeProviderFactory(".json", path => new MemorySettingsProvider())
                .RegisterContainer<TestSettings>()
                .Build();

            Assert.DoesNotThrow(() =>
            {
                manager.Merge();
                manager.Load();
                manager.Save();
            });
        }

        #endregion

        #region WithDepth Tests

        [Test]
        public void WithDepthAffectsScopeProviderPathTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("General", new SettingsEntry { Key = "UserName", Value = "admin", ValueKind = "String" });

            var createdPaths = new List<string>();

            new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .WithDepth(2)
                .SetScopeProviderFactory(".json", path =>
                {
                    createdPaths.Add(path);
                    return new MemorySettingsProvider();
                })
                .RegisterContainer<TestSettings>()
                .Build();

            Assert.That(createdPaths, Has.Count.EqualTo(1));

            createdPaths.Clear();

            new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .WithDepth(1)
                .SetScopeProviderFactory(".json", path =>
                {
                    createdPaths.Add(path);
                    return new MemorySettingsProvider();
                })
                .RegisterContainer<TestSettings>()
                .Build();

            Assert.That(createdPaths, Has.Count.EqualTo(1));
        }

        #endregion

        #region WithFileName Tests

        [Test]
        public void WithFileNameOverridesAutoGeneratedNameTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("General", new SettingsEntry { Key = "UserName", Value = "admin", ValueKind = "String" });

            var createdPaths = new List<string>();

            new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .WithFileName("custom-settings")
                .SetScopeProviderFactory(".json", path =>
                {
                    createdPaths.Add(path);
                    return new MemorySettingsProvider();
                })
                .RegisterContainer<TestSettings>()
                .Build();

            Assert.That(createdPaths, Has.Count.EqualTo(1));
            Assert.That(createdPaths[0], Does.EndWith("custom-settings.json"));
        }

        #endregion
    }
}
