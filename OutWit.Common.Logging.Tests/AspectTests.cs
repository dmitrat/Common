using OutWit.Common.Logging.Tests.Mock;
using Serilog;
using Serilog.Events;
using System.ComponentModel;
using Serilog.Sinks.TestCorrelator;

namespace OutWit.Common.Logging.Tests
{
    [TestFixture]
    public class AspectTests
    {
        [SetUp]
        public void Setup()
        {
            // Configure the global Serilog logger to write to the TestCorrelator.
            // The aspects use the static Serilog.Log.Logger instance.
            Log.Logger = new LoggerConfiguration()
             .MinimumLevel.Verbose() // Set the minimum level to capture all logs
             .WriteTo.TestCorrelator()
             .CreateLogger();
        }

        [Test]
        public void LogAspectShouldLogMethodExecution()
        {
            // Arrange
            var target = new AspectTestTarget();

            // Act
            using (var context = TestCorrelator.CreateContext())
            {
                target.SimpleMethod(123, "Test");

                // Assert
                var logEvent = TestCorrelator.GetLogEventsFromContextId(context.Id).Single();

                Assert.That(logEvent.Level, Is.EqualTo(LogEventLevel.Information));
                // Test the message template itself
                Assert.That(logEvent.MessageTemplate.Text, Is.EqualTo("Executed method {type}.{name}{parameters}"));

                // Test the structured property values
                Assert.That((logEvent.Properties["type"] as ScalarValue)?.Value, Is.EqualTo("AspectTestTarget"));
                Assert.That((logEvent.Properties["name"] as ScalarValue)?.Value, Is.EqualTo("SimpleMethod"));
                Assert.That((logEvent.Properties["parameters"] as ScalarValue)?.Value, Is.EqualTo("(id: 123, name: Test)"));
            }
        }

        [Test]
        public void LogAspectShouldLogErrorOnException()
        {
            // Arrange
            var target = new AspectTestTarget();

            // Act
            using (var context = TestCorrelator.CreateContext())
            {
                Assert.That(() => target.MethodThatThrows(), Throws.InstanceOf<ArgumentException>());

                // Assert
                var logEvent = TestCorrelator.GetLogEventsFromContextId(context.Id).Single();

                Assert.That(logEvent.Level, Is.EqualTo(LogEventLevel.Error));
                Assert.That(logEvent.Exception, Is.InstanceOf<ArgumentException>());
                Assert.That(logEvent.MessageTemplate.Text, Is.EqualTo("Error while executing {type}.{name}{parameters}"));

                // Test the structured property values
                Assert.That((logEvent.Properties["type"] as ScalarValue)?.Value, Is.EqualTo("AspectTestTarget"));
                Assert.That((logEvent.Properties["name"] as ScalarValue)?.Value, Is.EqualTo("MethodThatThrows"));
            }
        }


        [Test]
        public void NoLogAspectShouldPreventLogging()
        {
            // Arrange
            var target = new AspectTestTarget();

            // Act
            using (var context = TestCorrelator.CreateContext())
            {
                target.ExcludedMethod();

                // Assert
                Assert.That(TestCorrelator.GetLogEventsFromContextId(context.Id), Is.Empty);
            }
        }


        [Test]
        public void LogAspectShouldLogPropertyChangedAtCorrectLevel()
        {
            // Arrange
            var target = new AspectTestTarget();
            var eventArgs = new PropertyChangedEventArgs("MyProperty");

            // Act
            using (var context = TestCorrelator.CreateContext())
            {
                target.OnPropertyChanged(target, eventArgs);

                // Assert
                var logEvent = TestCorrelator.GetLogEventsFromContextId(context.Id).Single();

                Assert.That(logEvent.Level, Is.EqualTo(LogEventLevel.Information));
                Assert.That(logEvent.MessageTemplate.Text, Is.EqualTo("Executed method {type}.{name}{parameters}"));

                // Test the structured property values
                Assert.That((logEvent.Properties["type"] as ScalarValue)?.Value, Is.EqualTo("AspectTestTarget"));
                Assert.That((logEvent.Properties["name"] as ScalarValue)?.Value, Is.EqualTo("OnPropertyChanged"));
                Assert.That((logEvent.Properties["parameters"] as ScalarValue)?.Value, Is.EqualTo("(property: MyProperty)"));
            }
        }

        [Test]
        public void MeasureAspectShouldLogDuration()
        {
            // Arrange
            var target = new AspectTestTarget();

            // Act
            using (var context = TestCorrelator.CreateContext())
            {
                target.MeasuredMethod();

                // Assert
                var logEvents = TestCorrelator.GetLogEventsFromContextId(context.Id);
                // The [Log] attribute on the class will log execution, and [Measure] will log duration. We expect two events.
                var measureEvent = logEvents.FirstOrDefault(e => e.MessageTemplate.Text.Contains("duration"));

                Assert.That(measureEvent, Is.Not.Null, "Measure log event was not found.");
                // The MeasureAspect implementation always logs duration at the Warning level.
                Assert.That(measureEvent.Level, Is.EqualTo(LogEventLevel.Warning));
                Assert.That(measureEvent.RenderMessage(), Does.Contain("AspectTestTarget.MeasuredMethod duration:"));
            }
        }
    }
}
