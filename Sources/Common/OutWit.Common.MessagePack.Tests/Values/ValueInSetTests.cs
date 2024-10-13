using OutWit.Common.MessagePack.Values;
using NUnit.Framework;

namespace OutWit.Common.MessagePack.Tests.Values
{
    [TestFixture]
    public class ValueInSet
    {
        [Test]
        public void ConstructorTest()
        {
            var value = new ValueInSet<int>(1, 1, 2, 3);

            Assert.That(value.Value, Is.EqualTo(1));
            Assert.That(value.ValuesSet.Count, Is.EqualTo(3));
            Assert.That(value[0], Is.EqualTo(1));
            Assert.That(value[1], Is.EqualTo(2));
            Assert.That(value[2], Is.EqualTo(3));
        }

        [Test]
        public void IsTest()
        {
            var value1 = new ValueInSet<int>(1, 1, 2, 3);
            var value2 = new ValueInSet<int>(1, 1, 2, 3);

            Assert.That(value1.Is(value2), Is.True);

            value2 = new ValueInSet<int>(2, 1, 2, 3);
            Assert.That(value1.Is(value2), Is.False);

            value2 = new ValueInSet<int>(1, 1, 2);
            Assert.That(value1.Is(value2), Is.False);
        }

        [Test]
        public void CloneTest()
        {
            var value1 = new ValueInSet<int>(1, 1, 2, 3);

            var value2 = value1.Clone() as ValueInSet<int>;
            Assert.That(value2, Is.Not.Null);

            Assert.That(value1, Is.Not.SameAs(value2));
            Assert.That(value1.Is(value2), Is.True);
        }

        [Test]
        public void SerializationTest()
        {
            var value1 = new ValueInSet<int>(1, 1, 2, 3);

            var bytes = value1.ToPackBytes(false);

            var value2 = bytes.FromPackBytes<ValueInSet<int>>(false);
            Assert.That(value2, Is.Not.Null);

            Assert.That(value1, Is.Not.SameAs(value2));
            Assert.That(value1.Is(value2), Is.True);
        }
    }
}
