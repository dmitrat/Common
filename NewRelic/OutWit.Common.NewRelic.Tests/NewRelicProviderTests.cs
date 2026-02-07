using Microsoft.Extensions.Configuration;
using OutWit.Common.NewRelic.Interfaces;
using OutWit.Common.NewRelic.Model;
using OutWit.Common.Rest.Exceptions;

namespace OutWit.Common.NewRelic.Tests
{
    [TestFixture]
    [Explicit]
    public class NewRelicProviderTests
    {
        #region Setup

        [SetUp]
        public void Setup()
        {
            Configuration = new ConfigurationBuilder()
                .AddUserSecrets<NewRelicProviderTests>()
                .Build();

            // Load the values
            ApiKey = Configuration["NewRelic:ApiKey"];
            int.TryParse(Configuration["NewRelic:AccountId"], out var accountId);
            AccountId = accountId;

            // Fail-fast if secrets aren't configured
            if (string.IsNullOrEmpty(ApiKey) || AccountId == 0)
            {
                Assert.Inconclusive(
                    "New Relic secrets (ApiKey, AccountId) are not configured. " +
                    "Set them using 'dotnet user-secrets set' or environment variables.");
            }

            // Use the secrets loaded by TestConfig
            var options = new NewRelicClientOptions
            {
                ApiKey = ApiKey,
                AccountId = AccountId
            };

            var client = new NewRelicHttpClient(options);
            Provider = new NewRelicProvider(client);
        }

        #endregion

        #region Basic Query Tests

        [Test]
        public async Task GetRecentLogsSucceedsTest()
        {
            // Arrange
            // Act & Assert
            NewRelicLogPage page = null;

            // We use Assert.DoesNotThrowAsync to catch RestClientException
            // which would be thrown on auth failure (401/403).
            Assert.DoesNotThrowAsync(async () =>
            {
                page = await Provider.GetRecentLogsAsync(TimeSpan.FromDays(1), pageSize: 100);
            }, "API call failed. Check API Key and Account ID.");

            // Basic validation of the response
            Assert.That(page, Is.Not.Null);
            Assert.That(page.PageSize, Is.EqualTo(100));
            Assert.That(page.Items, Is.Not.Null);
        }

        [Test]
        public async Task GetLogsWithDateRangeSucceedsTest()
        {
            // Arrange
            var from = DateTime.UtcNow.AddHours(-2);
            var to = DateTime.UtcNow;

            // Act
            NewRelicLogPage page = null;

            Assert.DoesNotThrowAsync(async () =>
            {
                page = await Provider.GetLogsAsync(from, to, pageSize: 50);
            }, "API call failed. Check API Key and Account ID.");

            // Assert
            Assert.That(page, Is.Not.Null);
            Assert.That(page.PageSize, Is.EqualTo(50));
            Assert.That(page.Items, Is.Not.Null);
        }

        [Test]
        public async Task QueryAsyncWithFullParametersSucceedsTest()
        {
            // Arrange
            var query = new NewRelicLogQuery
            {
                Lookback = TimeSpan.FromHours(1),
                FullTextSearch = "request",
                Filters = new[]
                {
                    NewRelicLogFilters.LevelAtLeast(NewRelicLogSeverity.Information)
                },
                PageSize = 25,
                Offset = 0,
                SortOrder = NewRelicLogSortOrder.Descending
            };

            // Act
            NewRelicLogPage page = null;

            Assert.DoesNotThrowAsync(async () =>
            {
                page = await Provider.QueryAsync(query);
            }, "API call failed. Check API Key and Account ID.");

            // Assert
            Assert.That(page, Is.Not.Null);
            Assert.That(page.PageSize, Is.EqualTo(25));
        }

        #endregion

        #region Filter Tests

        [Test]
        public async Task SearchAsyncWithFiltersSucceedsTest()
        {
            // Arrange
            var filters = new List<NewRelicLogFilter>
            {
                // Use a filter that is likely to return few results
                NewRelicLogFilters.LevelAtLeast(NewRelicLogSeverity.Error)
            };

            // Act
            NewRelicLogPage page = null;

            Assert.DoesNotThrowAsync(async () =>
            {
                page = await Provider.SearchAsync(
                    text: "request",
                    lookback: TimeSpan.FromDays(3),
                    extraFilters: filters,
                    pageSize: 5);
            }, "API call failed. Check API Key and Account ID.");

            // Assert
            Assert.That(page, Is.Not.Null);
            Assert.That(page.PageSize, Is.EqualTo(5));
        }

        [Test]
        public async Task QueryWithMultipleFiltersSucceedsTest()
        {
            // Arrange
            var query = new NewRelicLogQuery
            {
                Lookback = TimeSpan.FromDays(1),
                Filters = new[]
                {
                    NewRelicLogFilters.LevelEquals(NewRelicLogSeverity.Error),
                    NewRelicLogFilters.MessageContains("exception")
                },
                PageSize = 10
            };

            // Act
            NewRelicLogPage page = null;

            Assert.DoesNotThrowAsync(async () =>
            {
                page = await Provider.QueryAsync(query);
            }, "API call failed.");

            // Assert
            Assert.That(page, Is.Not.Null);
        }

        #endregion

        #region Distinct Values Tests

        [Test]
        public async Task GetDistinctValuesTest()
        {
            // Arrange
            // Act & Assert
            IReadOnlyList<string> values = null;

            // We use Assert.DoesNotThrowAsync to catch RestClientException
            // which would be thrown on auth failure (401/403).
            Assert.DoesNotThrowAsync(async () =>
            {
                values = await Provider.GetDistinctValuesAsync(
                    DateTime.UtcNow.AddDays(-3),
                    DateTime.UtcNow,
                    NewRelicLogAttribute.SourceContext);
            }, "API call failed. Check API Key and Account ID.");

            // Basic validation of the response
            Assert.That(values, Is.Not.Empty);
        }

        [Test]
        public async Task GetDistinctValuesWithFiltersTest()
        {
            // Arrange
            var filters = new List<NewRelicLogFilter>
            {
                NewRelicLogFilters.LevelEquals(NewRelicLogSeverity.Error)
            };

            // Act
            IReadOnlyList<string> values = null;

            Assert.DoesNotThrowAsync(async () =>
            {
                values = await Provider.GetDistinctValuesAsync(
                    DateTime.UtcNow.AddDays(-1),
                    DateTime.UtcNow,
                    NewRelicLogAttribute.SourceContext,
                    filters,
                    limit: 50);
            }, "API call failed.");

            // Assert
            Assert.That(values, Is.Not.Null);
        }

        #endregion

        #region FindOffset Tests

        [Test]
        public async Task FindOffsetTest()
        {
            // Arrange
            var searchText = "request";
            // Fix the time so intervals match exactly
            var now = DateTime.UtcNow;
            var lookback = TimeSpan.FromDays(3);

            // 1. Create a query object that we'll use everywhere
            var baseQuery = new NewRelicLogQuery
            {
                FullTextSearch = searchText,
                From = now.Subtract(lookback),
                To = now,
                Filters = new[]
                {
                    NewRelicLogFilters.LevelEquals(NewRelicLogSeverity.Information)
                },
                PageSize = 150,
                SortOrder = NewRelicLogSortOrder.Descending
            };

            // 2. Make initial query to find a "target" entry (victim for the test)
            var initialPage = await Provider.QueryAsync(baseQuery);

            Assert.That(initialPage.Items, Is.Not.Empty, "Search returned no logs. Cannot perform Seek test.");

            // Take an entry from somewhere in the middle (e.g., 30th)
            var targetIndex = 30;
            if (initialPage.Items.Length <= targetIndex)
            {
                Assert.Inconclusive($"Not enough logs found ({initialPage.Items.Length}) to skip {targetIndex}. Need more data for this test.");
            }

            var targetEntry = initialPage.Items[targetIndex];

            // Act
            // Call the new version of FindOffsetAsync, passing query object and Timestamp
            var calculatedOffset = await Provider.FindOffsetAsync(baseQuery, targetEntry.Timestamp);

            // Assert
            Console.WriteLine($"Target Index: {targetIndex}, Calculated Offset: {calculatedOffset}");
            Assert.That(calculatedOffset, Is.GreaterThan(0));

            // Allow a small margin of error (+/- 5), as the order of logs with identical Timestamp is not guaranteed
            Assert.That(calculatedOffset, Is.EqualTo(targetIndex).Within(5));

            // 3. Verification (Reality check)
            // Use the same query but change Offset to the calculated one to try to retrieve this entry

            // Important: clone the query so we don't spoil baseQuery (if you need it clean),
            // or just change parameters if NewRelicLogQuery allows it (you have a Clone method in the model).
            var verifyQuery = baseQuery.Clone();
            verifyQuery.PageSize = 1;
            verifyQuery.Offset = (int)calculatedOffset;

            var verifyPage = await Provider.QueryAsync(verifyQuery);

            Assert.That(verifyPage.Items, Is.Not.Empty, "Verify request returned empty page.");
            var foundEntry = verifyPage.Items[0];

            // Check that at the calculated offset we found an entry with the same timestamp
            Assert.That(foundEntry.Timestamp, Is.EqualTo(targetEntry.Timestamp).Within(TimeSpan.FromMilliseconds(100)),
                "Seek functionality failed: the entry at the calculated offset has a different timestamp.");
        }

        [Test]
        public async Task FindOffsetForFirstEntryReturnsZeroTest()
        {
            // Arrange
            var query = new NewRelicLogQuery
            {
                Lookback = TimeSpan.FromDays(1),
                PageSize = 10,
                SortOrder = NewRelicLogSortOrder.Descending
            };

            var page = await Provider.QueryAsync(query);
            Assume.That(page.Items, Is.Not.Empty, "Need logs to test FindOffset");

            var firstEntry = page.Items[0];

            // Act
            var offset = await Provider.FindOffsetAsync(query, firstEntry.Timestamp);

            // Assert
            // First entry should have offset 0 or very close to it
            Assert.That(offset, Is.LessThanOrEqualTo(5),
                "First entry offset should be 0 or very small due to timestamp precision");
        }

        [Test]
        public async Task FindOffsetForNonexistentTimestampTest()
        {
            // Arrange
            var query = new NewRelicLogQuery
            {
                Lookback = TimeSpan.FromDays(1),
                PageSize = 10
            };

            // Use a timestamp far in the future
            var futureTimestamp = DateTime.UtcNow.AddYears(10);

            // Act
            var offset = await Provider.FindOffsetAsync(query, futureTimestamp);

            // Assert
            // For a future timestamp, offset should be the total count (all logs are before it)
            Assert.That(offset, Is.GreaterThanOrEqualTo(0));
        }

        #endregion

        #region Pagination Tests

        [Test]
        public async Task PaginationWorkflowTest()
        {
            // Arrange
            var pageSize = 10;
            var query = new NewRelicLogQuery
            {
                Lookback = TimeSpan.FromDays(1),
                PageSize = pageSize,
                Offset = 0,
                SortOrder = NewRelicLogSortOrder.Descending
            };

            // Act - Get first page
            var firstPage = await Provider.QueryAsync(query);

            Assert.That(firstPage, Is.Not.Null);
            Assert.That(firstPage.Items, Is.Not.Empty);

            // Get second page
            query.Offset = pageSize;
            var secondPage = await Provider.QueryAsync(query);

            // Assert
            Assert.That(secondPage, Is.Not.Null);

            // Verify that pages contain different entries
            if (firstPage.Items.Length > 0 && secondPage.Items.Length > 0)
            {
                Assert.That(firstPage.Items[0].Timestamp,
                    Is.Not.EqualTo(secondPage.Items[0].Timestamp),
                    "First entries of different pages should have different timestamps");
            }
        }

        [Test]
        public async Task HasMoreFlagIsAccurateTest()
        {
            // Arrange
            var query = new NewRelicLogQuery
            {
                Lookback = TimeSpan.FromDays(1),
                PageSize = 5
            };

            // Act
            var page = await Provider.QueryAsync(query);

            // Assert
            if (page.HasMore)
            {
                // If HasMore is true, we should be able to get next page
                query.Offset = page.PageSize;
                var nextPage = await Provider.QueryAsync(query);
                Assert.That(nextPage.Items, Is.Not.Empty,
                    "HasMore was true but next page is empty");
            }
        }

        [Test]
        public async Task ZeroOffsetReturnsFirstPageTest()
        {
            // Arrange
            var query = new NewRelicLogQuery
            {
                Lookback = TimeSpan.FromHours(1),
                PageSize = 5,
                Offset = 0
            };

            // Act
            var page = await Provider.QueryAsync(query);

            // Assert
            Assert.That(page, Is.Not.Null);
            Assert.That(page.Offset, Is.EqualTo(0));
        }

        #endregion

        #region Sort Order Tests

        [Test]
        public async Task DescendingSortOrderTest()
        {
            // Arrange
            var query = new NewRelicLogQuery
            {
                Lookback = TimeSpan.FromHours(1),
                PageSize = 10,
                SortOrder = NewRelicLogSortOrder.Descending
            };

            // Act
            var page = await Provider.QueryAsync(query);

            // Assert
            Assume.That(page.Items, Has.Length.GreaterThan(1), "Need at least 2 entries to verify sort order");

            // Verify descending order (newest first)
            for (int i = 0; i < page.Items.Length - 1; i++)
            {
                Assert.That(page.Items[i].Timestamp, Is.GreaterThanOrEqualTo(page.Items[i + 1].Timestamp),
                    $"Entry {i} should have timestamp >= entry {i + 1} in descending order");
            }
        }

        [Test]
        public async Task AscendingSortOrderTest()
        {
            // Arrange
            var query = new NewRelicLogQuery
            {
                Lookback = TimeSpan.FromHours(1),
                PageSize = 10,
                SortOrder = NewRelicLogSortOrder.Ascending
            };

            // Act
            var page = await Provider.QueryAsync(query);

            // Assert
            Assume.That(page.Items, Has.Length.GreaterThan(1), "Need at least 2 entries to verify sort order");

            // Verify ascending order (oldest first)
            for (int i = 0; i < page.Items.Length - 1; i++)
            {
                Assert.That(page.Items[i].Timestamp, Is.LessThanOrEqualTo(page.Items[i + 1].Timestamp),
                    $"Entry {i} should have timestamp <= entry {i + 1} in ascending order");
            }
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public void InvalidApiKeyThrowsExceptionTest()
        {
            // Arrange
            var options = new NewRelicClientOptions
            {
                ApiKey = "NRAK-InvalidKey12345", // Deliberately bad key
                AccountId = AccountId
            };
            var client = new NewRelicHttpClient(options);
            var provider = new NewRelicProvider(client);

            // Act & Assert
            // We expect a RestClientException (likely 401 or 403)
            Assert.ThrowsAsync<RestClientException>(async () =>
            {
                await provider.GetRecentLogsAsync(TimeSpan.FromMinutes(1));
            });
        }

        [Test]
        public void NullQueryThrowsArgumentNullExceptionTest()
        {
            // Arrange
            NewRelicLogQuery query = null;

            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await Provider.QueryAsync(query);
            });
        }

        [Test]
        public async Task EmptyResultSetReturnsEmptyPageTest()
        {
            // Arrange
            var query = new NewRelicLogQuery
            {
                // Search for something that definitely doesn't exist
                FullTextSearch = "XyZ_NonExistent_String_9999_ZyX",
                Lookback = TimeSpan.FromMinutes(1),
                PageSize = 10
            };

            // Act
            var page = await Provider.QueryAsync(query);

            // Assert
            Assert.That(page, Is.Not.Null);
            Assert.That(page.Items, Is.Empty);
            Assert.That(page.HasMore, Is.False);
        }

        #endregion

        #region Edge Cases

        [Test]
        public async Task VeryLargePageSizeIsClampedTest()
        {
            // Arrange
            var query = new NewRelicLogQuery
            {
                Lookback = TimeSpan.FromDays(1),
                PageSize = 10000, // Deliberately large
                SortOrder = NewRelicLogSortOrder.Descending
            };

            // Act
            var page = await Provider.QueryAsync(query);

            // Assert
            Assert.That(page, Is.Not.Null);
            // PageSize should be clamped to MaxPageSize
            Assert.That(page.PageSize, Is.LessThanOrEqualTo(2000),
                "PageSize should be clamped to reasonable limit");
        }

        #endregion

        #region Statistics Tests

        [Test]
        public async Task GetStatisticsSucceedsTest()
        {
            // Arrange
            var from = DateTime.UtcNow.AddDays(-7);
            var to = DateTime.UtcNow;

            // Act
            NewRelicLogStatistics stats = null;

            Assert.DoesNotThrowAsync(async () =>
            {
                stats = await Provider.GetStatisticsAsync(from, to);
            }, "API call failed. Check API Key and Account ID.");

            // Assert
            Assert.That(stats, Is.Not.Null);
            Assert.That(stats.From, Is.EqualTo(from).Within(TimeSpan.FromSeconds(1)));
            Assert.That(stats.To, Is.EqualTo(to).Within(TimeSpan.FromSeconds(1)));
            Assert.That(stats.TotalCount, Is.GreaterThanOrEqualTo(0));

            // Log the results for manual verification
            Console.WriteLine($"Log Statistics for last 7 days:");
            Console.WriteLine($"  Total Logs: {stats.TotalCount:N0}");
            Console.WriteLine($"  Errors: {stats.ErrorCount:N0} ({stats.ErrorRate:N2}%)");
            Console.WriteLine($"  Warnings: {stats.WarningCount:N0} ({stats.WarningRate:N2}%)");
            Console.WriteLine($"  Info: {stats.InfoCount:N0} ({stats.InfoRate:N2}%)");
            Console.WriteLine($"  Debug: {stats.DebugCount:N0} ({stats.DebugRate:N2}%)");
            Console.WriteLine($"  Average per day: {stats.AverageLogsPerDay:N0} logs");
            Console.WriteLine($"  Average errors/day: {stats.AverageErrorsPerDay:N0}");
            Console.WriteLine($"  Average warnings/day: {stats.AverageWarningsPerDay:N0}");
        }

        [Test]
        public async Task GetTopServicesRawQueryTest()
        {
            // This test is no longer needed as TopServices functionality was removed
            // Arrange
            var from = DateTime.UtcNow.AddDays(-1);
            var to = DateTime.UtcNow;

            // Act - Get statistics without top services
            var stats = await Provider.GetStatisticsAsync(from, to);

            // Assert
            Assert.That(stats, Is.Not.Null);
            Assert.That(stats.TotalCount, Is.GreaterThanOrEqualTo(0));

            Console.WriteLine($"Statistics TotalCount: {stats.TotalCount:N0}");
        }

        [Test]
        public async Task GetStatisticsWithFiltersTest()
        {
            // Arrange
            var from = DateTime.UtcNow.AddDays(-1);
            var to = DateTime.UtcNow;
            var filters = new List<NewRelicLogFilter>
            {
                NewRelicLogFilters.LevelAtLeast(NewRelicLogSeverity.Warning)
            };

            // Act
            var stats = await Provider.GetStatisticsAsync(from, to, filters);

            // Assert
            Assert.That(stats, Is.Not.Null);
            Assert.That(stats.TotalCount, Is.GreaterThanOrEqualTo(0));

            // With Warning+ filter, debug count should be 0
            Assert.That(stats.DebugCount, Is.EqualTo(0));
            Assert.That(stats.InfoCount, Is.EqualTo(0));

            Console.WriteLine($"Warning+ statistics for last day:");
            Console.WriteLine($"  Total: {stats.TotalCount:N0} logs");
            Console.WriteLine($"  Errors: {stats.ErrorCount:N0}");
            Console.WriteLine($"  Warnings: {stats.WarningCount:N0}");
        }

        [Test]
        public async Task GetStatisticsComputedPropertiesTest()
        {
            // Arrange
            var from = DateTime.UtcNow.AddDays(-30);
            var to = DateTime.UtcNow;

            // Act
            var stats = await Provider.GetStatisticsAsync(from, to);

            // Assert - Verify computed properties
            Assert.That(stats.DurationDays, Is.EqualTo(30).Within(0.1));

            if (stats.TotalCount > 0)
            {
                // Verify rates sum to ~100%
                var totalRate = stats.ErrorRate + stats.WarningRate + stats.InfoRate + stats.DebugRate;
                Assert.That(totalRate, Is.EqualTo(100).Within(1.0), 
                    "Sum of all severity rates should be approximately 100%");

                // Verify daily averages
                var expectedLogsPerDay = stats.TotalCount / stats.DurationDays;
                Assert.That(stats.AverageLogsPerDay, Is.EqualTo(expectedLogsPerDay).Within(0.1));

                var expectedErrorsPerDay = stats.ErrorCount / stats.DurationDays;
                Assert.That(stats.AverageErrorsPerDay, Is.EqualTo(expectedErrorsPerDay).Within(0.1));

                var expectedWarningsPerDay = stats.WarningCount / stats.DurationDays;
                Assert.That(stats.AverageWarningsPerDay, Is.EqualTo(expectedWarningsPerDay).Within(0.1));
            }
        }

        #endregion

        #region Data Consumption Tests

        [Test]
        public async Task GetDataConsumptionSucceedsTest()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var start = now.AddDays(-10);
            var today = now.Date;

            // Act
            NewRelicDataConsumption consumption = null;

            Assert.DoesNotThrowAsync(async () =>
            {
                consumption = await Provider.GetDataConsumptionAsync(start, today);
            }, "API call failed. Check API Key and Account ID.");

            // Assert
            Assert.That(consumption, Is.Not.Null);
            Assert.That(consumption.StartDate, Is.EqualTo(start));
            Assert.That(consumption.EndDate, Is.EqualTo(today));

            // Log the results for manual verification
            Console.WriteLine($"\n═══ DATA CONSUMPTION (Month-to-Date) ═══");
            Console.WriteLine($"Period: {consumption.StartDate:yyyy-MM-dd} to {consumption.EndDate:yyyy-MM-dd}");
            Console.WriteLine($"");
            Console.WriteLine($"📊 TOTAL USAGE:");
            Console.WriteLine($"  Total:        {consumption.TotalGigabytes:N2} GB");
            Console.WriteLine($"  Daily Avg:    {consumption.DailyAverageGigabytes:N3} GB/day");
            Console.WriteLine($"  MTD:          {consumption.MonthToDateGigabytes:N2} GB");
            Console.WriteLine($"  Projected:    {consumption.ProjectedEndOfMonthGigabytes:N2} GB (end of month)");
            Console.WriteLine($"");
            Console.WriteLine($"📦 BY DATA TYPE:");
            Console.WriteLine($"  Logs:         {consumption.LogsGigabytes:N3} GB");
            Console.WriteLine($"  Metrics:      {consumption.MetricsGigabytes:N3} GB");
            Console.WriteLine($"  Traces:       {consumption.TracesGigabytes:N3} GB");
            Console.WriteLine($"  Events:       {consumption.EventsGigabytes:N3} GB");
            Console.WriteLine($"");
            Console.WriteLine($"🎯 FREE TIER STATUS (100 GB limit):");
            Console.WriteLine($"  Used:         {consumption.FreeTierUsagePercent:N1}%");
            Console.WriteLine($"  Remaining:    {consumption.FreeTierRemainingGB:N2} GB");
            
            if (consumption.WillExceedFreeTier)
            {
                Console.WriteLine($"  ⚠️  WARNING: Projected to exceed free tier!");
                Console.WriteLine($"  Overage:      {consumption.ProjectedOverageGB:N2} GB");
            }
            else
            {
                Console.WriteLine($"  ✅ Within free tier limits");
            }
        }

        [Test]
        public async Task GetDataConsumptionLast30DaysTest()
        {
            // Arrange
            var to = DateTime.UtcNow.Date;
            var from = to.AddDays(-30);

            // Act
            var consumption = await Provider.GetDataConsumptionAsync(from, to);

            // Assert
            Assert.That(consumption, Is.Not.Null);
            Assert.That(consumption.TotalGigabytes, Is.GreaterThanOrEqualTo(0));
            Assert.That(consumption.DailyAverageGigabytes, Is.GreaterThanOrEqualTo(0));

            Console.WriteLine($"Last 30 days consumption:");
            Console.WriteLine($"  Total: {consumption.TotalGigabytes:N2} GB");
            Console.WriteLine($"  Daily Average: {consumption.DailyAverageGigabytes:N3} GB/day");
            Console.WriteLine($"  Logs: {consumption.LogsGigabytes:N3} GB");
        }

        #endregion

        #region Properties

        private IConfiguration Configuration { get; set; }

        private static string ApiKey { get; set; }

        private static int AccountId { get; set; }

        private INewRelicProvider Provider { get; set; }

        #endregion
    }
}
