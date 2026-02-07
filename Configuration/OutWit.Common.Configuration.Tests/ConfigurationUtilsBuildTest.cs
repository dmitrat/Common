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
    }
}
