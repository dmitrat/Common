using System.Reflection;

namespace OutWit.Common.Configuration.Tests
{
    [TestFixture]
    public class ConfigurationUtilsBuildTest
    {
        private string _testDir;
        private Assembly _assembly;

        [SetUp]
        public void Setup()
        {
            // Create a temporary directory for configuration files to avoid conflicts.
            _testDir = Path.Combine(Path.GetTempPath(), $"config_tests_{Path.GetRandomFileName()}");
            Directory.CreateDirectory(_testDir);

            // Mock the assembly location to point to our temporary directory.
            // In a real test scenario with a separate test project,
            // Assembly.GetExecutingAssembly() would point to the test assembly's location.
            // We'll simulate loading configs from this directory.
            _assembly = Assembly.GetExecutingAssembly();

            // NUnit (and other runners) might copy dependencies to a different folder.
            // To make tests reliable, we'll work with the test assembly's actual location.
            var assemblyLocation = Path.GetDirectoryName(new Uri(_assembly.Location).LocalPath);
            if (assemblyLocation != null)
            {
                _testDir = assemblyLocation;
            }
        }

        [TearDown]
        public void Teardown()
        {
            // Clean up created files.
            // In this setup, we avoid deleting from the actual bin directory.
            // If we used the temp dir exclusively, we would use:
            // if (Directory.Exists(_testDir))
            // {
            //     Directory.Delete(_testDir, true);
            // }
        }

        private void CreateConfigFile(string fileName, string content)
        {
            File.WriteAllText(Path.Combine(_testDir, fileName), content);
        }

        [Test]
        public void BuildUsesDefaultFileNameTest()
        {
            // Arrange
            var defaultSettings = @"{ ""Key1"": ""DefaultValue"" }";
            CreateConfigFile("appsettings.json", defaultSettings);

            // Act
            var configuration = ConfigurationUtils.For(_assembly).Build();

            // Assert
            Assert.That(configuration["Key1"], Is.EqualTo("DefaultValue"));
            File.Delete(Path.Combine(_testDir, "appsettings.json"));
        }

        [Test]
        public void BuildMergesEnvironmentConfigurationTest()
        {
            // Arrange
            var defaultSettings = @"{ ""Key1"": ""DefaultValue"", ""Key2"": ""BaseValue"" }";
            var devSettings = @"{ ""Key1"": ""DevOverride"" }";
            CreateConfigFile("appsettings.json", defaultSettings);
            CreateConfigFile("appsettings.Development.json", devSettings);

            // Act
            var configuration = ConfigurationUtils.For(_assembly)
                .WithEnvironment(ConfigurationEnvironment.Development)
                .Build();

            // Assert
            // The value from the environment-specific file should override the base file.
            Assert.That(configuration["Key1"], Is.EqualTo("DevOverride"));
            // The value only in the base file should still exist.
            Assert.That(configuration["Key2"], Is.EqualTo("BaseValue"));

            File.Delete(Path.Combine(_testDir, "appsettings.json"));
            File.Delete(Path.Combine(_testDir, "appsettings.Development.json"));
        }

        [Test]
        public void BuildWithCustomFileNameTest()
        {
            // Arrange
            var customSettings = @"{ ""CustomKey"": ""CustomValue"" }";
            var customProdSettings = @"{ ""CustomKey"": ""ProdValue"" }";
            CreateConfigFile("myconfig.json", customSettings);
            CreateConfigFile("myconfig.Production.json", customProdSettings);

            // Act
            var configuration = ConfigurationUtils.For(_assembly)
                .WithFileName("myconfig")
                .WithEnvironment("Production")
                .Build();

            // Assert
            Assert.That(configuration["CustomKey"], Is.EqualTo("ProdValue"));

            File.Delete(Path.Combine(_testDir, "myconfig.json"));
            File.Delete(Path.Combine(_testDir, "myconfig.Production.json"));
        }

        [Test]
        public void BuildWithNoFilesReturnsEmptyConfigTest()
        {
            // Arrange
            // No config files are created.

            // Act
            var configuration = ConfigurationUtils.For(_assembly)
                .WithFileName("nonexistent")
                .Build();

            // Assert
            Assert.That(configuration.GetChildren(), Is.Empty);
        }

        [Test]
        public void BuildDoesNotWatchConfigurationFilesByDefaultTest()
        {
            // Arrange
            CreateConfigFile("appsettings.json", @"{ ""Key1"": ""Value"" }");
            CreateConfigFile("appsettings.Development.json", @"{ ""Key1"": ""DevValue"" }");

            // Act
            var configuration = ConfigurationUtils.For(_assembly).WithEnvironment("Development").Build();

            // Assert - every JSON provider was built without reloadOnChange, so no
            // FileSystemWatcher (inotify instance on Linux) is pinned by the configuration.
            var sources = JsonSources(configuration);
            Assert.That(sources, Is.Not.Empty);
            Assert.That(sources.Select(source => source.ReloadOnChange), Has.All.False);
        }

        [Test]
        public void WithReloadOnChangeWatchesConfigurationFilesTest()
        {
            // Arrange
            CreateConfigFile("appsettings.json", @"{ ""Key1"": ""Value"" }");

            // Act
            var watching = ConfigurationUtils.For(_assembly).WithReloadOnChange().Build();
            var explicitlyOff = ConfigurationUtils.For(_assembly).WithReloadOnChange(false).Build();

            // Assert
            Assert.That(JsonSources(watching).Select(source => source.ReloadOnChange), Has.All.True);
            Assert.That(JsonSources(explicitlyOff).Select(source => source.ReloadOnChange), Has.All.False);
            Assert.That(watching["Key1"], Is.EqualTo("Value"));
        }

        [Test]
        public void RepeatedBuildsDoNotAccumulateOperatingSystemHandlesTest()
        {
            // A configuration built per request or per unit of work (the database provider
            // plugins did exactly that) must not pin a file watcher each time: on Linux every
            // watcher is an inotify instance, and fs.inotify.max_user_instances (often 128 or
            // 1024) ends the whole process's ability to open DbContexts once reached.
            CreateConfigFile("appsettings.json", @"{ ""Key1"": ""Value"" }");
            var baselineHandles = ProcessHandles.Count();
            if (baselineHandles < 0)
                Assert.Ignore("the platform exposes no handle count");

            // Warm up once so lazily created runtime handles do not count against the loop.
            _ = ConfigurationUtils.For(_assembly).Build()["Key1"];
            var before = ProcessHandles.Count();

            // Keep every configuration alive: a collected watcher would hide the leak.
            var kept = new List<Microsoft.Extensions.Configuration.IConfiguration>();
            for (var i = 0; i < 200; i++)
                kept.Add(ConfigurationUtils.For(_assembly).WithEnvironment("Development").Build());

            var after = ProcessHandles.Count();
            Assert.That(kept, Has.Count.EqualTo(200));
            Assert.That(after - before, Is.LessThan(20),
                $"200 configurations grew the process handle count from {before} to {after}: Build() pins a watcher per call");
        }

        [Test]
        public void ReloadingBuildsPinOneWatcherEachTest()
        {
            // The flip side, and the proof that the handle count above measures what it
            // claims: a reloading configuration holds a file watcher until it is disposed.
            // Kept small on purpose - on Linux each watcher is an inotify instance and the
            // per-user limit can be as low as 128.
            CreateConfigFile("appsettings.json", @"{ ""Key1"": ""Value"" }");
            if (ProcessHandles.Count() < 0)
                Assert.Ignore("the platform exposes no handle count");

            _ = ConfigurationUtils.For(_assembly).WithReloadOnChange().Build()["Key1"];
            var before = ProcessHandles.Count();
            var kept = new List<Microsoft.Extensions.Configuration.IConfigurationRoot>();
            try
            {
                for (var i = 0; i < 40; i++)
                    kept.Add((Microsoft.Extensions.Configuration.IConfigurationRoot)ConfigurationUtils.For(_assembly).WithReloadOnChange().Build());

                var after = ProcessHandles.Count();
                Assert.That(after - before, Is.GreaterThanOrEqualTo(30),
                    $"40 reloading configurations grew the handle count only from {before} to {after}; the watcher measurement is not trustworthy");
            }
            finally
            {
                foreach (var root in kept)
                    (root as IDisposable)?.Dispose();
            }
        }

        private static List<Microsoft.Extensions.Configuration.FileConfigurationSource> JsonSources(Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            return ((Microsoft.Extensions.Configuration.IConfigurationRoot)configuration).Providers
                .OfType<Microsoft.Extensions.Configuration.FileConfigurationProvider>()
                .Select(provider => provider.Source)
                .ToList();
        }
    }
}
