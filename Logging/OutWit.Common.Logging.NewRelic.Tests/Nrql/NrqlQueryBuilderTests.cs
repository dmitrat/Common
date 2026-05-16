using OutWit.Common.Logging.NewRelic.Model;
using OutWit.Common.Logging.NewRelic.Nrql;
using OutWit.Common.Logging.Query.Model;

namespace OutWit.Common.Logging.NewRelic.Tests.Nrql
{
    public class NrqlQueryBuilderTests
    {
        [SetUp]
        public void Setup()
        {
            BaseQuery = new LogQuery
            {
                PageSize = 100,
                Offset = 0,
                SortOrder = LogSortOrder.Descending
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
            BaseQuery.SortOrder = LogSortOrder.Ascending;
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
            BaseQuery.Filters = new LogFilter[]
            {
                LogFilter.Eq("level", "Error"),
                LogFilter.NotEq("service.name", "test-svc"),
                LogFilter.Contains("message", "failed"),
                LogFilter.NotContains("message", "ignore"),
                LogFilter.In("trace.id", "a", "b"),
                LogFilter.GreaterThan("value", "100")
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
            BaseQuery.Filters = new LogFilter[]
            {
                LogFilter.Eq("level", "Error")
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
            var filters = new List<LogFilter>
            {
                LogFilters.ServiceEquals("my-service")
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

        #region BaseFilters

        [Test]
        public void BuildNrqlWithBaseFilterTest()
        {
            // Arrange — provider-level scope only, no user filters.
            var baseFilters = new[] { LogFilter.Eq("service.name", "WitIdentity") };
            var expected = "SELECT * FROM Log WHERE service.name = 'WitIdentity' ORDER BY timestamp DESC LIMIT 100";

            // Act
            var result = BaseQuery.BuildNrql(baseFilters);

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void BuildNrqlBaseFilterPrependsUserFiltersTest()
        {
            // Arrange — base filter must come first, before search and user filters.
            BaseQuery.FullTextSearch = "boom";
            BaseQuery.Filters = new[] { LogFilter.Eq("level", "Error") };
            var baseFilters = new[] { LogFilter.Eq("service.name", "WitIdentity") };
            var expected = "SELECT * FROM Log WHERE service.name = 'WitIdentity' AND message LIKE '%boom%' AND level = 'Error' ORDER BY timestamp DESC LIMIT 100";

            // Act
            var result = BaseQuery.BuildNrql(baseFilters);

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void BuildNrqlWithMultipleBaseFiltersTest()
        {
            // Arrange — composite scope (e.g. service + host).
            var baseFilters = new[]
            {
                LogFilter.Eq("service.name", "WitIdentity"),
                LogFilter.In("host", "auth-1", "auth-2")
            };
            var expected = "SELECT * FROM Log WHERE service.name = 'WitIdentity' AND host IN ('auth-1', 'auth-2') ORDER BY timestamp DESC LIMIT 100";

            // Act
            var result = BaseQuery.BuildNrql(baseFilters);

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void BuildNrqlWithEmptyBaseFiltersBackCompatTest()
        {
            // Arrange — empty list must behave identically to no filtering.
            var expected = "SELECT * FROM Log ORDER BY timestamp DESC LIMIT 100";

            // Act
            var withNull = BaseQuery.BuildNrql(null);
            var withEmpty = BaseQuery.BuildNrql(Array.Empty<LogFilter>());

            // Assert
            Assert.That(withNull, Is.EqualTo(expected));
            Assert.That(withEmpty, Is.EqualTo(expected));
        }

        [Test]
        public void BuildDistinctNrqlWithBaseFiltersTest()
        {
            // Arrange
            var from = new DateTime(2025, 1, 1, 0, 0, 0);
            var to = new DateTime(2025, 1, 2, 0, 0, 0);
            var baseFilters = new[] { LogFilter.Eq("service.name", "WitIdentity") };
            var userFilters = new[] { LogFilter.Eq("level", "Error") };
            var expected = "SELECT uniques(Message.Properties.SourceContext) FROM Log WHERE service.name = 'WitIdentity' AND level = 'Error' SINCE '2025-01-01 00:00:00 +0000' UNTIL '2025-01-02 00:00:00 +0000' LIMIT 1000";

            // Act
            var result = NrqlQueryBuilder.BuildDistinctNrql(LogAttribute.SourceContext, from, to, userFilters, 1000, baseFilters);

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void BuildCountNrqlWithBaseFiltersTest()
        {
            // Arrange
            BaseQuery.Filters = new[] { LogFilter.Eq("level", "Error") };
            var baseFilters = new[] { LogFilter.Eq("service.name", "WitIdentity") };
            var target = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);
            var expected = "SELECT count(*) AS 'count' FROM Log WHERE service.name = 'WitIdentity' AND level = 'Error' AND timestamp > 1735725600000";

            // Act
            var result = NrqlQueryBuilder.BuildCountNrql(BaseQuery, target, baseFilters);

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void BuildStatisticsNrqlWithBaseFiltersTest()
        {
            // Arrange
            var from = new DateTime(2025, 1, 1);
            var to = new DateTime(2025, 1, 2);
            var baseFilters = new[] { LogFilter.Eq("service.name", "WitIdentity") };
            var expected = "SELECT count(*) AS 'totalCount', " +
                          "filter(count(*), WHERE level IN ('Error', 'Critical', 'Fatal')) AS 'errorCount', " +
                          "filter(count(*), WHERE level = 'Warning') AS 'warningCount', " +
                          "filter(count(*), WHERE level = 'Information') AS 'infoCount', " +
                          "filter(count(*), WHERE level IN ('Debug', 'Trace')) AS 'debugCount' " +
                          "FROM Log " +
                          "WHERE service.name = 'WitIdentity' " +
                          "SINCE '2025-01-01 00:00:00 +0000' " +
                          "UNTIL '2025-01-02 00:00:00 +0000'";

            // Act
            var result = NrqlQueryBuilder.BuildStatisticsNrql(from, to, filters: null, baseFilters: baseFilters);

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        #endregion

        private LogQuery BaseQuery { get; set; }
    }
}
