using OutWit.Common.NewRelic.Model;
using OutWit.Common.NUnit;
using OutWit.Common.MemoryPack;
using OutWit.Common.Utils;

namespace OutWit.Common.NewRelic.Tests.Model
{
    [TestFixture]
    public class NewRelicLogQueryTests
    {
        private NewRelicLogQuery CreateTestQuery()
        {
            return new NewRelicLogQuery
            {
                From = DateTime.UtcNow.AddHours(-1),
                To = DateTime.UtcNow,
                Lookback = null,
                FullTextSearch = "error",
                Filters = new NewRelicLogFilter[]
                {
                    NewRelicLogFilter.Eq("level", "Error")
                },
                PageSize = 50,
                Offset = 10,
                SortOrder = NewRelicLogSortOrder.Ascending
            };
        }

        [Test]
        public void ConstructorTest()
        {
            // Arrange & Act
            var query = new NewRelicLogQuery();

            // Assert
            Assert.That(query.From, Is.Null);
            Assert.That(query.To, Is.Null);
            Assert.That(query.Lookback, Is.Null);
            Assert.That(query.FullTextSearch, Is.Null);
            Assert.That(query.Filters, Is.Null);
            Assert.That(query.PageSize, Is.Null);
            Assert.That(query.Offset, Is.EqualTo(0));
            // Check default
            Assert.That(query.SortOrder, Is.EqualTo(NewRelicLogSortOrder.Descending));
        }

        [Test]
        public void IsTest()
        {
            // Arrange
            var query1 = CreateTestQuery();

            // Assert
            Assert.That(query1, Was.EqualTo(query1.Clone()));
            Assert.That(query1, Was.Not.EqualTo(query1.With(x => x.FullTextSearch, "warn")));
            Assert.That(query1, Was.Not.EqualTo(query1.With(x => x.PageSize, 100)));
            Assert.That(query1, Was.Not.EqualTo(query1.With(x => x.SortOrder, NewRelicLogSortOrder.Descending)));

            // Test filter collection difference
            var queryWithDifferentFilters = CreateTestQuery();
            queryWithDifferentFilters.Filters = queryWithDifferentFilters.Filters?.Concat(new[] { NewRelicLogFilter.Eq("service", "A") }).ToArray();
            Assert.That(query1, Was.Not.EqualTo(queryWithDifferentFilters));
        }

        [Test]
        public void CloneTest()
        {
            // Arrange
            var query1 = CreateTestQuery();

            // Act
            var clone = query1.Clone() as NewRelicLogQuery;

            // Assert
            Assert.That(clone, Is.Not.Null);
            Assert.That(clone, Is.Not.SameAs(query1));
            Assert.That(clone, Was.EqualTo(query1));

            // Ensure deep copy of filters
            Assert.That(clone.Filters, Is.Not.Null);
            Assert.That(clone.Filters, Is.Not.SameAs(query1.Filters));
            Assert.That(clone.Filters[0], Is.Not.SameAs(query1.Filters[0]));
            Assert.That(clone.Filters[0], Was.EqualTo(query1.Filters[0]));
        }

        [Test]
        public void CloneHandlesNullFiltersTest()
        {
            // Arrange
            var query1 = CreateTestQuery();
            query1.Filters = null; // Set filters to null

            // Act
            var clone = query1.Clone() as NewRelicLogQuery;

            // Assert
            Assert.That(clone, Is.Not.Null);
            Assert.That(clone, Is.Not.SameAs(query1));
            Assert.That(clone, Was.EqualTo(query1));
            Assert.That(clone.Filters, Is.Null);
        }

        [Test]
        public void MemoryPackCloneTest()
        {
            // Arrange
            var query1 = CreateTestQuery();

            // Act
            var clone = query1.MemoryPackClone();

            // Assert
            Assert.That(clone, Is.Not.Null);
            Assert.That(clone, Is.Not.SameAs(query1));
            Assert.That(clone, Was.EqualTo(query1));
        }
    }
}
