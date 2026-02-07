using System.Text.Json;
using OutWit.Common.NewRelic.Model;
using OutWit.Common.NewRelic.Nrql;

namespace OutWit.Common.NewRelic.Tests.Nrql
{
    [TestFixture]
    public class NrqlResponseParserTests
    {
        [Test]
        public void ToLogEntriesMapsAllFieldsCorrectlyTest()
        {
            // Arrange
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var response = CreateMockResponse(new Dictionary<string, object>
            {
                { "timestamp", timestamp },
                { "message", "Test message" },
                { "level", "Information" },
                { "SourceContext", "MyLogger" },
                { "exception.message", "Test exception" },
                { "service.name", "my-service" },
                { "host", "my-host" },
                { "environment", "prod" },
                { "trace.id", "trace123" },
                { "span.id", "span456" }
            });

            // Act
            var entries = response.ToLogEntries();

            // Assert
            Assert.That(entries, Has.Length.EqualTo(1));
            var entry = entries[0];
            Assert.That(entry.Timestamp, Is.EqualTo(DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime));
            Assert.That(entry.Message, Is.EqualTo("Test message"));
            Assert.That(entry.Level, Is.EqualTo(NewRelicLogSeverity.Information));
            Assert.That(entry.SourceContext, Is.EqualTo("MyLogger"));
            Assert.That(entry.Exception, Is.EqualTo("Test exception"));
            Assert.That(entry.ServiceName, Is.EqualTo("my-service"));
            Assert.That(entry.Host, Is.EqualTo("my-host"));
            Assert.That(entry.Environment, Is.EqualTo("prod"));
            Assert.That(entry.TraceId, Is.EqualTo("trace123"));
            Assert.That(entry.SpanId, Is.EqualTo("span456"));
        }

        [Test]
        public void ToLogEntriesHandlesFieldAliasesTest()
        {
            // Arrange
            var response = CreateMockResponse(new Dictionary<string, object>
            {
                { "log.level", "Warning" },      // Alias for level
                { "serviceName", "my-service" }, // Alias for service.name
                { "hostname", "my-host" },       // Alias for host
                { "env", "dev" },                // Alias for environment
                { "traceId", "trace-abc" },      // Alias for trace.id
                { "spanId", "span-xyz" }         // Alias for span.id
            });

            // Act
            var entry = response.ToLogEntries().First();

            // Assert
            Assert.That(entry.Level, Is.EqualTo(NewRelicLogSeverity.Warning));
            Assert.That(entry.ServiceName, Is.EqualTo("my-service"));
            Assert.That(entry.Host, Is.EqualTo("my-host"));
            Assert.That(entry.Environment, Is.EqualTo("dev"));
            Assert.That(entry.TraceId, Is.EqualTo("trace-abc"));
            Assert.That(entry.SpanId, Is.EqualTo("span-xyz"));
        }

        [Test]
        public void ToLogEntriesConcatenatesExceptionFieldsTest()
        {
            // Arrange
            var response = CreateMockResponse(new Dictionary<string, object>
            {
                { "exception.message", "The error" },
                { "exception.stacktrace", "The stack..." }
            });

            // Act
            var entry = response.ToLogEntries().First();
            var expected = $"The error{Environment.NewLine}The stack...";

            // Assert
            Assert.That(entry.Exception, Is.EqualTo(expected));
        }

        [Test]
        public void ToLogEntriesSetsDefaultTimestampTest()
        {
            // Arrange
            var response = CreateMockResponse(new Dictionary<string, object>
            {
                { "message", "No timestamp" }
            });
            var before = DateTime.UtcNow;

            // Act
            var entry = response.ToLogEntries().First();

            // Assert
            Assert.That(entry.Timestamp, Is.GreaterThanOrEqualTo(before));
            Assert.That(entry.Timestamp, Is.LessThanOrEqualTo(DateTime.UtcNow));
        }

        [Test]
        public void ToLogEntriesReturnsEmptyArrayForEmptyResponseTest()
        {
            // Arrange
            var response = new List<Dictionary<string, JsonElement>>();

            // Act
            var entries = response.ToLogEntries();

            // Assert
            Assert.That(entries, Is.Empty);
        }

        [Test]
        public void ToLogEntriesConvertsJsonPrimitivesToStringsTest()
        {
            // Arrange
            var response = CreateMockResponse(new Dictionary<string, object>
            {
                { "message", true },
                { "level", "Error" },
                { "host", null }
            });

            // Act
            var entry = response.ToLogEntries().First();

            // Assert
            Assert.That(entry.Message, Is.EqualTo("true"));
            Assert.That(entry.Level, Is.EqualTo(NewRelicLogSeverity.Error));
            Assert.That(entry.Host, Is.Null);
        }

        // --- Helper Method ---

        /// <summary>
        /// Creates the mock response structure from simple object dictionaries.
        /// </summary>
        private List<Dictionary<string, JsonElement>> CreateMockResponse(params Dictionary<string, object>[] rows)
        {
            var responseList = new List<Dictionary<string, JsonElement>>();
            foreach (var row in rows)
            {
                var jsonDict = new Dictionary<string, JsonElement>();
                foreach (var kvp in row)
                {
                    // Serialize and re-parse to get a JsonElement
                    var json = JsonSerializer.Serialize(kvp.Value);
                    var element = JsonDocument.Parse(json).RootElement.Clone();
                    jsonDict.Add(kvp.Key, element);
                }
                responseList.Add(jsonDict);
            }
            return responseList;
        }
    }
}
