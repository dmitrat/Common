using System;
using System.Net.Http;

namespace OutWit.Common.Logging.Loki.Tests
{
    [TestFixture]
    public class LokiHttpClientTests
    {
        #region Header Tests

        [Test]
        public void ConstructorAddsTenantIdHeaderWhenProvidedTest()
        {
            using var http = new HttpClient(new HttpMessageHandlerStub());
            var options = new LokiOptions { BaseUrl = "http://loki:3100", TenantId = "team-a" };

            _ = new LokiHttpClient(http, options);

            Assert.That(http.DefaultRequestHeaders.Contains("X-Scope-OrgID"), Is.True);
        }

        [Test]
        public void ConstructorAddsBasicAuthHeaderWhenUsernameProvidedTest()
        {
            using var http = new HttpClient(new HttpMessageHandlerStub());
            var options = new LokiOptions { BaseUrl = "http://loki:3100", Username = "admin", Password = "secret" };

            _ = new LokiHttpClient(http, options);

            Assert.That(http.DefaultRequestHeaders.Authorization, Is.Not.Null);
            Assert.That(http.DefaultRequestHeaders.Authorization!.Scheme, Is.EqualTo("Basic"));
        }

        [Test]
        public void ConstructorThrowsWhenBaseUrlMissingTest()
        {
            using var http = new HttpClient(new HttpMessageHandlerStub());

            Assert.Throws<InvalidOperationException>(() =>
                _ = new LokiHttpClient(http, new LokiOptions()));
        }

        #endregion

        #region Timestamp Conversion Tests

        [Test]
        public void ToUnixNanosecondsForUnixEpochReturnsZeroTest()
        {
            var ns = LokiHttpClient.ToUnixNanoseconds(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            Assert.That(ns, Is.EqualTo(0L));
        }

        [Test]
        public void ToUnixNanosecondsConvertsKnownDateTest()
        {
            // 2024-01-01 00:00:00 UTC = 1704067200 seconds since epoch.
            var ns = LokiHttpClient.ToUnixNanoseconds(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            Assert.That(ns, Is.EqualTo(1704067200_000_000_000L));
        }

        #endregion
    }
}
