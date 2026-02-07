using System.Text.Json;
using OutWit.Common.NewRelic.Nrql;

namespace OutWit.Common.NewRelic.Tests.Nrql
{
    [TestFixture]
    public class NrqlResponseParserStatisticsTests
    {
        [Test]
        public void ToStatisticsWithValidDataTest()
        {
            // Arrange
            var from = new DateTime(2025, 1, 1);
            var to = new DateTime(2025, 1, 8);
            var response = new List<Dictionary<string, JsonElement>>
            {
                CreateRow(new()
                {
                    { "totalCount", 1000000L },
                    { "errorCount", 10000L },
                    { "warningCount", 50000L },
                    { "infoCount", 800000L },
                    { "debugCount", 140000L }
                })
            };

            // Act
            var result = response.ToStatistics(from, to);

            // Assert
            Assert.That(result.From, Is.EqualTo(from));
            Assert.That(result.To, Is.EqualTo(to));
            Assert.That(result.TotalCount, Is.EqualTo(1000000));
            Assert.That(result.ErrorCount, Is.EqualTo(10000));
            Assert.That(result.WarningCount, Is.EqualTo(50000));
            Assert.That(result.InfoCount, Is.EqualTo(800000));
            Assert.That(result.DebugCount, Is.EqualTo(140000));
        }

        [Test]
        public void ToStatisticsWithEmptyResponseTest()
        {
            // Arrange
            var from = new DateTime(2025, 1, 1);
            var to = new DateTime(2025, 1, 8);
            var response = new List<Dictionary<string, JsonElement>>();

            // Act
            var result = response.ToStatistics(from, to);

            // Assert
            Assert.That(result.From, Is.EqualTo(from));
            Assert.That(result.To, Is.EqualTo(to));
            Assert.That(result.TotalCount, Is.EqualTo(0));
        }

        [Test]
        public void ToStatisticsHandlesMissingFieldsTest()
        {
            // Arrange
            var from = new DateTime(2025, 1, 1);
            var to = new DateTime(2025, 1, 8);
            var response = new List<Dictionary<string, JsonElement>>
            {
                CreateRow(new()
                {
                    { "totalCount", 1000L },
                    // Missing other fields
                })
            };

            // Act
            var result = response.ToStatistics(from, to);

            // Assert
            Assert.That(result.TotalCount, Is.EqualTo(1000));
            Assert.That(result.ErrorCount, Is.EqualTo(0));
            Assert.That(result.WarningCount, Is.EqualTo(0));
            Assert.That(result.InfoCount, Is.EqualTo(0));
            Assert.That(result.DebugCount, Is.EqualTo(0));
        }

        [Test]
        public void ToDataConsumptionWithValidDataTest()
        {
            // Arrange
            var from = new DateTime(2025, 1, 1);
            var to = new DateTime(2025, 1, 15); // 14 days
            var response = new List<Dictionary<string, JsonElement>>
            {
                CreateRow(new() { { "facet", "Logs" }, { "sum.GigabytesIngested", 0.5 } }),
                CreateRow(new() { { "facet", "Metrics" }, { "sum.GigabytesIngested", 10.2 } }),
                CreateRow(new() { { "facet", "APM" }, { "sum.GigabytesIngested", 3.3 } }),
                CreateRow(new() { { "facet", "Infrastructure" }, { "sum.GigabytesIngested", 2.0 } })
            };

            // Act
            var result = response.ToDataConsumption(from, to);

            // Assert
            Assert.That(result.StartDate, Is.EqualTo(from));
            Assert.That(result.EndDate, Is.EqualTo(to));
            Assert.That(result.TotalGigabytes, Is.EqualTo(16.0).Within(0.01));
            Assert.That(result.DailyAverageGigabytes, Is.EqualTo(16.0 / 14).Within(0.01));
            Assert.That(result.LogsGigabytes, Is.EqualTo(0.5));
            Assert.That(result.MetricsGigabytes, Is.EqualTo(10.2));
            Assert.That(result.TracesGigabytes, Is.EqualTo(5.3).Within(0.01)); // APM + Infrastructure
            Assert.That(result.EventsGigabytes, Is.EqualTo(0));
        }

        [Test]
        public void ToDataConsumptionWithEmptyResponseTest()
        {
            // Arrange
            var from = new DateTime(2025, 1, 1);
            var to = new DateTime(2025, 1, 15);
            var response = new List<Dictionary<string, JsonElement>>();

            // Act
            var result = response.ToDataConsumption(from, to);

            // Assert
            Assert.That(result.StartDate, Is.EqualTo(from));
            Assert.That(result.EndDate, Is.EqualTo(to));
            Assert.That(result.TotalGigabytes, Is.EqualTo(0));
            Assert.That(result.DailyAverageGigabytes, Is.EqualTo(0));
        }

        [Test]
        public void ToDataConsumptionWithProductLineFieldTest()
        {
            // Arrange - using productLine instead of facet
            var from = new DateTime(2025, 1, 1);
            var to = new DateTime(2025, 1, 10);
            var response = new List<Dictionary<string, JsonElement>>
            {
                CreateRow(new() { { "productLine", "Logging" }, { "sum", 1.5 } }),
                CreateRow(new() { { "productLine", "Metric" }, { "sum", 8.0 } })
            };

            // Act
            var result = response.ToDataConsumption(from, to);

            // Assert
            Assert.That(result.TotalGigabytes, Is.EqualTo(9.5).Within(0.01));
            Assert.That(result.LogsGigabytes, Is.EqualTo(1.5));
            Assert.That(result.MetricsGigabytes, Is.EqualTo(8.0));
        }

        [Test]
        public void ToDataConsumptionAggregatesUnknownProductsTest()
        {
            // Arrange
            var from = new DateTime(2025, 1, 1);
            var to = new DateTime(2025, 1, 10);
            var response = new List<Dictionary<string, JsonElement>>
            {
                CreateRow(new() { { "facet", "Browser" }, { "sum.GigabytesIngested", 0.5 } }),
                CreateRow(new() { { "facet", "Mobile" }, { "sum.GigabytesIngested", 0.3 } }),
                CreateRow(new() { { "facet", "Serverless" }, { "sum.GigabytesIngested", 0.2 } }),
                CreateRow(new() { { "facet", "UnknownProduct" }, { "sum.GigabytesIngested", 0.1 } })
            };

            // Act
            var result = response.ToDataConsumption(from, to);

            // Assert
            // All should go to EventsGigabytes
            Assert.That(result.EventsGigabytes, Is.EqualTo(1.1).Within(0.01));
            Assert.That(result.TotalGigabytes, Is.EqualTo(1.1).Within(0.01));
        }

        // --- Helper Method ---

        private Dictionary<string, JsonElement> CreateRow(Dictionary<string, object> data)
        {
            var result = new Dictionary<string, JsonElement>();
            foreach (var kvp in data)
            {
                var json = JsonSerializer.Serialize(kvp.Value);
                var element = JsonDocument.Parse(json).RootElement.Clone();
                result[kvp.Key] = element;
            }
            return result;
        }
    }
}
