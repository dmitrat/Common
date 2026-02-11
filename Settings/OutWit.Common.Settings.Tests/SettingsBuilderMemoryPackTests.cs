using System;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Settings.Configuration;
using OutWit.Common.Settings.Interfaces;
using OutWit.Common.Settings.Providers;
using OutWit.Common.Settings.Tests.Utils;

namespace OutWit.Common.Settings.Tests
{
    [TestFixture]
    public class SettingsBuilderMemoryPackTests
    {
        #region Constants

        private const int BUILT_IN_TYPE_COUNT = 14;

        #endregion

        #region Setup

        [SetUp]
        public void Setup()
        {
            SettingsBuilder.ResetMemoryPackRegistrations();
        }

        #endregion

        #region Accumulation Tests

        [Test]
        public void BuildRegistersBuiltInTypesTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("General", new SettingsEntry
            {
                Key = "UserName", Value = "admin", ValueKind = "String"
            });

            new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .RegisterContainer<TestSettings>()
                .Build();

            Assert.That(SettingsBuilder.MemoryPackRegistrationCount, Is.EqualTo(BUILT_IN_TYPE_COUNT));
        }

        [Test]
        public void MultipleBuildCallsAccumulateCustomTypesTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("General", new SettingsEntry
            {
                Key = "UserName", Value = "admin", ValueKind = "String"
            });

            new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .AddSerializer(new DummySerializer(typeof(DateOnly)))
                .RegisterContainer<TestSettings>()
                .Build();

            Assert.That(SettingsBuilder.MemoryPackRegistrationCount, Is.EqualTo(BUILT_IN_TYPE_COUNT + 1));

            new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .RegisterContainer<TestSettings>()
                .Build();

            Assert.That(SettingsBuilder.MemoryPackRegistrationCount, Is.EqualTo(BUILT_IN_TYPE_COUNT + 1));
        }

        [Test]
        public void BuildOrderDoesNotAffectRegistrationsTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("General", new SettingsEntry
            {
                Key = "UserName", Value = "admin", ValueKind = "String"
            });

            new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .RegisterContainer<TestSettings>()
                .Build();

            new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .AddSerializer(new DummySerializer(typeof(DateOnly)))
                .AddSerializer(new DummySerializer(typeof(TimeOnly)))
                .RegisterContainer<TestSettings>()
                .Build();

            Assert.That(SettingsBuilder.MemoryPackRegistrationCount, Is.EqualTo(BUILT_IN_TYPE_COUNT + 2));
        }

        [Test]
        public void RegisterMemoryPackAccumulatesWithBuildTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("General", new SettingsEntry
            {
                Key = "UserName", Value = "admin", ValueKind = "String"
            });

            new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .AddSerializer(new DummySerializer(typeof(DateOnly)))
                .RegisterContainer<TestSettings>()
                .Build();

            SettingsBuilder.RegisterMemoryPack(b =>
                b.AddSerializer(new DummySerializer(typeof(TimeOnly))));

            Assert.That(SettingsBuilder.MemoryPackRegistrationCount, Is.EqualTo(BUILT_IN_TYPE_COUNT + 2));
        }

        [Test]
        public void DuplicateSerializerTypesAreDeduplicatedTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("General", new SettingsEntry
            {
                Key = "UserName", Value = "admin", ValueKind = "String"
            });

            new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .AddSerializer(new DummySerializer(typeof(DateOnly)))
                .RegisterContainer<TestSettings>()
                .Build();

            new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .AddSerializer(new DummySerializer(typeof(DateOnly)))
                .RegisterContainer<TestSettings>()
                .Build();

            Assert.That(SettingsBuilder.MemoryPackRegistrationCount, Is.EqualTo(BUILT_IN_TYPE_COUNT + 1));
        }

        [Test]
        public void ResetClearsAllRegistrationsTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("General", new SettingsEntry
            {
                Key = "UserName", Value = "admin", ValueKind = "String"
            });

            new SettingsBuilder()
                .AddProvider(SettingsScope.Default, defaultProvider)
                .AddSerializer(new DummySerializer(typeof(DateOnly)))
                .RegisterContainer<TestSettings>()
                .Build();

            Assert.That(SettingsBuilder.MemoryPackRegistrationCount, Is.GreaterThan(0));

            SettingsBuilder.ResetMemoryPackRegistrations();

            Assert.That(SettingsBuilder.MemoryPackRegistrationCount, Is.EqualTo(0));
        }

        #endregion

        #region Concurrency Tests

        [Test]
        public void ConcurrentBuildCallsDoNotCorruptRegistrationsTest()
        {
            var defaultProvider = new MemorySettingsProvider(isReadOnly: true);
            defaultProvider.AddEntry("General", new SettingsEntry
            {
                Key = "UserName", Value = "admin", ValueKind = "String"
            });

            var tasks = new Task[10];
            for (int i = 0; i < tasks.Length; i++)
            {
                int index = i;
                tasks[i] = Task.Run(() =>
                {
                    var builder = new SettingsBuilder()
                        .AddProvider(SettingsScope.Default, defaultProvider)
                        .AddSerializer(new DummySerializer(typeof(DateOnly)))
                        .RegisterContainer<TestSettings>();

                    if (index % 2 == 0)
                        builder.AddSerializer(new DummySerializer(typeof(TimeOnly)));

                    builder.Build();
                });
            }

            Assert.DoesNotThrowAsync(() => Task.WhenAll(tasks));

            Assert.That(SettingsBuilder.MemoryPackRegistrationCount, Is.EqualTo(BUILT_IN_TYPE_COUNT + 2));
        }

        #endregion

        #region Nested Types

        private sealed class DummySerializer : ISettingsSerializer
        {
            public DummySerializer(Type valueType)
            {
                ValueType = valueType;
            }

            public string ValueKind => $"Dummy_{ValueType.Name}";

            public Type ValueType { get; }

            public object Deserialize(string value, string tag) => value;

            public string Serialize(object value) => value?.ToString() ?? "";

            public bool AreEqual(object a, object b) => Equals(a, b);
        }

        #endregion
    }
}
