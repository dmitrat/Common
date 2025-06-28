using OutWit.Common.CommandLine.Tests.Mock;

namespace OutWit.Common.CommandLine.Tests
{
    [TestFixture]
    public class SerializationUtilsTests
    {
        #region Serialization Tests

        [Test]
        public void SerializeCommandLineWithAllPropertiesSetReturnsCorrectStringTest()
        {
            // Arrange
            var options = new TestOptions
            {
                Name = "John Doe",
                Count = 42,
                Verbose = true,
                Timeout = TimeSpan.FromMinutes(15)
            };

            // Act
            string result = options.SerializeCommandLine();

            // Assert
            Assert.That(result, Does.Contain("--name \"John Doe\"")); // NUnit constraint for substring
            Assert.That(result, Does.Contain("--count 42"));
            Assert.That(result, Does.Contain("--verbose"));
            Assert.That(result, Does.Contain("--timeout 00:15:00"));
            Assert.That(result, Does.Not.Contain("IgnoredProperty"));
        }

        [Test]
        public void SerializeCommandLineWithFalseBooleanExcludesFlagTest()
        {
            // Arrange
            var options = new TestOptions
            {
                Name = "Test",
                Verbose = false // Flag is disabled
            };

            // Act
            string result = options.SerializeCommandLine();

            // Assert
            Assert.That(result, Does.Not.Contain("--verbose"), "The output should not contain the flag for a false boolean.");
            Assert.That(result, Does.Contain("--name Test"));
        }

        [Test]
        public void SerializeCommandLineWithTrueBooleanIncludesFlagWithoutValueTest()
        {
            // Arrange
            var options = new TestOptions
            {
                Name = "Test",
                Verbose = true // Flag is enabled
            };

            // Act
            string result = options.SerializeCommandLine();

            // Assert
            Assert.That(result, Does.Contain("--verbose"));
            // Ensure the value "True" was not added
            Assert.That(result, Does.Not.Contain("--verbose True"));
        }

        [Test]
        public void SerializeCommandLineWithNullPropertyValueExcludesPropertyTest()
        {
            // Arrange
            var options = new TestOptions
            {
                Name = "Test",
                Timeout = null // This property value is null
            };

            // Act
            string result = options.SerializeCommandLine();

            // Assert
            Assert.That(result, Does.Not.Contain("--timeout"), "The output should not contain the key for a null property.");
        }

        #endregion

        #region Deserialization Tests

        [Test]
        public void DeserializeCommandLineFromArrayWithValidArgumentsPopulatesObjectCorrectlyTest()
        {
            // Arrange
            string[] args = { "--name", "Jane Doe", "--count", "123", "--verbose", "--timeout", "01:30:00" };

            // Act
            var result = args.DeserializeCommandLine<TestOptions>();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo("Jane Doe"));
            Assert.That(result.Count, Is.EqualTo(123));
            Assert.That(result.Verbose, Is.True);
            Assert.That(result.Timeout, Is.EqualTo(TimeSpan.FromHours(1.5)));
        }

        [Test]
        public void DeserializeCommandLineFromStringWithValidArgumentsPopulatesObjectCorrectlyTest()
        {
            // Arrange
            string argString = "--name \"Jane Doe\" --count 123 --verbose";

            // Act
            var result = argString.DeserializeCommandLine<TestOptions>();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo("Jane Doe"));
            Assert.That(result.Count, Is.EqualTo(123));
            Assert.That(result.Verbose, Is.True);
        }

        [Test]
        public void DeserializeCommandLineFromArrayWithMissingRequiredArgumentReturnsNullTest()
        {
            // Arrange
            // The --name argument is required (Required = true) and is missing here.
            string[] args = { "--count", "50" };

            // Act
            var result = args.DeserializeCommandLine<TestOptions>();

            // Assert
            // Since CommandLineParser fails to parse required arguments, its 'Value' property will be null.
            // Our wrapper method should return default(T), which is null for a class.
            Assert.That(result, Is.Null);
        }

        [Test]
        public void DeserializeCommandLineFromArrayWithInvalidArgumentTypeReturnsNullTest()
        {
            // Arrange
            // 'abc' cannot be converted to an integer for the 'Count' property.
            string[] args = { "--name", "Test", "--count", "abc" };

            // Act
            var result = args.DeserializeCommandLine<TestOptions>();

            // Assert
            Assert.That(result, Is.Null, "Deserialization should fail and return null when argument type is incorrect.");
        }

        #endregion
    }
}