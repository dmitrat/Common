using OutWit.Common.MemoryPack;
using OutWit.Common.NewRelic.Model;
using OutWit.Common.NUnit;
using OutWit.Common.Utils;

namespace OutWit.Common.NewRelic.Tests.Model
{
    [TestFixture]
    public class NewRelicLogStatisticsTests
    {
        private NewRelicLogStatistics CreateTestStatistics()
        {
            return new NewRelicLogStatistics
            {
                From = DateTime.UtcNow.AddDays(-7),
                To = DateTime.UtcNow,
                TotalCount = 1_000_000,
                ErrorCount = 10_000,
                WarningCount = 50_000,
                InfoCount = 800_000,
                DebugCount = 140_000
            };
        }

        [Test]
        public void ConstructorTest()
        {
            // Arrange & Act
            var stats = new NewRelicLogStatistics();

            // Assert
            Assert.That(stats.From, Is.EqualTo(default(DateTime)));
            Assert.That(stats.To, Is.EqualTo(default(DateTime)));
            Assert.That(stats.TotalCount, Is.EqualTo(0));
        }

        [Test]
        public void IsTest()
        {
            // Arrange
            var stats1 = CreateTestStatistics();

            // Assert
            Assert.That(stats1, Was.EqualTo(stats1.Clone()));
            Assert.That(stats1, Was.Not.EqualTo(stats1.With(x => x.TotalCount, 2_000_000L)));
            Assert.That(stats1, Was.Not.EqualTo(stats1.With(x => x.ErrorCount, 20_000L)));
            Assert.That(stats1, Was.Not.EqualTo(stats1.With(x => x.WarningCount, 100_000L)));
        }

        [Test]
        public void CloneTest()
        {
            // Arrange
            var stats1 = CreateTestStatistics();

            // Act
            var clone = stats1.Clone() as NewRelicLogStatistics;

            // Assert
            Assert.That(clone, Is.Not.Null);
            Assert.That(clone, Is.Not.SameAs(stats1));
            Assert.That(clone, Was.EqualTo(stats1));
        }

        [Test]
        public void MemoryPackCloneTest()
        {
            // Arrange
            var stats1 = CreateTestStatistics();

            // Act
            var clone = stats1.MemoryPackClone();

            // Assert
            Assert.That(clone, Is.Not.Null);
            Assert.That(clone, Is.Not.SameAs(stats1));
            Assert.That(clone, Was.EqualTo(stats1));
        }

        [Test]
        public void ErrorRateCalculationTest()
        {
            // Arrange
            var stats = new NewRelicLogStatistics
            {
                TotalCount = 1000,
                ErrorCount = 100
            };

            // Act & Assert
            Assert.That(stats.ErrorRate, Is.EqualTo(10.0));
        }

        [Test]
        public void WarningRateCalculationTest()
        {
            // Arrange
            var stats = new NewRelicLogStatistics
            {
                TotalCount = 1000,
                WarningCount = 250
            };

            // Act & Assert
            Assert.That(stats.WarningRate, Is.EqualTo(25.0));
        }

        [Test]
        public void RatesWithZeroTotalCountTest()
        {
            // Arrange
            var stats = new NewRelicLogStatistics
            {
                TotalCount = 0,
                ErrorCount = 100
            };

            // Act & Assert
            Assert.That(stats.ErrorRate, Is.EqualTo(0));
            Assert.That(stats.WarningRate, Is.EqualTo(0));
            Assert.That(stats.InfoRate, Is.EqualTo(0));
            Assert.That(stats.DebugRate, Is.EqualTo(0));
        }

        [Test]
        public void DurationDaysCalculationTest()
        {
            // Arrange
            var from = new DateTime(2025, 1, 1);
            var to = new DateTime(2025, 1, 8); // 7 days
            var stats = new NewRelicLogStatistics
            {
                From = from,
                To = to
            };

            // Act & Assert
            Assert.That(stats.DurationDays, Is.EqualTo(7.0));
        }

        [Test]
        public void AverageLogsPerDayCalculationTest()
        {
            // Arrange
            var stats = new NewRelicLogStatistics
            {
                From = DateTime.UtcNow.AddDays(-7),
                To = DateTime.UtcNow,
                TotalCount = 700_000
            };

            // Act & Assert
            Assert.That(stats.AverageLogsPerDay, Is.EqualTo(100_000).Within(100));
        }

        [Test]
        public void AverageErrorsPerDayCalculationTest()
        {
            // Arrange
            var stats = new NewRelicLogStatistics
            {
                From = DateTime.UtcNow.AddDays(-7),
                To = DateTime.UtcNow,
                ErrorCount = 700
            };

            // Act & Assert
            Assert.That(stats.AverageErrorsPerDay, Is.EqualTo(100).Within(1));
        }

        [Test]
        public void AverageWarningsPerDayCalculationTest()
        {
            // Arrange
            var stats = new NewRelicLogStatistics
            {
                From = DateTime.UtcNow.AddDays(-10),
                To = DateTime.UtcNow,
                WarningCount = 1000
            };

            // Act & Assert
            Assert.That(stats.AverageWarningsPerDay, Is.EqualTo(100).Within(1));
        }

        [Test]
        public void AllRatesSumToHundredPercentTest()
        {
            // Arrange
            var stats = new NewRelicLogStatistics
            {
                TotalCount = 1000,
                ErrorCount = 100,
                WarningCount = 200,
                InfoCount = 500,
                DebugCount = 200
            };

            // Act
            var total = stats.ErrorRate + stats.WarningRate + stats.InfoRate + stats.DebugRate;

            // Assert
            Assert.That(total, Is.EqualTo(100.0).Within(0.1));
        }
    }
}
