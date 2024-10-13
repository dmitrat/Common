using OutWit.Common.MessagePack.Messages;
using NUnit.Framework;

namespace OutWit.Common.MessagePack.Tests.Messages
{
    [TestFixture]
    public class NetMessageTests
    {
        [Test]
        public void ConstructorTest()
        {
            var message = new NetMessage("testMessage", true);

            Assert.That(message.Message, Is.EqualTo("testMessage"));
            Assert.That(message.IsError, Is.EqualTo(true));
        }

        [Test]
        public void IsTest()
        {
            var message1 = new NetMessage("testMessage", true);
            var message2 = new NetMessage("testMessage", true);

            Assert.That(message1.Is(message2), Is.True);

            message2 = new NetMessage("testMessage1", true);
            Assert.That(message1.Is(message2), Is.False);

            message2 = new NetMessage("testMessage", false);
            Assert.That(message1.Is(message2), Is.False);
        }

        [Test]
        public void CloneTest()
        {
            var message1 = new NetMessage("testMessage", true);

            var message2 = message1.Clone() as NetMessage;
            Assert.That(message2, Is.Not.Null);

            Assert.That(message1, Is.Not.SameAs(message2));
            Assert.That(message1.Is(message2), Is.True);
        }

        [Test]
        public void SerializationTest()
        {
            var message1 = new NetMessage("testMessage", true);

            var bytes = message1.ToPackBytes(false);

            var message2 = bytes.FromPackBytes<NetMessage>(false);
            Assert.That(message2, Is.Not.Null);

            Assert.That(message1, Is.Not.SameAs(message2));
            Assert.That(message1.Is(message2), Is.True);
        }
    }
}
