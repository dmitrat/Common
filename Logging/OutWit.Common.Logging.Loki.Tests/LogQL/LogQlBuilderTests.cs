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
        public void BuildRangeQueryWithDefaultLabelsProducesStreamSelectorTest()
        {
            var query = new LogQuery();
            var labels = new Dictionary<string, string> { ["service_name"] = "WitIdentity" };

            var logql = LogQlBuilder.BuildRangeQuery(query, labels);

            Assert.That(logql, Is.EqualTo("{service_name=\"WitIdentity\"}"));
        }

        [Test]
        public void BuildRangeQueryWithLevelFilterFoldsIntoSelectorTest()
        {
            var query = new LogQuery
            {
                Filters = [LogFilter.Eq("level", "Error")]
            };
            var labels = new Dictionary<string, string> { ["service_name"] = "WitIdentity" };

            var logql = LogQlBuilder.BuildRangeQuery(query, labels);

            Assert.That(logql, Is.EqualTo("{service_name=\"WitIdentity\", level=\"Error\"}"));
        }

        [Test]
        public void BuildRangeQueryWithCustomAttributeAddsJsonFilterTest()
        {
            var query = new LogQuery
            {
                Filters = [LogFilter.Eq("user_id", "u-42")]
            };
            var labels = new Dictionary<string, string> { ["service_name"] = "WitIdentity" };

            var logql = LogQlBuilder.BuildRangeQuery(query, labels);

            Assert.That(logql, Is.EqualTo("{service_name=\"WitIdentity\"} | json | user_id = \"u-42\""));
        }

        [Test]
        public void BuildRangeQueryWithFullTextSearchAppendsRegexMatchTest()
        {
            var query = new LogQuery { FullTextSearch = "passkey" };
            var labels = new Dictionary<string, string> { ["service_name"] = "WitIdentity" };

            var logql = LogQlBuilder.BuildRangeQuery(query, labels);

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

        #endregion

        #region Level Histogram Tests

        [Test]
        public void BuildLevelHistogramProducesSumByLevelCountOverTimeTest()
        {
            var labels = new Dictionary<string, string> { ["service_name"] = "WitIdentity" };

            var logql = LogQlBuilder.BuildLevelHistogram(null, labels, System.TimeSpan.FromHours(1));

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
