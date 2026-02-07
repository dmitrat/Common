using Microsoft.Extensions.Configuration;
using OutWit.Common.Configuration.Tests.Model;

namespace OutWit.Common.Configuration.Tests
{
    [TestFixture]
    public class ConfigurationUtilsBindTest
    {
        [Test]
        public void BindSettingsBindsSimplePropertiesTest()
        {
            // Arrange
            var configValues = new Dictionary<string, string>
            {
                { "StringValue", "Hello World" },
                { "IntValue", "123" }
            };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configValues)
                .Build();

            // Act
            var settings = configuration.BindSettings<SimpleSettings>();

            // Assert
            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.StringValue, Is.EqualTo("Hello World"));
            Assert.That(settings.IntValue, Is.EqualTo(123));
            // This property does not exist in the config and should retain its default value.
            Assert.That(settings.UnboundValue, Is.True);
        }

        [Test]
        public void BindSettingsBindsComplexPropertiesTest()
        {
            // Arrange
            var configValues = new Dictionary<string, string>
            {
                { "ConnectionStrings:DefaultConnection", "Server=.;Database=Test;Trusted_Connection=True;" }
            };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configValues)
                .Build();

            // Act
            var settings = configuration.BindSettings<ComplexSettings>();

            // Assert
            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.ConnectionStrings, Is.Not.Null);
            Assert.That(settings.ConnectionStrings.DefaultConnection, Is.EqualTo("Server=.;Database=Test;Trusted_Connection=True;"));
        }

        [Test]
        public void BindSettingsUsesConfigSectionAttributeTest()
        {
            // Arrange
            var configValues = new Dictionary<string, string>
            {
                { "Logging:LogLevel", "Debug" },
                { "ConnectionStrings:DefaultConnection", "Server=.;Database=Prod;" }
            };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configValues)
                .Build();

            // Act
            var settings = configuration.BindSettings<AttributedSettings>();

            // Assert
            // The 'LogLevel' property should be bound from the 'Logging:LogLevel' section.
            Assert.That(settings.LogLevel, Is.EqualTo("Debug"));
            // The 'Database' property should be bound from the 'ConnectionStrings' section.
            Assert.That(settings.Database, Is.Not.Null);
            Assert.That(settings.Database.DefaultConnection, Is.EqualTo("Server=.;Database=Prod;"));
        }

        [Test]
        public void BindSettingsIgnoresMissingSectionTest()
        {
            // Arrange
            var configValues = new Dictionary<string, string>
            {
                // Config is empty
            };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configValues)
                .Build();

            // Act
            var settings = configuration.BindSettings<SimpleSettings>();

            // Assert
            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.StringValue, Is.Null);
            Assert.That(settings.IntValue, Is.EqualTo(0));
            Assert.That(settings.UnboundValue, Is.True); // Default value is preserved
        }
    }
}
