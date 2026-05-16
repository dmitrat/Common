using System.Collections.Generic;
using OutWit.Common.Logging.Loki.LogQL;
using OutWit.Common.Logging.Query.Model;

namespace OutWit.Common.Logging.Loki.Tests.LogQL
{
    [TestFixture]
    public class LogQlBuilderTests
    {
        #region Range Query Tests

        [Test]
        public void BuildRangeQueryWithBaseFiltersProducesStreamSelectorTest()
        {
            var query = new LogQuery();
            var baseFilters = new[] { LogFilter.Eq("service.name", "WitIdentity") };

            var logql = LogQlBuilder.BuildRangeQuery(query, baseFilters);

            Assert.That(logql, Is.EqualTo("{service_name=\"WitIdentity\"}"));
        }

        [Test]
        public void BuildRangeQueryWithLevelFilterFoldsIntoSelectorTest()
        {
            var query = new LogQuery
            {
                Filters = [LogFilter.Eq("level", "Error")]
            };
            var baseFilters = new[] { LogFilter.Eq("service.name", "WitIdentity") };

            var logql = LogQlBuilder.BuildRangeQuery(query, baseFilters);

            Assert.That(logql, Is.EqualTo("{service_name=\"WitIdentity\", level=\"Error\"}"));
        }

        [Test]
        public void BuildRangeQueryWithCustomAttributeAddsJsonFilterTest()
        {
            var query = new LogQuery
            {
                Filters = [LogFilter.Eq("user_id", "u-42")]
            };
            var baseFilters = new[] { LogFilter.Eq("service.name", "WitIdentity") };

            var logql = LogQlBuilder.BuildRangeQuery(query, baseFilters);

            Assert.That(logql, Is.EqualTo("{service_name=\"WitIdentity\"} | json | user_id = \"u-42\""));
        }

        [Test]
        public void BuildRangeQueryWithFullTextSearchAppendsRegexMatchTest()
        {
            var query = new LogQuery { FullTextSearch = "passkey" };
            var baseFilters = new[] { LogFilter.Eq("service.name", "WitIdentity") };

            var logql = LogQlBuilder.BuildRangeQuery(query, baseFilters);

            Assert.That(logql, Does.Contain("|~ \"passkey\""));
        }

        [Test]
        public void BuildRangeQueryNormalizesDottedLabelNameTest()
        {
            // LogQL label names cannot contain a dot; "service.name" must be normalized.
            var query = new LogQuery
            {
                Filters = [LogFilter.Eq("service.name", "WitIdentity")]
            };

            var logql = LogQlBuilder.BuildRangeQuery(query, null);

            Assert.That(logql, Is.EqualTo("{service_name=\"WitIdentity\"}"));
        }

        [Test]
        public void BuildRangeQueryEscapesQuotesInValuesTest()
        {
            var query = new LogQuery
            {
                Filters = [LogFilter.Eq("level", "say \"hi\"")]
            };

            var logql = LogQlBuilder.BuildRangeQuery(query, null);

            Assert.That(logql, Is.EqualTo("{level=\"say \\\"hi\\\"\"}"));
        }

        [Test]
        public void BuildRangeQueryEmptyBaseFiltersBackCompatTest()
        {
            // Same output as no base filters at all — null and [] are equivalent.
            var query = new LogQuery
            {
                Filters = [LogFilter.Eq("level", "Error")]
            };

            var withNull = LogQlBuilder.BuildRangeQuery(query, null);
            var withEmpty = LogQlBuilder.BuildRangeQuery(query, System.Array.Empty<LogFilter>());

            Assert.That(withNull, Is.EqualTo("{level=\"Error\"}"));
            Assert.That(withEmpty, Is.EqualTo("{level=\"Error\"}"));
        }

        [Test]
        public void BuildRangeQueryBaseFilterAndUserFilterCombineInSelectorTest()
        {
            var query = new LogQuery
            {
                Filters = [LogFilter.Eq("level", "Error")]
            };
            var baseFilters = new[]
            {
                LogFilter.Eq("service.name", "WitIdentity"),
                LogFilter.Eq("env", "prod")
            };

            var logql = LogQlBuilder.BuildRangeQuery(query, baseFilters);

            Assert.That(logql, Is.EqualTo("{service_name=\"WitIdentity\", env=\"prod\", level=\"Error\"}"));
        }

        [Test]
        public void BuildRangeQueryBaseFilterOnCustomAttributeGoesToJsonPipelineTest()
        {
            // Non-stream label — base filter still applies but lands behind `| json`.
            var query = new LogQuery();
            var baseFilters = new[] { LogFilter.Eq("tenant.id", "omnibus") };

            var logql = LogQlBuilder.BuildRangeQuery(query, baseFilters);

            Assert.That(logql, Is.EqualTo("{} | json | tenant_id = \"omnibus\""));
        }

        #endregion

        #region Level Histogram Tests

        [Test]
        public void BuildLevelHistogramProducesSumByLevelCountOverTimeTest()
        {
            var baseFilters = new[] { LogFilter.Eq("service.name", "WitIdentity") };

            var logql = LogQlBuilder.BuildLevelHistogram(null, baseFilters, System.TimeSpan.FromHours(1));

            Assert.That(logql, Is.EqualTo("sum by (level) (count_over_time({service_name=\"WitIdentity\"} | json [1h]))"));
        }

        [Test]
        public void BuildLevelHistogramFormatsDayRangeWithDSuffixTest()
        {
            var logql = LogQlBuilder.BuildLevelHistogram(null, null, System.TimeSpan.FromDays(3));

            Assert.That(logql, Does.EndWith("[3d]))"));
        }

        [Test]
        public void BuildLevelHistogramFormatsMinutesRangeWithMSuffixTest()
        {
            var logql = LogQlBuilder.BuildLevelHistogram(null, null, System.TimeSpan.FromMinutes(15));

            Assert.That(logql, Does.EndWith("[15m]))"));
        }

        #endregion
    }
}
