using OutWit.Common.NewRelic.Model;
using OutWit.Common.NUnit;
using OutWit.Common.MemoryPack;
using OutWit.Common.Utils;

namespace OutWit.Common.NewRelic.Tests.Model
{
    [TestFixture]
    public class NewRelicLogFilterTests
    {
        [Test]
        public void ConstructorAndFactoryTest()
        {
            // Arrange & Act (Default constructor)
            var filter = new NewRelicLogFilter();

            // Assert
            Assert.That(filter.Attribute, Is.EqualTo(string.Empty));
            Assert.That(filter.Operator, Is.EqualTo(NewRelicLogFilterOperator.Equals));
            Assert.That(filter.Values, Is.Empty);

            // Arrange & Act (Factory)
            var eqFilter = NewRelicLogFilter.Eq("level", "Error");
            var inFilter = NewRelicLogFilter.In("service", "A", "B");

            // Assert
            Assert.That(eqFilter.Attribute, Is.EqualTo("level"));
            Assert.That(eqFilter.Operator, Is.EqualTo(NewRelicLogFilterOperator.Equals));
            Assert.That(eqFilter.Values, Is.EquivalentTo(new[] { "Error" }));

            Assert.That(inFilter.Attribute, Is.EqualTo("service"));
            Assert.That(inFilter.Operator, Is.EqualTo(NewRelicLogFilterOperator.In));
            Assert.That(inFilter.Values, Is.EquivalentTo(new[] { "A", "B" }));
        }

        [Test]
        public void IsTest()
        {
            // Arrange
            var filter1 = NewRelicLogFilter.In("service", "A", "B");

            // Assert
            Assert.That(filter1, Was.EqualTo(filter1.Clone()));
            Assert.That(filter1, Was.Not.EqualTo(filter1.With(x => x.Attribute, "level")));
            Assert.That(filter1, Was.Not.EqualTo(filter1.With(x => x.Operator, NewRelicLogFilterOperator.NotEquals)));
            Assert.That(filter1, Was.Not.EqualTo(filter1.With(x => x.Values, new[] { "A", "C" })));
        }

        [Test]
        public void CloneTest()
        {
            // Arrange
            var filter1 = NewRelicLogFilter.In("service", "A", "B");

            // Act
            var clone = filter1.Clone() as NewRelicLogFilter;

            // Assert
            Assert.That(clone, Is.Not.Null);
            Assert.That(clone, Is.Not.SameAs(filter1));
            Assert.That(clone, Was.EqualTo(filter1));
            // Ensure array is deep copied
            Assert.That(clone.Values, Is.Not.SameAs(filter1.Values));
        }

        [Test]
        public void MemoryPackCloneTest()
        {
            // Arrange
            var filter1 = NewRelicLogFilter.In("service", "A", "B");

            // Act
            var clone = filter1.MemoryPackClone();

            // Assert
            Assert.That(clone, Is.Not.Null);
            Assert.That(clone, Is.Not.SameAs(filter1));
            Assert.That(clone, Was.EqualTo(filter1));
        }
    }
}
