using Microsoft.Extensions.Logging;
using NUnit.Framework.Legacy;
using System.Collections.Specialized;
using System.ComponentModel;

namespace OutWit.Common.Logging.Tests
{
    [TestFixture]
    public class SimpleLoggerTests
    {
        private SimpleLogger m_logger;

        [SetUp]
        public void Setup()
        {
            m_logger = new SimpleLogger("TestLogger", LogLevel.Information);
        }

        [Test]
        public void IsEnabledShouldReturnTrueForLogLevelAtOrAboveMinimum()
        {
            // Assert
            Assert.That(m_logger.IsEnabled(LogLevel.Information), Is.True);
            Assert.That(m_logger.IsEnabled(LogLevel.Warning), Is.True);
        }


        [Test]
        public void IsEnabledShouldReturnFalseForLogLevelBelowMinimum()
        {
            // Assert
            Assert.That(m_logger.IsEnabled(LogLevel.Debug), Is.False);
            Assert.That(m_logger.IsEnabled(LogLevel.Trace), Is.False);
        }


        [Test]
        public void LogShouldAddFormattedMessageToCollection()
        {
            // Act
            m_logger.LogInformation("This is a test message.");

            // Assert
            Assert.That(m_logger.Count(), Is.EqualTo(1));
            // The formatter adds a date and event ID, so we check for the main content.
            Assert.That(m_logger.First(), Does.Contain("info: TestLogger[0] This is a test message."));
        }


        [Test]
        public void LogShouldRaiseCollectionChangedEvent()
        {
            // Arrange
            NotifyCollectionChangedEventArgs receivedArgs = null;
            m_logger.CollectionChanged += (sender, args) => { receivedArgs = args; };

            // Act
            m_logger.LogWarning("Event test.");

            // Assert
            Assert.That(receivedArgs, Is.Not.Null);
            Assert.That(receivedArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Add));
        }


        [Test]
        public void MinimumLevelSetShouldRaisePropertyChangedEvent()
        {
            // Arrange
            PropertyChangedEventArgs receivedArgs = null;
            m_logger.PropertyChanged += (sender, args) => { receivedArgs = args; };

            // Act
            m_logger.MinimumLevel = LogLevel.Debug;

            // Assert
            Assert.That(receivedArgs, Is.Not.Null);
            Assert.That(receivedArgs.PropertyName, Is.EqualTo(nameof(SimpleLogger.MinimumLevel)));
        }
    }
}