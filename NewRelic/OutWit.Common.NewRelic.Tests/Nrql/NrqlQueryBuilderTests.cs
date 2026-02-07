using OutWit.Common.NewRelic.Model;
using OutWit.Common.NewRelic.Nrql;

namespace OutWit.Common.NewRelic.Tests.Nrql
{
    public class NrqlQueryBuilderTests
    {
        [SetUp]
        public void Setup()
        {
            BaseQuery = new NewRelicLogQuery
            {
                PageSize = 100,
                Offset = 0,
                SortOrder = NewRelicLogSortOrder.Descending
            };
        }

        [Test]
        public void BuildNrqlWithSimpleLimitTest()
        {
            // Arrange
            BaseQuery.PageSize = 10;
            var expected = "SELECT * FROM Log ORDER BY timestamp DESC LIMIT 10";

            // Act
            var result = BaseQuery.BuildNrql();

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void BuildNrqlWithTimeWindowTest()
        {
            // Arrange
            BaseQuery.From = new DateTime(2025, 1, 1, 10, 0, 0);
            BaseQuery.To = new DateTime(2025, 1, 1, 11, 0, 0);
            var expected = "SELECT * FROM Log SINCE '2025-01-01 10:00:00 +0000' UNTIL '2025-01-01 11:00:00 +0000' ORDER BY timestamp DESC LIMIT 100";

            // Act
            var result = BaseQuery.BuildNrql();

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void BuildNrqlWithOffsetTest()
        {
            // Arrange
            BaseQuery.Offset = 200;
            var expected = "SELECT * FROM Log ORDER BY timestamp DESC LIMIT 100 OFFSET 200";

            // Act
            var result = BaseQuery.BuildNrql();

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void BuildNrqlWithAscendingSortTest()
        {
            // Arrange
            BaseQuery.SortOrder = NewRelicLogSortOrder.Ascending;
            var expected = "SELECT * FROM Log ORDER BY timestamp ASC LIMIT 100";

            // Act
            var result = BaseQuery.BuildNrql();

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void BuildNrqlWithFullTextSearchTest()
        {
            // Arrange
            BaseQuery.FullTextSearch = "error 'quotes'";
            var expected = "SELECT * FROM Log WHERE message LIKE '%error \\'quotes\\'%' ORDER BY timestamp DESC LIMIT 100";

            // Act
            var result = BaseQuery.BuildNrql();

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void BuildNrqlWithMultipleFiltersTest()
        {
            // Arrange
            BaseQuery.Filters = new NewRelicLogFilter[]
            {
                NewRelicLogFilter.Eq("level", "Error"),
                NewRelicLogFilter.NotEq("service.name", "test-svc"),
                NewRelicLogFilter.Contains("message", "failed"),
                NewRelicLogFilter.NotContains("message", "ignore"),
                NewRelicLogFilter.In("trace.id", "a", "b"),
                NewRelicLogFilter.GreaterThan("value", "100")
            };

            var expected = "SELECT * FROM Log WHERE level = 'Error' AND service.name != 'test-svc' AND message LIKE '%failed%' AND message NOT LIKE '%ignore%' AND trace.id IN ('a', 'b') AND value > 100 ORDER BY timestamp DESC LIMIT 100";

            // Act
            var result = BaseQuery.BuildNrql();

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void BuildNrqlWithAllOptionsTest()
        {
            // Arrange
            BaseQuery.From = new DateTime(2025, 1, 1, 10, 0, 0);
            BaseQuery.To = new DateTime(2025, 1, 1, 11, 0, 0);
            BaseQuery.Offset = 50;
            BaseQuery.FullTextSearch = "failed";
            BaseQuery.Filters = new NewRelicLogFilter[]
            {
                NewRelicLogFilter.Eq("level", "Error")
            };

            var expected = "SELECT * FROM Log WHERE message LIKE '%failed%' AND level = 'Error' SINCE '2025-01-01 10:00:00 +0000' UNTIL '2025-01-01 11:00:00 +0000' ORDER BY timestamp DESC LIMIT 100 OFFSET 50";

            // Act
            var result = BaseQuery.BuildNrql();

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void BuildStatisticsNrqlTest()
        {
            // Arrange
            var from = new DateTime(2025, 1, 1, 0, 0, 0);
            var to = new DateTime(2025, 1, 8, 0, 0, 0);
            
            var expected = "SELECT count(*) AS 'totalCount', " +
                          "filter(count(*), WHERE level IN ('Error', 'Critical', 'Fatal')) AS 'errorCount', " +
                          "filter(count(*), WHERE level = 'Warning') AS 'warningCount', " +
                          "filter(count(*), WHERE level = 'Information') AS 'infoCount', " +
                          "filter(count(*), WHERE level IN ('Debug', 'Trace')) AS 'debugCount' " +
                          "FROM Log " +
                          "SINCE '2025-01-01 00:00:00 +0000' " +
                          "UNTIL '2025-01-08 00:00:00 +0000'";

            // Act
            var result = NrqlQueryBuilder.BuildStatisticsNrql(from, to);

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void BuildStatisticsNrqlWithFiltersTest()
        {
            // Arrange
            var from = new DateTime(2025, 1, 1);
            var to = new DateTime(2025, 1, 2);
            var filters = new List<NewRelicLogFilter>
            {
                NewRelicLogFilters.ServiceEquals("my-service")
            };

            var expected = "SELECT count(*) AS 'totalCount', " +
                          "filter(count(*), WHERE level IN ('Error', 'Critical', 'Fatal')) AS 'errorCount', " +
                          "filter(count(*), WHERE level = 'Warning') AS 'warningCount', " +
                          "filter(count(*), WHERE level = 'Information') AS 'infoCount', " +
                          "filter(count(*), WHERE level IN ('Debug', 'Trace')) AS 'debugCount' " +
                          "FROM Log " +
                          "WHERE service.name = 'my-service' " +
                          "SINCE '2025-01-01 00:00:00 +0000' " +
                          "UNTIL '2025-01-02 00:00:00 +0000'";

            // Act
            var result = NrqlQueryBuilder.BuildStatisticsNrql(from, to, filters);

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void BuildConsumptionNrqlTest()
        {
            // Arrange
            var from = new DateTime(2025, 1, 1);
            var to = new DateTime(2025, 1, 15);

            var expected = "FROM NrConsumption SELECT sum(GigabytesIngested) SINCE '2025-01-01' UNTIL '2025-01-15' FACET productLine LIMIT 100";

            // Act
            var result = NrqlQueryBuilder.BuildConsumptionNrql(from, to);

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        private NewRelicLogQuery BaseQuery { get; set; }
    }
}
