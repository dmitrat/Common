using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OutWit.Common.Logging.Utils;

namespace OutWit.Common.Logging.Tests
{
    [TestFixture]
    public class LogUtilsTests
    {
        private Mock<ILogger> m_mockLogger;

        [SetUp]
        public void Setup()
        {
            m_mockLogger = new Mock<ILogger>();
            // Enable all log levels for the mock to ensure Log() calls go through
            m_mockLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        }

        [Test]
        public void MeasureShouldLogDurationOnSuccess()
        {
            // Arrange
            var action = new Action(() => { });

            // Act
            m_mockLogger.Object.Measure("TestOperation", action, LogLevel.Information);

            // Assert
            // Verify that the Log method was called with Information level and a duration message.
            m_mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("TestOperation duration:")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }


        [Test]
        public void MeasureShouldLogErrorOnException()
        {
            // Arrange
            var exception = new InvalidOperationException("Failure");
            var action = new Action(() => throw exception);

            // Act
            // The Measure method catches the exception, so we don't expect it to be re-thrown.
            m_mockLogger.Object.Measure("FailingOperation", action);

            // Assert
            // Verify that LogError was called with the correct exception.
            m_mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("FailingOperation failed")),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }


        [Test]
        public void LogShouldCallCorrectLoggerMethod()
        {
            // Arrange
            var message = "Formatted message {0}";
            var args = new object[] { 1 };

            // Act
            LogUtils.Log(m_mockLogger.Object, LogLevel.Warning, message, args);

            // Assert
            // Verify that LogWarning was called with the formatted message.
            // The LogUtils.Log method uses string.Format, so we check the final string.
            m_mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString() == "Formatted message 1"),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }

}
