using OutWit.Common.NewRelic.Model;
using OutWit.Common.MemoryPack;
using OutWit.Common.NUnit;
using OutWit.Common.Utils;

namespace OutWit.Common.NewRelic.Tests.Model
{
    [TestFixture]
    public class NewRelicLogEntryTests
    {
        private NewRelicLogEntry CreateTestEntry()
        {
            return new NewRelicLogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = NewRelicLogSeverity.Warning,
                Message = "Test message",
                Exception = "Test exception",
                SourceContext = "TestContext",
                ServiceName = "TestService",
                Host = "TestHost",
                Environment = "TestEnv",
                TraceId = "trace-123",
                SpanId = "span-456"
            };
        }

        [Test]
        public void ConstructorTest()
        {
            // Arrange & Act
            var entry = new NewRelicLogEntry();

            // Assert
            Assert.That(entry.Timestamp, Is.EqualTo(default(DateTime)));
            Assert.That(entry.Message, Is.Null);
            Assert.That(entry.Level, Is.Null);
            Assert.That(entry.Exception, Is.Null);
        }

        [Test]
        public void IsTest()
        {
            // Arrange
            var entry = CreateTestEntry();

            // Assert
            Assert.That(entry, Was.EqualTo(entry.Clone()));
            Assert.That(entry, Was.Not.EqualTo(entry.With(x => x.Timestamp, DateTime.UtcNow.AddMinutes(1))));
            Assert.That(entry, Was.Not.EqualTo(entry.With(x => x.Level, NewRelicLogSeverity.Error)));
            Assert.That(entry, Was.Not.EqualTo(entry.With(x => x.Message, "Changed")));
            Assert.That(entry, Was.Not.EqualTo(entry.With(x => x.Exception, "Changed")));
            Assert.That(entry, Was.Not.EqualTo(entry.With(x => x.SourceContext, "Changed")));
            Assert.That(entry, Was.Not.EqualTo(entry.With(x => x.ServiceName, "Changed")));
            Assert.That(entry, Was.Not.EqualTo(entry.With(x => x.Host, "Changed")));
            Assert.That(entry, Was.Not.EqualTo(entry.With(x => x.Environment, "Changed")));
            Assert.That(entry, Was.Not.EqualTo(entry.With(x => x.TraceId, "trace-789")));
            Assert.That(entry, Was.Not.EqualTo(entry.With(x => x.SpanId, "span-789")));
        }

        [Test]
        public void CloneTest()
        {
            // Arrange
            var entry = CreateTestEntry();

            // Act
            var clone = entry.Clone() as NewRelicLogEntry;

            // Assert
            Assert.That(clone, Is.Not.Null);
            Assert.That(clone, Is.Not.SameAs(entry));
            Assert.That(clone, Was.EqualTo(entry));
        }

        [Test]
        public void MemoryPackCloneTest()
        {
            // Arrange
            var entry = CreateTestEntry();

            // Act
            var clone = entry.MemoryPackClone();

            // Assert
            Assert.That(clone, Is.Not.Null);
            Assert.That(clone, Is.Not.SameAs(entry));
            Assert.That(clone, Was.EqualTo(entry));
        }
    }
}
