using OutWit.Common.NewRelic.Model;
using OutWit.Common.NUnit;
using OutWit.Common.MemoryPack;
using OutWit.Common.Utils;

namespace OutWit.Common.NewRelic.Tests.Model
{
    [TestFixture]
    public class NewRelicLogPageTests
    {
        private NewRelicLogPage CreateTestPage()
        {
            return new NewRelicLogPage
            {
                Offset = 10,
                PageSize = 20,
                HasMore = true,
                Items = new[]
                {
                    new NewRelicLogEntry { Message = "Entry 1" },
                    new NewRelicLogEntry { Message = "Entry 2" }
                }
            };
        }

        [Test]
        public void ConstructorTest()
        {
            // Arrange & Act
            var page = new NewRelicLogPage();

            // Assert
            Assert.That(page.Offset, Is.EqualTo(0));
            Assert.That(page.PageSize, Is.EqualTo(0));
            Assert.That(page.HasMore, Is.False);
            Assert.That(page.Items, Is.Not.Null);
            Assert.That(page.Items, Is.Empty);
        }

        [Test]
        public void IsTest()
        {
            // Arrange
            var page1 = CreateTestPage();

            // Assert
            Assert.That(page1, Was.EqualTo(page1.Clone()));
            Assert.That(page1, Was.Not.EqualTo(page1.With(x => x.Offset, 11)));
            Assert.That(page1, Was.Not.EqualTo(page1.With(x => x.PageSize, 21)));
            Assert.That(page1, Was.Not.EqualTo(page1.With(x => x.HasMore, false)));

            // Test item collection difference
            var pageWithDifferentItems = CreateTestPage();
            pageWithDifferentItems.Items[1].Message = "Entry 2 changed";
            Assert.That(page1, Was.Not.EqualTo(pageWithDifferentItems));
        }

        [Test]
        public void CloneTest()
        {
            // Arrange
            var page1 = CreateTestPage();

            // Act
            var clone = page1.Clone() as NewRelicLogPage;

            // Assert
            Assert.That(clone, Is.Not.Null);
            Assert.That(clone, Is.Not.SameAs(page1));
            Assert.That(clone, Was.EqualTo(page1));

            // Ensure deep copy of items
            Assert.That(clone.Items, Is.Not.SameAs(page1.Items));
            Assert.That(clone.Items[0], Is.Not.SameAs(page1.Items[0]));
            Assert.That(clone.Items[0], Was.EqualTo(page1.Items[0]));
        }

        [Test]
        public void MemoryPackCloneTest()
        {
            // Arrange
            var page1 = CreateTestPage();

            // Act
            var clone = page1.MemoryPackClone();

            // Assert
            Assert.That(clone, Is.Not.Null);
            Assert.That(clone, Is.Not.SameAs(page1));
            Assert.That(clone, Was.EqualTo(page1));
        }
    }
}
