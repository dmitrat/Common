using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using OutWit.Common.Logging.Query.Model;

namespace OutWit.Common.Logging.Loki.Tests
{
    [TestFixture]
    public class LokiLogQueryProviderTests
    {
        #region Constants

        private const string SAMPLE_STREAMS_RESPONSE = """
        {
          "status": "success",
          "data": {
            "resultType": "streams",
            "result": [
              {
                "stream": {
                  "service_name": "WitIdentity",
                  "level": "Error",
                  "hostname": "prod-1"
                },
                "values": [
                  ["1704067200000000000", "Failed to authenticate user u-42"],
                  ["1704067201000000000", "DB connection refused"]
                ]
              }
            ]
          }
        }
        """;

        private const string SAMPLE_MATRIX_RESPONSE = """
        {
          "status": "success",
          "data": {
            "resultType": "matrix",
            "result": [
              { "metric": { "level": "Error" },       "values": [["1704067200","12"]] },
              { "metric": { "level": "Warning" },     "values": [["1704067200","5"]] },
              { "metric": { "level": "Information" }, "values": [["1704067200","123"]] }
            ]
          }
        }
        """;

        private const string SAMPLE_LABEL_VALUES_RESPONSE = """
        { "status": "success", "data": ["WitIdentity", "WitEngine", "WitCloud"] }
        """;

        #endregion

        #region Fields

        private HttpMessageHandlerStub m_handler = null!;
        private HttpClient m_http = null!;
        private LokiHttpClient m_client = null!;
        private LokiLogQueryProvider m_provider = null!;

        #endregion

        #region Setup

        [SetUp]
        public void Setup()
        {
            m_handler = new HttpMessageHandlerStub();
            m_http = new HttpClient(m_handler);
            m_client = new LokiHttpClient(m_http, new LokiOptions
            {
                BaseUrl = "http://loki:3100",
                BaseFilters = [LogFilter.Eq("service.name", "WitIdentity")],
                MaxRange = TimeSpan.FromDays(30)
            });
            m_provider = new LokiLogQueryProvider(m_client);
        }

        [TearDown]
        public void TearDown()
        {
            m_http.Dispose();
            m_handler.Dispose();
        }

        #endregion

        #region Query Tests

        [Test]
        public async Task QueryAsyncParsesStreamsResponseIntoLogEntriesTest()
        {
            m_handler.EnqueueResponse(SAMPLE_STREAMS_RESPONSE);

            var page = await m_provider.QueryAsync(new LogQuery
            {
                From = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                To = new DateTime(2024, 1, 1, 1, 0, 0, DateTimeKind.Utc)
            });

            Assert.That(page.Items, Has.Length.EqualTo(2));
            Assert.That(page.Items[0].Message, Is.EqualTo("Failed to authenticate user u-42"));
            Assert.That(page.Items[0].ServiceName, Is.EqualTo("WitIdentity"));
            Assert.That(page.Items[0].Level, Is.EqualTo(LogSeverity.Error));
            Assert.That(page.Items[0].Host, Is.EqualTo("prod-1"));
            Assert.That(page.Items[0].Timestamp, Is.EqualTo(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        }

        [Test]
        public async Task QueryAsyncIssuesQueryRangeRequestTest()
        {
            m_handler.EnqueueResponse(SAMPLE_STREAMS_RESPONSE);

            await m_provider.QueryAsync(new LogQuery
            {
                From = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                To = new DateTime(2024, 1, 1, 1, 0, 0, DateTimeKind.Utc)
            });

            Assert.That(m_handler.Received, Has.Count.EqualTo(1));
            var req = m_handler.Received[0];
            Assert.That(req.RequestUri!.AbsolutePath, Is.EqualTo("/loki/api/v1/query_range"));
            Assert.That(req.RequestUri.Query, Does.Contain("start="));
            Assert.That(req.RequestUri.Query, Does.Contain("end="));
            Assert.That(req.RequestUri.Query, Does.Contain("limit="));
        }

        [Test]
        public void QueryAsyncRejectsRangeBeyondMaxRangeTest()
        {
            // MaxRange in Setup is 30 days; ask for 60.
            var asyncOp = m_provider.QueryAsync(new LogQuery
            {
                From = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                To = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc)
            });

            Assert.That(async () => await asyncOp, Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        #endregion

        #region Statistics Tests

        [Test]
        public async Task GetStatisticsAsyncSumsLevelCountsTest()
        {
            m_handler.EnqueueResponse(SAMPLE_MATRIX_RESPONSE);

            var stats = await m_provider.GetStatisticsAsync(
                from: new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                to: new DateTime(2024, 1, 1, 1, 0, 0, DateTimeKind.Utc));

            Assert.That(stats.ErrorCount, Is.EqualTo(12));
            Assert.That(stats.WarningCount, Is.EqualTo(5));
            Assert.That(stats.InfoCount, Is.EqualTo(123));
            Assert.That(stats.TotalCount, Is.EqualTo(12 + 5 + 123));
        }

        #endregion

        #region Distinct Values Tests

        [Test]
        public async Task GetDistinctValuesAsyncReturnsLabelValuesTest()
        {
            m_handler.EnqueueResponse(SAMPLE_LABEL_VALUES_RESPONSE);

            var values = await m_provider.GetDistinctValuesAsync(
                from: new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                to: new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                attribute: LogAttribute.ServiceName);

            Assert.That(values, Is.EquivalentTo(new[] { "WitIdentity", "WitEngine", "WitCloud" }));
        }

        [Test]
        public async Task GetDistinctValuesAsyncRespectsLimitTest()
        {
            m_handler.EnqueueResponse(SAMPLE_LABEL_VALUES_RESPONSE);

            var values = await m_provider.GetDistinctValuesAsync(
                from: new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                to: new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                attribute: LogAttribute.ServiceName,
                limit: 2);

            Assert.That(values, Has.Count.EqualTo(2));
        }

        #endregion

        #region Unsupported Method Tests

        [Test]
        public async Task FindOffsetAsyncReturnsMinusOneTest()
        {
            // Loki has no offset concept; provider returns -1.
            var offset = await m_provider.FindOffsetAsync(new LogQuery(), DateTime.UtcNow);

            Assert.That(offset, Is.EqualTo(-1L));
        }

        [Test]
        public async Task GetStorageInfoAsyncReturnsAllNullsTest()
        {
            var info = await m_provider.GetStorageInfoAsync();

            Assert.That(info.UsedBytes, Is.Null);
            Assert.That(info.LimitBytes, Is.Null);
            Assert.That(info.TotalEntries, Is.Null);
            Assert.That(info.Breakdown, Is.Null);
        }

        #endregion
    }
}
